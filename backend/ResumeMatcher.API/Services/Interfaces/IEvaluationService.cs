using ResumeMatcher.API.Models;

namespace ResumeMatcher.API.Services.Interfaces;

public interface IEvaluationService
{
    Task<long> CreatePairAsync(EvaluationPairRequest request);
    Task<IEnumerable<EvaluationPair>> ListPairsAsync();
    Task<bool> UpdatePairAsync(long id, short label, string? notes);
    Task<EvaluationRunResult> RunAsync();
    Task<EvaluationSummary?> GetResultsAsync(long id);
    Task<EvaluationSummary> GetSummaryAsync();
}
