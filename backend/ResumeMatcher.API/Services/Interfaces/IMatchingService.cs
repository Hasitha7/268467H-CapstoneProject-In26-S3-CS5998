using ResumeMatcher.API.Models;

namespace ResumeMatcher.API.Services.Interfaces;

public interface IMatchingService
{
    Task<MatchingResult> RunAsync(MatchRequest request);
    Task<MatchingResultListItem?> GetAsync(long id);
    Task<IEnumerable<MatchingResultListItem>> ListAsync(long? resumeId, long? jobDescriptionId);
    Task<MatchSkillGaps?> GetSkillGapsAsync(long id);
    Task<MatchComparison?> CompareAsync(long resumeId, long jobDescriptionId);
}
