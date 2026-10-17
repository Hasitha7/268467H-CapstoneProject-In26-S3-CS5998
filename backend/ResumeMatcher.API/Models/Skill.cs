namespace ResumeMatcher.API.Models;

public class Skill
{
    public long SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? Category { get; set; }
}

public class SkillRequest
{
    public string SkillName { get; set; } = string.Empty;
    public string? Category { get; set; }
}
