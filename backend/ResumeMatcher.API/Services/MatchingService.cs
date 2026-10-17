using System.Text.RegularExpressions;
using ResumeMatcher.API.Models;
using ResumeMatcher.API.Repositories;
using ResumeMatcher.API.Repositories.Interfaces;
using ResumeMatcher.API.Services.Interfaces;

namespace ResumeMatcher.API.Services;

public class MatchingService : IMatchingService
{
    public const string ScoringFormula = "FinalScore = 0.50 × SemanticScore + 0.50 × SkillCoverageScore; TF-IDF is retained as a lexical baseline.";
    private const string ModelVersion = "sentence-transformers-all-MiniLM-L6-v2";
    private readonly IResumeRepository _resumeRepository;
    private readonly IJobDescriptionRepository _jobRepository;
    private readonly IMatchingRepository _matchingRepository;
    private readonly IResumeService _resumeService;
    private readonly ISemanticSimilarityService _semanticSimilarityService;

    public MatchingService(
        IResumeRepository resumeRepository,
        IJobDescriptionRepository jobRepository,
        IMatchingRepository matchingRepository,
        IResumeService resumeService,
        ISemanticSimilarityService semanticSimilarityService)
    {
        _resumeRepository = resumeRepository;
        _jobRepository = jobRepository;
        _matchingRepository = matchingRepository;
        _resumeService = resumeService;
        _semanticSimilarityService = semanticSimilarityService;
    }

    public async Task<MatchingResult> RunAsync(MatchRequest request)
    {
        var resume = await _resumeRepository.GetResumeByIdAsync(request.ResumeId)
            ?? throw new KeyNotFoundException($"Resume {request.ResumeId} was not found.");
        var job = await _jobRepository.GetByIdAsync(request.JobDescriptionId)
            ?? throw new KeyNotFoundException($"Job description {request.JobDescriptionId} was not found.");

        if (string.IsNullOrWhiteSpace(resume.ExtractedText) || string.IsNullOrWhiteSpace(job.DescriptionText))
        {
            throw new ArgumentException("Resume and job description text must be non-empty to run matching.");
        }

        await _resumeService.ExtractSkillsAsync(request.ResumeId);

        var resumeSkills = (await _resumeRepository.GetResumeSkillsAsync(request.ResumeId)).ToList();
        var (matched, missingRequired, missingPreferred, coverage) = CalculateSkillGaps(job, resumeSkills, resume.ExtractedText);
        var lexical = CalculateTfIdfCosine(resume.ExtractedText, job.DescriptionText);
        var semantic = await _semanticSimilarityService.GetSimilarityAsync(resume.ExtractedText, job.DescriptionText);
        var final = Math.Clamp(0.5m * semantic + 0.5m * coverage, 0m, 1m);

        var result = new MatchingResult
        {
            ResumeId = request.ResumeId,
            JobDescriptionId = request.JobDescriptionId,
            TfIdfScore = decimal.Round(lexical, 6),
            SemanticScore = decimal.Round(semantic, 6),
            SkillCoverageScore = decimal.Round(coverage, 6),
            FinalScore = decimal.Round(final, 6),
            ModelVersion = ModelVersion,
            MatchedSkills = matched,
            MissingRequiredSkills = missingRequired,
            MissingPreferredSkills = missingPreferred,
            Explanation = $"Final score is a weighted ranking score (not a calibrated probability): 50% normalized Sentence-BERT cosine similarity, where normalized score = (cosine + 1) / 2, and 50% weighted skill coverage. TF-IDF cosine similarity is reported as a lexical baseline. {matched.Count} job skills matched; {missingRequired.Count} required and {missingPreferred.Count} preferred skills are missing."
        };

        result.MatchingResultId = await _matchingRepository.CreateAsync(result);
        return result;
    }

    public Task<MatchingResultListItem?> GetAsync(long id) => _matchingRepository.GetByIdAsync(id);

    public Task<IEnumerable<MatchingResultListItem>> ListAsync(long? resumeId, long? jobDescriptionId) =>
        _matchingRepository.ListAsync(resumeId, jobDescriptionId);

    public async Task<MatchSkillGaps?> GetSkillGapsAsync(long id)
    {
        var result = await _matchingRepository.GetByIdAsync(id);
        if (result is null)
        {
            return null;
        }

        var resume = await _resumeRepository.GetResumeByIdAsync(result.ResumeId);
        var job = await _jobRepository.GetByIdAsync(result.JobDescriptionId);
        if (resume is null || job is null)
        {
            throw new InvalidOperationException("A saved match references a missing resume or job description.");
        }

        var (matched, missingRequired, missingPreferred, _) = CalculateSkillGaps(
            job, await _resumeRepository.GetResumeSkillsAsync(result.ResumeId), resume.ExtractedText);
        return new MatchSkillGaps
        {
            MatchingResultId = id,
            MatchedSkills = matched,
            MissingRequiredSkills = missingRequired,
            MissingPreferredSkills = missingPreferred
        };
    }

    public async Task<MatchComparison?> CompareAsync(long resumeId, long jobDescriptionId)
    {
        var result = (await _matchingRepository.ListAsync(resumeId, jobDescriptionId)).FirstOrDefault();
        if (result is null)
        {
            return null;
        }

        return new MatchComparison
        {
            ResumeId = resumeId,
            JobDescriptionId = jobDescriptionId,
            TfIdfScore = result.TfIdfScore ?? 0,
            SemanticScore = result.SemanticScore ?? 0,
            SkillCoverageScore = result.SkillCoverageScore ?? 0,
            FinalScore = result.FinalScore ?? 0,
            ScoringFormula = ScoringFormula,
            ModelVersion = result.ModelVersion ?? string.Empty
        };
    }

    private static (List<string> Matched, List<string> MissingRequired, List<string> MissingPreferred, decimal Coverage)
        CalculateSkillGaps(JobDescription job, IEnumerable<ExtractedSkill> resumeSkills, string resumeText)
    {
        var extractedNames = resumeSkills.Select(skill => skill.SkillName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var matched = new List<string>();
        var missingRequired = new List<string>();
        var missingPreferred = new List<string>();
        decimal totalWeight = 0;
        decimal matchedWeight = 0;

        foreach (var jobSkill in job.Skills)
        {
            var preferred = jobSkill.Importance.Equals("Preferred", StringComparison.OrdinalIgnoreCase);
            var weight = preferred ? 0.5m : 1m;
            totalWeight += weight;
            var hasSkill = extractedNames.Contains(jobSkill.SkillName) ||
                           SkillTextMatcher.Find(jobSkill.SkillName, resumeText) is not null;
            if (hasSkill)
            {
                matched.Add(jobSkill.SkillName);
                matchedWeight += weight;
            }
            else if (preferred)
            {
                missingPreferred.Add(jobSkill.SkillName);
            }
            else
            {
                missingRequired.Add(jobSkill.SkillName);
            }
        }

        return (matched, missingRequired, missingPreferred, totalWeight == 0 ? 0 : matchedWeight / totalWeight);
    }

    private static decimal CalculateTfIdfCosine(string first, string second)
    {
        var documents = new[] { Tokenize(first), Tokenize(second) };
        var vocabulary = documents.SelectMany(document => document).Distinct(StringComparer.Ordinal).ToArray();
        if (vocabulary.Length == 0)
        {
            return 0;
        }

        var vectors = documents.Select(document =>
        {
            var frequencies = document.GroupBy(token => token).ToDictionary(group => group.Key, group => group.Count());
            return vocabulary.Select(token =>
            {
                var idf = Math.Log(1d + documents.Length / (1d + documents.Count(doc => doc.Contains(token))));
                return frequencies.GetValueOrDefault(token, 0) * idf;
            }).ToArray();
        }).ToArray();

        var dot = vectors[0].Zip(vectors[1], (left, right) => left * right).Sum();
        var normA = Math.Sqrt(vectors[0].Sum(value => value * value));
        var normB = Math.Sqrt(vectors[1].Sum(value => value * value));
        return normA == 0 || normB == 0 ? 0 : (decimal)Math.Clamp(dot / (normA * normB), 0d, 1d);
    }

    private static string[] Tokenize(string text) =>
        Regex.Matches(text.ToLowerInvariant(), @"[\p{L}\p{N}+#.]+")
            .Select(match => match.Value)
            .Where(token => token.Length > 1)
            .ToArray();
}