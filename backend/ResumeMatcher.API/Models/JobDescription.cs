namespace ResumeMatcher.API.Models;

public class JobDescription
{
    public long JobDescriptionId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string DescriptionText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<JobSkill> Skills { get; set; } = [];
}

public class JobDescriptionListItem
{
    public long JobDescriptionId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class JobDescriptionRequest
{
    public string JobTitle { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string DescriptionText { get; set; } = string.Empty;
}

public class JobSkill
{
    public long SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string Importance { get; set; } = "Required";
    public bool NeedsReview { get; set; }
}

public class JobSkillUpdateRequest
{
    public List<JobSkillUpdate> Skills { get; set; } = [];
}

public class JobSkillUpdate
{
    public string SkillName { get; set; } = string.Empty;
    public string Importance { get; set; } = string.Empty;
}
