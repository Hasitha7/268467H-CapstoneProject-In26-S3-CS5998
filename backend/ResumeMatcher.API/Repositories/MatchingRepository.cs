using Dapper;
using ResumeMatcher.API.Data;
using ResumeMatcher.API.Models;
using ResumeMatcher.API.Repositories.Interfaces;

namespace ResumeMatcher.API.Repositories;

public class MatchingRepository : IMatchingRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public MatchingRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<long> CreateAsync(MatchingResult result)
    {
        const string sql = """
            INSERT INTO dbo.MatchingResults
                (ResumeId, JobDescriptionId, TfIdfScore, SemanticScore, SkillCoverageScore, FinalScore, ModelVersion)
            VALUES
                (@ResumeId, @JobDescriptionId, @TfIdfScore, @SemanticScore, @SkillCoverageScore, @FinalScore, @ModelVersion);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
            """;
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<long>(sql, result);
    }

    public async Task<MatchingResultListItem?> GetByIdAsync(long id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<MatchingResultListItem>(
            """
            SELECT MatchingResultId, ResumeId, JobDescriptionId, TfIdfScore, SemanticScore,
                   SkillCoverageScore, FinalScore, ModelVersion, CreatedAt
            FROM dbo.MatchingResults WHERE MatchingResultId = @Id;
            """, new { Id = id });
    }

    public async Task<IEnumerable<MatchingResultListItem>> ListAsync(long? resumeId, long? jobDescriptionId)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<MatchingResultListItem>(
            """
            SELECT MatchingResultId, ResumeId, JobDescriptionId, TfIdfScore, SemanticScore,
                   SkillCoverageScore, FinalScore, ModelVersion, CreatedAt
            FROM dbo.MatchingResults
            WHERE (@ResumeId IS NULL OR ResumeId = @ResumeId)
              AND (@JobDescriptionId IS NULL OR JobDescriptionId = @JobDescriptionId)
            ORDER BY CreatedAt DESC;
            """, new { ResumeId = resumeId, JobDescriptionId = jobDescriptionId });
    }

    public async Task<IEnumerable<EvaluationResultRow>> GetEvaluationResultsAsync(long? evaluationPairId = null)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<EvaluationResultRow>(
            """
            WITH LatestResult AS
            (
                SELECT ep.EvaluationPairId, ep.HumanLabel, mr.TfIdfScore, mr.SemanticScore,
                       mr.SkillCoverageScore, mr.FinalScore, mr.CreatedAt,
                       ROW_NUMBER() OVER (PARTITION BY ep.EvaluationPairId ORDER BY mr.CreatedAt DESC, mr.MatchingResultId DESC) AS RowNum
                FROM dbo.EvaluationPairs ep
                INNER JOIN dbo.MatchingResults mr
                    ON mr.ResumeId = ep.ResumeId AND mr.JobDescriptionId = ep.JobDescriptionId
                WHERE (@EvaluationPairId IS NULL OR ep.EvaluationPairId = @EvaluationPairId)
            )
            SELECT EvaluationPairId, HumanLabel, TfIdfScore, SemanticScore, SkillCoverageScore, FinalScore, CreatedAt
            FROM LatestResult WHERE RowNum = 1;
            """, new { EvaluationPairId = evaluationPairId });
    }

    public async Task<int> GetCountAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.MatchingResults;");
    }
}
