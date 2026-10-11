
using ResumeMatcher.API.Data;

var builder = WebApplication.CreateBuilder(args);

// Register OpenAPI
builder.Services.AddOpenApi();

// Register database connection factory
builder.Services.AddSingleton<SqlConnectionFactory>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Test database connection
app.MapGet("/api/database/test", async (
    SqlConnectionFactory connectionFactory) =>
{
    using var connection = connectionFactory.CreateConnection();

    try
    {
        var database = await Dapper.SqlMapper
            .QuerySingleAsync<string>(
                connection, "SELECT DB_NAME();");

        return Results.Ok(new
        {
            message = "Database connection successful",
            database
        });
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Database connection failed.");

        return Results.Problem(
            title: "Database connection failed",
            detail: "Check the SQL Server instance and connection string.");
    }
});

app.UseHttpsRedirection();

app.Run();