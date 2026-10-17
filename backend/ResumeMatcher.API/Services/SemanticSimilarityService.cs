using System.Net.Http.Json;
using ResumeMatcher.API.Services.Interfaces;

namespace ResumeMatcher.API.Services;

public class SemanticSimilarityService : ISemanticSimilarityService
{
    private readonly HttpClient _httpClient;

    public SemanticSimilarityService(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<decimal> GetSimilarityAsync(
        string firstText, string secondText, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            "similarity",
            new { text1 = firstText, text2 = secondText },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            if ((int)response.StatusCode < 500 && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new ArgumentException("The NLP service rejected text that exceeds its supported input size.");
            }

            throw new HttpRequestException(
                $"NLP service returned HTTP {(int)response.StatusCode}.",
                null,
                response.StatusCode);
        }

        var result = await response.Content.ReadFromJsonAsync<SimilarityResponse>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("The NLP service returned an empty similarity response.");

        return Math.Clamp(result.Similarity, 0m, 1m);
    }

    private sealed class SimilarityResponse
    {
        public decimal Similarity { get; init; }
    }
}
