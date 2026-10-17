namespace ResumeMatcher.API.Services.Interfaces;

public interface ISemanticSimilarityService
{
    Task<decimal> GetSimilarityAsync(string firstText, string secondText, CancellationToken cancellationToken = default);
}
