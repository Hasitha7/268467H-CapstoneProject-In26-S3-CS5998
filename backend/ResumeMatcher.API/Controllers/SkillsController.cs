using Microsoft.AspNetCore.Mvc;
using ResumeMatcher.API.Models;
using ResumeMatcher.API.Services.Interfaces;

namespace ResumeMatcher.API.Controllers;

[ApiController]
[Route("api/skills")]
public class SkillsController : ControllerBase
{
    private readonly ISkillService _service;

    public SkillsController(ISkillService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetSkills() => Ok(await _service.ListAsync());

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories() => Ok(await _service.GetCategoriesAsync());

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { message = "The query parameter is required." });
        }
        return Ok(await _service.SearchAsync(query));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SkillRequest request)
    {
        try
        {
            var id = await _service.CreateAsync(request);
            return Created("/api/skills", new { skillId = id, request.SkillName, request.Category });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (Microsoft.Data.SqlClient.SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return Conflict(new { message = "A skill with this name already exists." });
        }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] SkillRequest request)
    {
        try
        {
            return await _service.UpdateAsync(id, request)
                ? Ok(new { skillId = id, request.SkillName, request.Category })
                : NotFound(new { message = $"Skill {id} was not found." });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { message = exception.Message });
        }
        catch (Microsoft.Data.SqlClient.SqlException exception) when (exception.Number is 2601 or 2627)
        {
            return Conflict(new { message = "A skill with this name already exists." });
        }
    }
}
