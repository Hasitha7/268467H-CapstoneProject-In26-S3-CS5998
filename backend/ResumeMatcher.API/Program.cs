
using ResumeMatcher.API.Data;
using ResumeMatcher.API.Repositories;
using ResumeMatcher.API.Repositories.Interfaces;
using ResumeMatcher.API.Services;
using ResumeMatcher.API.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddScoped<IResumeRepository, ResumeRepository>();
builder.Services.AddScoped<IResumeService, ResumeService>();
builder.Services.AddScoped<IJobDescriptionRepository, JobDescriptionRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IMatchingRepository, MatchingRepository>();
builder.Services.AddScoped<IEvaluationRepository, EvaluationRepository>();
builder.Services.AddScoped<IReportingRepository, ReportingRepository>();
builder.Services.AddScoped<IJobDescriptionService, JobDescriptionService>();
builder.Services.AddScoped<ISkillService, SkillService>();
builder.Services.AddScoped<IMatchingService, MatchingService>();
builder.Services.AddScoped<IEvaluationService, EvaluationService>();
builder.Services.AddHttpClient<ISemanticSimilarityService, SemanticSimilarityService>(client =>
{
    var baseUrl = builder.Configuration["NlpService:BaseUrl"]
        ?? throw new InvalidOperationException("NlpService:BaseUrl is not configured.");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromMinutes(3);
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "resume-matcher-api"
}));

app.MapControllers();

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

app.Run();