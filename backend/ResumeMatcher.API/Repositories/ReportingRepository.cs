using Dapper;
using ResumeMatcher.API.Data;
using ResumeMatcher.API.Repositories.Interfaces;

namespace ResumeMatcher.API.Repositories;

public class ReportingRepository : IReportingRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public ReportingRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<int> GetResumeCountAsync() => await CountAsync("dbo.Resumes");
    public async Task<int> GetJobCountAsync() => await CountAsync("dbo.JobDescriptions");
    public async Task<int> GetMatchCountAsync() => await CountAsync("dbo.MatchingResults");

    private async Task<int> CountAsync(string table)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM {table};");
    }
}
