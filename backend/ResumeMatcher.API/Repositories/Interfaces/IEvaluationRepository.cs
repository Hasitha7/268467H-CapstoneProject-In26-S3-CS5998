using ResumeMatcher.API.Models;

namespace ResumeMatcher.API.Repositories.Interfaces;

public interface IEvaluationRepository
{
    Task<bool> ResumeAndJobExistAsync(long resumeId, long jobDescriptionId);
    Task<long> CreatePairAsync(EvaluationPairRequest request);
    Task<IEnumerable<EvaluationPair>> ListPairsAsync();
    Task<bool> UpdatePairAsync(long id, short label, string? notes);
    Task<IEnumerable<EvaluationPair>> GetPairsAsync();
    Task<EvaluationPair?> GetPairAsync(long id);
    Task<int> GetResumeCountAsync();
    Task<int> GetJobCountAsync();
}
