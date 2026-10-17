using System.Data;
using Dapper;
using ResumeMatcher.API.Data;
using ResumeMatcher.API.Models;
using ResumeMatcher.API.Repositories.Interfaces;

namespace ResumeMatcher.API.Repositories;

public class JobDescriptionRepository : IJobDescriptionRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public JobDescriptionRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<long> CreateAsync(JobDescriptionRequest request)
    {
        const string sql = """
            INSERT INTO dbo.JobDescriptions (JobTitle, CompanyName, DescriptionText)
            VALUES (@JobTitle, @CompanyName, @DescriptionText);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
            """;
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<long>(sql, request);
    }

    public async Task<IEnumerable<JobDescriptionListItem>> ListAsync()
    {
        const string sql = """
            SELECT JobDescriptionId, JobTitle, CompanyName, CreatedAt
            FROM dbo.JobDescriptions
            ORDER BY CreatedAt DESC;
            """;
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<JobDescriptionListItem>(sql);
    }

    public async Task<JobDescription?> GetByIdAsync(long id)
    {
        const string jobSql = """
            SELECT JobDescriptionId, JobTitle, CompanyName, DescriptionText, CreatedAt
            FROM dbo.JobDescriptions WHERE JobDescriptionId = @Id;
            """;
        const string skillsSql = """
            SELECT s.SkillId, s.SkillName, jds.Importance, jds.NeedsReview
            FROM dbo.JobDescriptionSkills jds
            INNER JOIN dbo.Skills s ON s.SkillId = jds.SkillId
            WHERE jds.JobDescriptionId = @Id
            ORDER BY jds.Importance, s.SkillName;
            """;
        using var connection = _connectionFactory.CreateConnection();
        var job = await connection.QuerySingleOrDefaultAsync<JobDescription>(jobSql, new { Id = id });
        if (job is not null)
        {
            job.Skills = (await connection.QueryAsync<JobSkill>(skillsSql, new { Id = id })).ToList();
        }
        return job;
    }

    public async Task<bool> UpdateAsync(long id, JobDescriptionRequest request)
    {
        const string sql = """
            UPDATE dbo.JobDescriptions
            SET JobTitle = @JobTitle, CompanyName = @CompanyName, DescriptionText = @DescriptionText
            WHERE JobDescriptionId = @Id;
            """;
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(sql, new { request.JobTitle, request.CompanyName, request.DescriptionText, Id = id }) > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(
                "DELETE FROM dbo.MatchingResults WHERE JobDescriptionId = @Id;",
                new { Id = id }, transaction);
            await connection.ExecuteAsync(
                "DELETE FROM dbo.EvaluationPairs WHERE JobDescriptionId = @Id;",
                new { Id = id }, transaction);
            var deleted = await connection.ExecuteAsync(
                "DELETE FROM dbo.JobDescriptions WHERE JobDescriptionId = @Id;",
                new { Id = id }, transaction);
            transaction.Commit();
            return deleted > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task ReplaceSkillsAsync(long id, IEnumerable<JobSkill> skills)
    {
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(
                "DELETE FROM dbo.JobDescriptionSkills WHERE JobDescriptionId = @Id;",
                new { Id = id }, transaction);
            foreach (var skill in skills)
            {
                var skillId = await GetOrCreateSkillAsync(connection, transaction, skill.SkillName);
                await connection.ExecuteAsync(
                    """
                    INSERT INTO dbo.JobDescriptionSkills (JobDescriptionId, SkillId, Importance, NeedsReview)
                    VALUES (@Id, @SkillId, @Importance, @NeedsReview);
                    """,
                    new { Id = id, SkillId = skillId, skill.Importance, skill.NeedsReview }, transaction);
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<Skill>> GetSkillDictionaryAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Skill>(
            "SELECT SkillId, SkillName, Category FROM dbo.Skills ORDER BY LEN(SkillName) DESC, SkillName;");
    }

    public async Task<long> GetOrCreateSkillAsync(string name)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await GetOrCreateSkillAsync(connection, null, name);
    }

    private static async Task<long> GetOrCreateSkillAsync(
        IDbConnection connection, IDbTransaction? transaction, string name)
    {
        const string sql = """
            DECLARE @SkillId BIGINT;
            SELECT @SkillId = SkillId
            FROM dbo.Skills WITH (UPDLOCK, HOLDLOCK)
            WHERE SkillName = @Name;
            IF @SkillId IS NULL
            BEGIN
                INSERT INTO dbo.Skills (SkillName, Category) VALUES (@Name, NULL);
                SET @SkillId = CAST(SCOPE_IDENTITY() AS BIGINT);
            END;
            SELECT @SkillId;
            """;
        return await connection.QuerySingleAsync<long>(sql, new { Name = name.Trim() }, transaction);
    }
}
