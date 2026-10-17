using Microsoft.AspNetCore.Mvc;
using ResumeMatcher.API.Models;
using ResumeMatcher.API.Services.Interfaces;

namespace ResumeMatcher.API.Controllers;

[ApiController]
[Route("api/job-descriptions")]
public class JobDescriptionsController : ControllerBase
{
    private readonly IJobDescriptionService _service;

    public JobDescriptionsController(IJobDescriptionService service) => _service = service;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JobDescriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JobTitle) || string.IsNullOrWhiteSpace(request.DescriptionText))
        {
            return BadRequest(new { message = "JobTitle and DescriptionText are required." });
        }

        if (request.JobTitle.Length > 255 || request.CompanyName?.Length > 255)
        {
            return BadRequest(new { message = "JobTitle and CompanyName must be 255 characters or fewer." });
        }

        var id = await _service.CreateAsync(request);
        var job = await _service.GetByIdAsync(id);
        return CreatedAtAction(nameof(GetById), new { id }, job);
    }

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await _service.ListAsync());

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var job = await _service.GetByIdAsync(id);
        return job is null ? NotFound(new { message = $"Job description {id} was not found." }) : Ok(job);
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] JobDescriptionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.JobTitle) || string.IsNullOrWhiteSpace(request.DescriptionText))
        {
            return BadRequest(new { message = "JobTitle and DescriptionText are required." });
        }
        if (request.JobTitle.Length > 255 || request.CompanyName?.Length > 255)
        {
            return BadRequest(new { message = "JobTitle and CompanyName must be 255 characters or fewer." });
        }

        if (!await _service.UpdateAsync(id, request))
        {
            return NotFound(new { message = $"Job description {id} was not found." });
        }

        return Ok(await _service.GetByIdAsync(id));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        return await _service.DeleteAsync(id)
            ? NoContent()
            : NotFound(new { message = $"Job description {id} was not found." });
    }

    [HttpPost("{id:long}/extract-skills")]
    public async Task<IActionResult> ExtractSkills(long id)
    {
        var job = await _service.ExtractSkillsAsync(id);
        return job is null ? NotFound(new { message = $"Job description {id} was not found." }) : Ok(job);
    }

    [HttpPut("{id:long}/skills")]
    public async Task<IActionResult> UpdateSkills(long id, [FromBody] JobSkillUpdateRequest request)
    {
        try
        {
            var job = await _service.UpdateSkillsAsync(id, request);
            return job is null ? NotFound(new { message = $"Job description {id} was not found." }) : Ok(job);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
