using Microsoft.AspNetCore.Mvc;
using ResumeMatcher.API.Services;

namespace ResumeMatcher.API.Controllers;

[ApiController]
[Route("api/resumes")]
public class ResumesController : ControllerBase
{
    private readonly IResumeService _resumeService;

    public ResumesController(IResumeService resumeService)
    {
        _resumeService = resumeService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadResume([FromForm] IFormFile file)
    {
        try
        {
            var result = await _resumeService.UploadResumeAsync(file);
            return CreatedAtAction(nameof(GetResumeById), new { id = result.ResumeId }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetResumes()
    {
        var resumes = await _resumeService.ListResumesAsync();
        return Ok(resumes);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetResumeById(long id)
    {
        var resume = await _resumeService.GetResumeByIdAsync(id);

        if (resume is null)
        {
            return NotFound(new { message = $"Resume {id} was not found." });
        }

        return Ok(resume);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteResume(long id)
    {
        try
        {
            await _resumeService.DeleteResumeAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:long}/extract-skills")]
    public async Task<IActionResult> ExtractSkills(long id)
    {
        try
        {
            var result = await _resumeService.ExtractSkillsAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
