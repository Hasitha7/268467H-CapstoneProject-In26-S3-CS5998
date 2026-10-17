using Microsoft.AspNetCore.Mvc;
using ResumeMatcher.API.Models;
using ResumeMatcher.API.Services.Interfaces;

namespace ResumeMatcher.API.Controllers;

[ApiController]
[Route("api/evaluations")]
public class EvaluationsController : ControllerBase
{
    private readonly IEvaluationService _service;

    public EvaluationsController(IEvaluationService service) => _service = service;

    [HttpPost("pairs")]
    public async Task<IActionResult> CreatePair([FromBody] EvaluationPairRequest request)
    {
        if (request.ResumeId <= 0 || request.JobDescriptionId <= 0)
        {
            return BadRequest(new { message = "ResumeId and JobDescriptionId must be positive IDs." });
        }

        try
        {
            var id = await _service.CreatePairAsync(request);
            return CreatedAtAction(nameof(GetResults), new { id }, new { evaluationPairId = id });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { message = exception.Message });
        }
        catch (Microsoft.Data.SqlClient.SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return Conflict(new { message = "An evaluation pair already exists for this resume and job description." });
        }
    }

    [HttpGet("pairs")]
    public async Task<IActionResult> ListPairs() => Ok(await _service.ListPairsAsync());

    [HttpPut("pairs/{id:long}")]
    public async Task<IActionResult> UpdatePair(long id, [FromBody] EvaluationPairRequest request)
    {
        try
        {
            return await _service.UpdatePairAsync(id, request.HumanLabel, request.Notes)
                ? Ok(new { evaluationPairId = id, request.HumanLabel, request.Notes })
                : NotFound(new { message = $"Evaluation pair {id} was not found." });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("run")]
    public async Task<IActionResult> Run()
    {
        try
        {
            return Ok(await _service.RunAsync());
        }
        catch (HttpRequestException)
        {
            return Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Semantic matching service unavailable",
                detail: "The Sentence-BERT NLP service could not process evaluation pairs.");
        }
        catch (KeyNotFoundException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    [HttpGet("{id:long}/results")]
    public async Task<IActionResult> GetResults(long id)
    {
        var result = await _service.GetResultsAsync(id);
        return result is null ? NotFound(new { message = $"Evaluation pair {id} was not found." }) : Ok(result);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary() => Ok(await _service.GetSummaryAsync());
}
