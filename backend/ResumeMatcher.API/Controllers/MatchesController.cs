using Microsoft.AspNetCore.Mvc;
using ResumeMatcher.API.Models;
using ResumeMatcher.API.Services.Interfaces;

namespace ResumeMatcher.API.Controllers;

[ApiController]
[Route("api/matches")]
public class MatchesController : ControllerBase
{
    private readonly IMatchingService _service;

    public MatchesController(IMatchingService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Run([FromBody] MatchRequest request)
    {
        if (request.ResumeId <= 0 || request.JobDescriptionId <= 0)
        {
            return BadRequest(new { message = "ResumeId and JobDescriptionId must be positive IDs." });
        }

        try
        {
            return Ok(await _service.RunAsync(request));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (HttpRequestException)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Semantic matching service unavailable",
                detail: "The Sentence-BERT NLP service could not process the request. Ensure nlp-service is running.");
        }
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] long? resumeId, [FromQuery] long? jobDescriptionId)
    {
        if (resumeId is <= 0 || jobDescriptionId is <= 0)
        {
            return BadRequest(new { message = "Filter IDs must be positive when provided." });
        }
        return Ok(await _service.ListAsync(resumeId, jobDescriptionId));
    }

    [HttpGet("compare")]
    public async Task<IActionResult> Compare([FromQuery] long resumeId, [FromQuery] long jobDescriptionId)
    {
        if (resumeId <= 0 || jobDescriptionId <= 0)
        {
            return BadRequest(new { message = "ResumeId and JobDescriptionId must be positive IDs." });
        }
        var comparison = await _service.CompareAsync(resumeId, jobDescriptionId);
        return comparison is null ? NotFound(new { message = "No matching result exists for the supplied resume and job." }) : Ok(comparison);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> Get(long id)
    {
        var match = await _service.GetAsync(id);
        return match is null ? NotFound(new { message = $"Matching result {id} was not found." }) : Ok(match);
    }

    [HttpGet("{id:long}/skill-gaps")]
    public async Task<IActionResult> GetSkillGaps(long id)
    {
        var gaps = await _service.GetSkillGapsAsync(id);
        return gaps is null ? NotFound(new { message = $"Matching result {id} was not found." }) : Ok(gaps);
    }
}
