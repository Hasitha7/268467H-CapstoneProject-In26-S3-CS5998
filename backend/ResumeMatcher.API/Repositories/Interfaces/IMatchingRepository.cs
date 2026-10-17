using ResumeMatcher.API.Models;

namespace ResumeMatcher.API.Repositories.Interfaces;

public interface IMatchingRepository
{
    Task<long> CreateAsync(MatchingResult result);
    Task<MatchingResultListItem?> GetByIdAsync(long id);
    Task<IEnumerable<MatchingResultListItem>> ListAsync(long? resumeId, long? jobDescriptionId);
    Task<IEnumerable<EvaluationResultRow>> GetEvaluationResultsAsync(long? evaluationPairId = null);
    Task<int> GetCountAsync();
}

public class EvaluationResultRow
{
    public long EvaluationPairId { get; set; }
    public short HumanLabel { get; set; }
    public decimal? TfIdfScore { get; set; }
    public decimal? SemanticScore { get; set; }
    public decimal? SkillCoverageScore { get; set; }
    public decimal? FinalScore { get; set; }
    public DateTime CreatedAt { get; set; }
}
