using ResumeMatcher.API.Models;
using ResumeMatcher.API.Repositories.Interfaces;
using ResumeMatcher.API.Services.Interfaces;

namespace ResumeMatcher.API.Services;

public class EvaluationService : IEvaluationService
{
    private const string Mapping = "Score <= 0.25 => 0 (Poor); <= 0.50 => 1 (Weak); <= 0.75 => 2 (Moderate); otherwise 3 (Strong). Thresholds are explicit and should be validated against a held-out labelled set.";
    private readonly IEvaluationRepository _evaluationRepository;
    private readonly IMatchingRepository _matchingRepository;
    private readonly IMatchingService _matchingService;

    public EvaluationService(
        IEvaluationRepository evaluationRepository,
        IMatchingRepository matchingRepository,
        IMatchingService matchingService)
    {
        _evaluationRepository = evaluationRepository;
        _matchingRepository = matchingRepository;
        _matchingService = matchingService;
    }

    public async Task<long> CreatePairAsync(EvaluationPairRequest request)
    {
        ValidateLabel(request.HumanLabel);
        if (!await _evaluationRepository.ResumeAndJobExistAsync(request.ResumeId, request.JobDescriptionId))
        {
            throw new KeyNotFoundException("The supplied resume or job description was not found.");
        }
        return await _evaluationRepository.CreatePairAsync(request);
    }

    public Task<IEnumerable<EvaluationPair>> ListPairsAsync() => _evaluationRepository.ListPairsAsync();

    public async Task<bool> UpdatePairAsync(long id, short label, string? notes)
    {
        ValidateLabel(label);
        return await _evaluationRepository.UpdatePairAsync(id, label, notes);
    }

    public async Task<EvaluationRunResult> RunAsync()
    {
        var pairs = await _evaluationRepository.GetPairsAsync();
        foreach (var pair in pairs)
        {
            await _matchingService.RunAsync(new MatchRequest
            {
                ResumeId = pair.ResumeId,
                JobDescriptionId = pair.JobDescriptionId
            });
        }

        var rows = (await _matchingRepository.GetEvaluationResultsAsync()).ToList();
        var summary = CalculateMetrics(rows, row => row.FinalScore);
        return new EvaluationRunResult
        {
            EvaluatedPairCount = summary.EvaluatedPairCount,
            Accuracy = summary.Accuracy,
            MacroF1 = summary.MacroF1,
            MeanAbsoluteError = summary.MeanAbsoluteError,
            PredictionMapping = Mapping,
            ModelMetrics = CalculateModelMetrics(rows)
        };
    }

    public async Task<EvaluationSummary?> GetResultsAsync(long id)
    {
        if (await _evaluationRepository.GetPairAsync(id) is null)
        {
            return null;
        }

        return BuildSummary(await _matchingRepository.GetEvaluationResultsAsync(id));
    }

    public async Task<EvaluationSummary> GetSummaryAsync() =>
        BuildSummary(await _matchingRepository.GetEvaluationResultsAsync());

    private static EvaluationSummary BuildSummary(IEnumerable<EvaluationResultRow> source)
    {
        var rows = source.ToList();
        var summary = CalculateMetrics(rows, row => row.FinalScore);
        summary.PredictionMapping = Mapping;
        summary.MeanScores = new Dictionary<string, decimal>
        {
            ["tfIdfScore"] = Mean(rows, row => row.TfIdfScore),
            ["semanticScore"] = Mean(rows, row => row.SemanticScore),
            ["skillCoverageScore"] = Mean(rows, row => row.SkillCoverageScore),
            ["finalScore"] = Mean(rows, row => row.FinalScore)
        };
        summary.ModelMetrics = CalculateModelMetrics(rows);
        return summary;
    }

    private static Dictionary<string, ModelEvaluationMetrics> CalculateModelMetrics(
        IReadOnlyCollection<EvaluationResultRow> rows) => new()
    {
        ["tfIdf"] = ToMetrics(CalculateMetrics(rows, row => row.TfIdfScore)),
        ["semantic"] = ToMetrics(CalculateMetrics(rows, row => row.SemanticScore)),
        ["skillCoverage"] = ToMetrics(CalculateMetrics(rows, row => row.SkillCoverageScore)),
        ["final"] = ToMetrics(CalculateMetrics(rows, row => row.FinalScore))
    };

    private static ModelEvaluationMetrics ToMetrics(EvaluationSummary summary) => new()
    {
        Accuracy = summary.Accuracy,
        MacroF1 = summary.MacroF1,
        MeanAbsoluteError = summary.MeanAbsoluteError
    };

    private static EvaluationSummary CalculateMetrics(
        IReadOnlyCollection<EvaluationResultRow> rows,
        Func<EvaluationResultRow, decimal?> scoreSelector)
    {
        var evaluated = rows.Where(row => scoreSelector(row).HasValue).ToList();
        if (evaluated.Count == 0)
        {
            return new EvaluationSummary();
        }

        var confusion = new int[4, 4];
        var absoluteError = 0;
        foreach (var row in evaluated)
        {
            var prediction = MapScore(scoreSelector(row)!.Value);
            confusion[row.HumanLabel, prediction]++;
            absoluteError += Math.Abs(row.HumanLabel - prediction);
        }

        var correct = Enumerable.Range(0, 4).Sum(label => confusion[label, label]);
        decimal f1Sum = 0;
        for (var label = 0; label < 4; label++)
        {
            var truePositive = confusion[label, label];
            var falsePositive = Enumerable.Range(0, 4).Where(other => other != label).Sum(other => confusion[other, label]);
            var falseNegative = Enumerable.Range(0, 4).Where(other => other != label).Sum(other => confusion[label, other]);
            var denominator = 2 * truePositive + falsePositive + falseNegative;
            f1Sum += denominator == 0 ? 0 : (decimal)(2 * truePositive) / denominator;
        }

        return new EvaluationSummary
        {
            EvaluatedPairCount = evaluated.Count,
            Accuracy = decimal.Round((decimal)correct / evaluated.Count, 4),
            MacroF1 = decimal.Round(f1Sum / 4, 4),
            MeanAbsoluteError = decimal.Round((decimal)absoluteError / evaluated.Count, 4)
        };
    }

    private static decimal Mean(IEnumerable<EvaluationResultRow> rows, Func<EvaluationResultRow, decimal?> selector)
    {
        var scores = rows.Select(selector).Where(score => score.HasValue).Select(score => score!.Value).ToList();
        return scores.Count == 0 ? 0 : decimal.Round(scores.Average(), 4);
    }

    private static short MapScore(decimal score) => score switch
    {
        <= 0.25m => 0,
        <= 0.50m => 1,
        <= 0.75m => 2,
        _ => 3
    };

    private static void ValidateLabel(short label)
    {
        if (label is < 0 or > 3)
        {
            throw new ArgumentException("HumanLabel must be between 0 (Poor) and 3 (Strong).");
        }
    }
}