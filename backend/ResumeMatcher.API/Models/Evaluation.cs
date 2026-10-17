namespace ResumeMatcher.API.Models;

public class EvaluationPair
{
    public long EvaluationPairId { get; set; }
    public long ResumeId { get; set; }
    public long JobDescriptionId { get; set; }
    public short HumanLabel { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EvaluationPairRequest
{
    public long ResumeId { get; set; }
    public long JobDescriptionId { get; set; }
    public short HumanLabel { get; set; }
    public string? Notes { get; set; }
}

public class EvaluationRunResult
{
    public int EvaluatedPairCount { get; set; }
    public decimal Accuracy { get; set; }
    public decimal MacroF1 { get; set; }
    public decimal MeanAbsoluteError { get; set; }
    public string PredictionMapping { get; set; } = string.Empty;
    public Dictionary<string, ModelEvaluationMetrics> ModelMetrics { get; set; } = [];
}

public class EvaluationSummary
{
    public int EvaluatedPairCount { get; set; }
    public decimal Accuracy { get; set; }
    public decimal MacroF1 { get; set; }
    public decimal MeanAbsoluteError { get; set; }
    public string PredictionMapping { get; set; } = string.Empty;
    public Dictionary<string, decimal> MeanScores { get; set; } = [];
    public Dictionary<string, ModelEvaluationMetrics> ModelMetrics { get; set; } = [];
}

public class ModelEvaluationMetrics
{
    public decimal Accuracy { get; set; }
    public decimal MacroF1 { get; set; }
    public decimal MeanAbsoluteError { get; set; }
}
