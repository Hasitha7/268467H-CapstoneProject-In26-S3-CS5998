namespace ResumeMatcher.API.Models;

public class Resume
{
    public long ResumeId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ExtractedText { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class ResumeListItem
{
    public long ResumeId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class ResumeUploadResult
{
    public long ResumeId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class SkillExtractionResult
{
    public long ResumeId { get; set; }
    public int ExtractedSkillCount { get; set; }
    public List<ExtractedSkill> Skills { get; set; } = new();
}

public class ExtractedSkill
{
    public long SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
}
