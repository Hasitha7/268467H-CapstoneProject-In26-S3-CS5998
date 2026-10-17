using Dapper;
using ResumeMatcher.API.Data;
using ResumeMatcher.API.Models;
using System.Data;

namespace ResumeMatcher.API.Repositories;

public class ResumeRepository : IResumeRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ResumeRepository(SqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<long> InsertResumeAsync(string fileName, string extractedText)
    {
        const string sql = @"
            INSERT INTO dbo.Resumes (FileName, ExtractedText, UploadedAt)
            VALUES (@FileName, @ExtractedText, SYSUTCDATETIME());
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        using IDbConnection connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<long>(sql, new
        {
            FileName = fileName,
            ExtractedText = extractedText
        });
    }

    public async Task<IEnumerable<ResumeListItem>> GetResumesAsync()
    {
        const string sql = @"
            SELECT ResumeId, FileName, UploadedAt
            FROM dbo.Resumes
            ORDER BY UploadedAt DESC;";

        using IDbConnection connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ResumeListItem>(sql);
    }

    public async Task<Resume?> GetResumeByIdAsync(long resumeId)
    {
        const string sql = @"
            SELECT ResumeId, FileName, ExtractedText, UploadedAt
            FROM dbo.Resumes
            WHERE ResumeId = @ResumeId;";

        using IDbConnection connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Resume>(sql, new { ResumeId = resumeId });
    }

    public async Task<bool> DeleteResumeAsync(long resumeId)
    {
        using IDbConnection connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(
                "DELETE FROM dbo.MatchingResults WHERE ResumeId = @ResumeId;",
                new { ResumeId = resumeId }, transaction);
            await connection.ExecuteAsync(
                "DELETE FROM dbo.EvaluationPairs WHERE ResumeId = @ResumeId;",
                new { ResumeId = resumeId }, transaction);
            var deleted = await connection.ExecuteAsync(
                "DELETE FROM dbo.Resumes WHERE ResumeId = @ResumeId;",
                new { ResumeId = resumeId }, transaction);
            transaction.Commit();
            return deleted > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<long> GetOrCreateSkillAsync(string skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            throw new ArgumentException("Skill name cannot be empty.", nameof(skillName));
        }

        const string selectSql = @"
            SELECT SkillId
            FROM dbo.Skills
            WHERE SkillName = @SkillName;";

        const string insertSql = @"
            INSERT INTO dbo.Skills (SkillName, Category)
            VALUES (@SkillName, NULL);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";

        using IDbConnection connection = _connectionFactory.CreateConnection();

        var existingSkillId = await connection.QuerySingleOrDefaultAsync<long?>(selectSql, new { SkillName = skillName });
        if (existingSkillId.HasValue)
        {
            return existingSkillId.Value;
        }

        return await connection.QuerySingleAsync<long>(insertSql, new { SkillName = skillName });
    }

    public async Task UpsertResumeSkillAsync(long resumeId, long skillId, decimal confidence)
    {
        const string sql = @"
            IF EXISTS (
                SELECT 1
                FROM dbo.ResumeSkills
                WHERE ResumeId = @ResumeId AND SkillId = @SkillId)
            BEGIN
                UPDATE dbo.ResumeSkills
                SET Confidence = @Confidence
                WHERE ResumeId = @ResumeId AND SkillId = @SkillId;
            END
            ELSE
            BEGIN
                INSERT INTO dbo.ResumeSkills (ResumeId, SkillId, Confidence)
                VALUES (@ResumeId, @SkillId, @Confidence);
            END;";

        using IDbConnection connection = _connectionFactory.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            ResumeId = resumeId,
            SkillId = skillId,
            Confidence = confidence
        });
    }

    public async Task<IEnumerable<ExtractedSkill>> GetResumeSkillsAsync(long resumeId)
    {
        const string sql = @"
            SELECT rs.SkillId, s.SkillName, rs.Confidence
            FROM dbo.ResumeSkills rs
            INNER JOIN dbo.Skills s ON s.SkillId = rs.SkillId
            WHERE rs.ResumeId = @ResumeId
            ORDER BY s.SkillName;";

        using IDbConnection connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<ExtractedSkill>(sql, new { ResumeId = resumeId });
    }

    public async Task ReplaceResumeSkillsAsync(long resumeId, IEnumerable<ExtractedSkill> skills)
    {
        using IDbConnection connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(
                "DELETE FROM dbo.ResumeSkills WHERE ResumeId = @ResumeId;",
                new { ResumeId = resumeId }, transaction);
            foreach (var skill in skills)
            {
                await connection.ExecuteAsync(
                    """
                    INSERT INTO dbo.ResumeSkills (ResumeId, SkillId, Confidence)
                    VALUES (@ResumeId, @SkillId, @Confidence);
                    """,
                    new { ResumeId = resumeId, skill.SkillId, skill.Confidence }, transaction);
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<IEnumerable<Skill>> GetAllSkillsAsync()
    {
        const string sql = @"
            SELECT SkillId, SkillName, Category
            FROM dbo.Skills
            ORDER BY LEN(SkillName) DESC, SkillName;";

        using IDbConnection connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Skill>(sql);
    }
}
