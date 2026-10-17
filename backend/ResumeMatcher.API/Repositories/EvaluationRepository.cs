using Dapper;
using ResumeMatcher.API.Data;
using ResumeMatcher.API.Models;
using ResumeMatcher.API.Repositories.Interfaces;

namespace ResumeMatcher.API.Repositories;

public class EvaluationRepository : IEvaluationRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public EvaluationRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<bool> ResumeAndJobExistAsync(long resumeId, long jobDescriptionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            """
            SELECT CASE WHEN
                EXISTS (SELECT 1 FROM dbo.Resumes WHERE ResumeId = @ResumeId)
                AND EXISTS (SELECT 1 FROM dbo.JobDescriptions WHERE JobDescriptionId = @JobDescriptionId)
            THEN 1 ELSE 0 END;
            """, new { ResumeId = resumeId, JobDescriptionId = jobDescriptionId });
    }

    public async Task<long> CreatePairAsync(EvaluationPairRequest request)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<long>(
            """
            INSERT INTO dbo.EvaluationPairs (ResumeId, JobDescriptionId, HumanLabel, Notes)
            VALUES (@ResumeId, @JobDescriptionId, @HumanLabel, @Notes);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
            """, request);
    }

    public async Task<IEnumerable<EvaluationPair>> ListPairsAsync() => await GetPairsAsync();

    public async Task<IEnumerable<EvaluationPair>> GetPairsAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<EvaluationPair>(
            """
            SELECT EvaluationPairId, ResumeId, JobDescriptionId, HumanLabel, Notes, CreatedAt
            FROM dbo.EvaluationPairs ORDER BY CreatedAt DESC;
            """);
    }

    public async Task<bool> UpdatePairAsync(long id, short label, string? notes)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(
            "UPDATE dbo.EvaluationPairs SET HumanLabel = @Label, Notes = @Notes WHERE EvaluationPairId = @Id;",
            new { Label = label, Notes = notes, Id = id }) > 0;
    }

    public async Task<EvaluationPair?> GetPairAsync(long id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<EvaluationPair>(
            """
            SELECT EvaluationPairId, ResumeId, JobDescriptionId, HumanLabel, Notes, CreatedAt
            FROM dbo.EvaluationPairs WHERE EvaluationPairId = @Id;
            """, new { Id = id });
    }

    public async Task<int> GetResumeCountAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Resumes;");
    }

    public async Task<int> GetJobCountAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.JobDescriptions;");
    }
}
