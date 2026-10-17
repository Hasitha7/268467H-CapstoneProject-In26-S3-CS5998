using Microsoft.AspNetCore.Mvc;
using ResumeMatcher.API.Repositories.Interfaces;
using ResumeMatcher.API.Services;
using ResumeMatcher.API.Services.Interfaces;

namespace ResumeMatcher.API.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportingRepository _repository;
    private readonly IMatchingService _matchingService;
    private readonly IEvaluationService _evaluationService;

    public ReportsController(
        IReportingRepository repository,
        IMatchingService matchingService,
        IEvaluationService evaluationService)
    {
        _repository = repository;
        _matchingService = matchingService;
        _evaluationService = evaluationService;
    }

    [HttpGet("/api/dashboard/summary")]
    public async Task<IActionResult> GetDashboardSummary() => Ok(new
    {
        resumeCount = await _repository.GetResumeCountAsync(),
        jobDescriptionCount = await _repository.GetJobCountAsync(),
        completedMatchCount = await _repository.GetMatchCountAsync()
    });

    [HttpGet("matches/{id:long}")]
    public async Task<IActionResult> GetMatchReport(long id)
    {
        var match = await _matchingService.GetAsync(id);
        if (match is null)
        {
            return NotFound(new { message = $"Matching result {id} was not found." });
        }

        return Ok(new
        {
            match,
            skillGaps = await _matchingService.GetSkillGapsAsync(id),
            scoringFormula = MatchingService.ScoringFormula
        });
    }

    [HttpGet("evaluations/{id:long}")]
    public async Task<IActionResult> GetEvaluationReport(long id)
    {
        var report = await _evaluationService.GetResultsAsync(id);
        return report is null ? NotFound(new { message = $"Evaluation pair {id} was not found." }) : Ok(report);
    }
}
