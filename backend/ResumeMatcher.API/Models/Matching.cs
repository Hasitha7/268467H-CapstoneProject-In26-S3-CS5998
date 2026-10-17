namespace ResumeMatcher.API.Models;

public class MatchRequest
{
    public long ResumeId { get; set; }
    public long JobDescriptionId { get; set; }
}

public class MatchingResult
{
    public long MatchingResultId { get; set; }
    public long ResumeId { get; set; }
    public long JobDescriptionId { get; set; }
    public decimal TfIdfScore { get; set; }
    public decimal SemanticScore { get; set; }
    public decimal SkillCoverageScore { get; set; }
    public decimal FinalScore { get; set; }
    public string ModelVersion { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<string> MatchedSkills { get; set; } = [];
    public List<string> MissingRequiredSkills { get; set; } = [];
    public List<string> MissingPreferredSkills { get; set; } = [];
    public string Explanation { get; set; } = string.Empty;
}

public class MatchingResultListItem
{
    public long MatchingResultId { get; set; }
    public long ResumeId { get; set; }
    public long JobDescriptionId { get; set; }
    public decimal? TfIdfScore { get; set; }
    public decimal? SemanticScore { get; set; }
    public decimal? SkillCoverageScore { get; set; }
    public decimal? FinalScore { get; set; }
    public string? ModelVersion { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MatchSkillGaps
{
    public long MatchingResultId { get; set; }
    public List<string> MatchedSkills { get; set; } = [];
    public List<string> MissingRequiredSkills { get; set; } = [];
    public List<string> MissingPreferredSkills { get; set; } = [];
}

public class MatchComparison
{
    public long ResumeId { get; set; }
    public long JobDescriptionId { get; set; }
    public decimal TfIdfScore { get; set; }
    public decimal SemanticScore { get; set; }
    public decimal SkillCoverageScore { get; set; }
    public decimal FinalScore { get; set; }
    public string ScoringFormula { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
}
