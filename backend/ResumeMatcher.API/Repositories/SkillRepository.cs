using Dapper;
using ResumeMatcher.API.Data;
using ResumeMatcher.API.Models;
using ResumeMatcher.API.Repositories.Interfaces;

namespace ResumeMatcher.API.Repositories;

public class SkillRepository : ISkillRepository
{
    private readonly SqlConnectionFactory _connectionFactory;

    public SkillRepository(SqlConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<IEnumerable<Skill>> ListAsync() => await QuerySkillsAsync(
        "SELECT SkillId, SkillName, Category FROM dbo.Skills ORDER BY SkillName;", null);

    public async Task<IEnumerable<string>> GetCategoriesAsync()
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<string>(
            "SELECT DISTINCT Category FROM dbo.Skills WHERE Category IS NOT NULL ORDER BY Category;");
    }

    public async Task<IEnumerable<Skill>> SearchAsync(string query) => await QuerySkillsAsync(
        """
        SELECT SkillId, SkillName, Category FROM dbo.Skills
        WHERE SkillName LIKE @Pattern OR Category LIKE @Pattern
        ORDER BY SkillName;
        """, new { Pattern = $"%{EscapeLike(query.Trim())}%" });

    public async Task<long> CreateAsync(SkillRequest request)
    {
        const string sql = """
            INSERT INTO dbo.Skills (SkillName, Category)
            VALUES (@SkillName, @Category);
            SELECT CAST(SCOPE_IDENTITY() AS BIGINT);
            """;
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<long>(sql, new
        {
            SkillName = request.SkillName.Trim(),
            Category = NormalizeCategory(request.Category)
        });
    }

    public async Task<bool> UpdateAsync(long id, SkillRequest request)
    {
        const string sql = """
            UPDATE dbo.Skills SET SkillName = @SkillName, Category = @Category
            WHERE SkillId = @Id;
            """;
        using var connection = _connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(sql, new
        {
            SkillName = request.SkillName.Trim(),
            Category = NormalizeCategory(request.Category),
            Id = id
        }) > 0;
    }

    public async Task<Skill?> GetByIdAsync(long id)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<Skill>(
            "SELECT SkillId, SkillName, Category FROM dbo.Skills WHERE SkillId = @Id;",
            new { Id = id });
    }

    public async Task<IEnumerable<Skill>> GetAllAsync() => await ListAsync();

    private async Task<IEnumerable<Skill>> QuerySkillsAsync(string sql, object? parameters)
    {
        using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<Skill>(sql, parameters);
    }

    private static string? NormalizeCategory(string? category) =>
        string.IsNullOrWhiteSpace(category) ? null : category.Trim();

    private static string EscapeLike(string value) =>
        value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");
}
