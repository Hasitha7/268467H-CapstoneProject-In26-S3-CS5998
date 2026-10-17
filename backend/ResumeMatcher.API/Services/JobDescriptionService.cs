using System.Text.RegularExpressions;
using ResumeMatcher.API.Models;
using ResumeMatcher.API.Repositories.Interfaces;
using ResumeMatcher.API.Services.Interfaces;

namespace ResumeMatcher.API.Services;

public class JobDescriptionService : IJobDescriptionService
{
    private static readonly Regex SentenceBoundary = new(
        @"(?<=[!?;])\s+|(?<=\.)\s+(?=[A-Z])|\r?\n",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex RequiredCue = new(
        @"(?<!not )\b(required|must(?:\s+have)?|essential|mandatory|need(?:ed)?|proficient\s+in)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex PreferredCue = new(
        @"\b(preferred|preferably|nice\s+to\s+have|bonus|optional|a\s+plus|desirable|not\s+(?:required|necessary|mandatory))\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private readonly IJobDescriptionRepository _repository;

    public JobDescriptionService(IJobDescriptionRepository repository) => _repository = repository;

    public Task<long> CreateAsync(JobDescriptionRequest request) => _repository.CreateAsync(request);
    public Task<IEnumerable<JobDescriptionListItem>> ListAsync() => _repository.ListAsync();
    public Task<JobDescription?> GetByIdAsync(long id) => _repository.GetByIdAsync(id);
    public Task<bool> UpdateAsync(long id, JobDescriptionRequest request) => _repository.UpdateAsync(id, request);
    public Task<bool> DeleteAsync(long id) => _repository.DeleteAsync(id);

    public async Task<JobDescription?> ExtractSkillsAsync(long id)
    {
        var job = await _repository.GetByIdAsync(id);
        if (job is null)
        {
            return null;
        }

        var extracted = new List<JobSkill>();
        foreach (var skill in await _repository.GetSkillDictionaryAsync())
        {
            var match = SkillTextMatcher.FindMatch(skill.SkillName, job.DescriptionText);
            if (match is null)
            {
                continue;
            }

            var (context, contextStart) = FindSentenceContaining(job.DescriptionText, match.Index);
            var skillPosition = match.Index - contextStart;
            var requiredDistance = FindNearestCueDistance(RequiredCue, context, skillPosition);
            var preferredDistance = FindNearestCueDistance(PreferredCue, context, skillPosition);
            var isUnspecified = requiredDistance is null && preferredDistance is null;
            var importance = preferredDistance.HasValue &&
                             (!requiredDistance.HasValue || preferredDistance.Value < requiredDistance.Value)
                ? "Preferred"
                : "Required";

            extracted.Add(new JobSkill
            {
                SkillId = skill.SkillId,
                SkillName = skill.SkillName,
                Importance = importance,
                NeedsReview = isUnspecified ||
                              (requiredDistance.HasValue && preferredDistance.HasValue &&
                               requiredDistance.Value == preferredDistance.Value) ||
                              PreferredCue.IsMatch(context) &&
                              PreferredCue.Match(context).Value.StartsWith("not ", StringComparison.OrdinalIgnoreCase)
            });
        }

        await _repository.ReplaceSkillsAsync(id, extracted);
        return await _repository.GetByIdAsync(id);
    }

    private static (string Text, int Start) FindSentenceContaining(string text, int characterPosition)
    {
        var searchFrom = 0;
        foreach (var sentence in SentenceBoundary.Split(text))
        {
            var start = text.IndexOf(sentence, searchFrom, StringComparison.Ordinal);
            if (start < 0)
            {
                continue;
            }

            if (characterPosition >= start && characterPosition < start + sentence.Length)
            {
                return (sentence, start);
            }

            searchFrom = start + sentence.Length;
        }

        return (text, 0);
    }

    private static int? FindNearestCueDistance(Regex cue, string context, int skillPosition)
    {
        var distances = cue.Matches(context)
            .Select(match => Math.Abs(match.Index - skillPosition))
            .ToArray();
        return distances.Length == 0 ? null : distances.Min();
    }

    public async Task<JobDescription?> UpdateSkillsAsync(long id, JobSkillUpdateRequest request)
    {
        if (await _repository.GetByIdAsync(id) is null)
        {
            return null;
        }

        if (request.Skills.Any(skill =>
                string.IsNullOrWhiteSpace(skill.SkillName) ||
                skill.SkillName.Trim().Length > 150 ||
                (skill.Importance != "Required" && skill.Importance != "Preferred")))
        {
            throw new ArgumentException("Each skill needs a name of at most 150 characters and an Importance of Required or Preferred.");
        }

        var skills = request.Skills
            .GroupBy(skill => skill.SkillName.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group => new JobSkill
            {
                SkillName = group.First().SkillName.Trim(),
                Importance = group.First().Importance,
                NeedsReview = false
            })
            .ToList();
        await _repository.ReplaceSkillsAsync(id, skills);
        return await _repository.GetByIdAsync(id);
    }
}

internal static class SkillTextMatcher
{
    private static readonly IReadOnlyDictionary<string, string[]> Aliases =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["ASP.NET Core"] = [@"\bASP\s*\.?\s*NET\s*Core\b", @"\bASPNet\s+Core\b"],
            ["ASP.NET"] = [@"\bASP\s*\.?\s*NET\b(?!\s*Core)"],
            [".NET"] = [@"(?<!ASP)\.NET\b", @"\bdot\s*net\b", @"\bdotnet\b"],
            ["C#"] = [@"\bC\s*#(?!\w)", @"\bCSharp\b"],
            ["C++"] = [@"\bC\s*\+\+(?!\w)"],
            ["JavaScript"] = [@"\bJava\s*Script\b", @"\bJS\b"],
            ["TypeScript"] = [@"\bType\s*Script\b", @"\bTS\b"],
            ["Node.js"] = [@"\bNode\s*\.?\s*js\b", @"\bNodeJS\b"],
            ["REST API"] = [@"\bREST(?:ful)?\s+APIs?\b"],
            ["SQL Server"] = [@"\bSQL\s+Server\b", @"\bMSSQL\b"]
        };

    public static string? Find(string canonicalName, string text)
    {
        var match = FindMatch(canonicalName, text);
        return match?.Value;
    }

    public static Match? FindMatch(string canonicalName, string text)
    {
        var patterns = Aliases.TryGetValue(canonicalName, out var aliases)
            ? aliases
            : [Regex.Escape(canonicalName)];

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (match.Success)
            {
                return match;
            }
        }

        return null;
    }
}