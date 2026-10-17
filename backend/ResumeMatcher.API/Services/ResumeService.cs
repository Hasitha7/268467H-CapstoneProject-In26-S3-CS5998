using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using ResumeMatcher.API.Models;
using ResumeMatcher.API.Repositories;
using UglyToad.PdfPig;

namespace ResumeMatcher.API.Services;

public class ResumeService : IResumeService
{
    private readonly IResumeRepository _resumeRepository;

    public ResumeService(IResumeRepository resumeRepository)
    {
        _resumeRepository = resumeRepository;
    }

    public async Task<ResumeUploadResult> UploadResumeAsync(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("A valid resume file is required.", nameof(file));
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            throw new ArgumentException("Resume files must be 10 MB or smaller.", nameof(file));
        }

        var extension = Path.GetExtension(file.FileName);
        if (!extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase) &&
            !extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only PDF and TXT resume files are supported.", nameof(file));
        }

        var fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Length > 255)
        {
            throw new ArgumentException("The resume filename must contain 1 to 255 characters.", nameof(file));
        }

        var extractedText = await ExtractTextFromFileAsync(file);
        var resumeId = await _resumeRepository.InsertResumeAsync(fileName, extractedText);

        return new ResumeUploadResult
        {
            ResumeId = resumeId,
            FileName = fileName,
            Message = "Resume uploaded and text extracted successfully"
        };
    }

    public async Task<IEnumerable<ResumeListItem>> ListResumesAsync()
    {
        return await _resumeRepository.GetResumesAsync();
    }

    public async Task<Resume?> GetResumeByIdAsync(long resumeId)
    {
        return await _resumeRepository.GetResumeByIdAsync(resumeId);
    }

    public async Task DeleteResumeAsync(long resumeId)
    {
        if (!await _resumeRepository.DeleteResumeAsync(resumeId))
        {
            throw new KeyNotFoundException($"Resume {resumeId} was not found.");
        }
    }

    public async Task<SkillExtractionResult> ExtractSkillsAsync(long resumeId)
    {
        var resume = await _resumeRepository.GetResumeByIdAsync(resumeId)
            ?? throw new KeyNotFoundException($"Resume {resumeId} was not found.");

        var normalizedText = NormalizeText(resume.ExtractedText);
        var skills = new List<ExtractedSkill>();

        foreach (var skill in await _resumeRepository.GetAllSkillsAsync())
        {
            if (SkillTextMatcher.Find(skill.SkillName, resume.ExtractedText) is null)
            {
                continue;
            }

            skills.Add(new ExtractedSkill
            {
                SkillId = skill.SkillId,
                SkillName = skill.SkillName,
                Confidence = CalculateConfidence(skill.SkillName, normalizedText)
            });
        }

        await _resumeRepository.ReplaceResumeSkillsAsync(resumeId, skills);
        return new SkillExtractionResult
        {
            ResumeId = resumeId,
            ExtractedSkillCount = skills.Count,
            Skills = skills
        };
    }

    private static async Task<string> ExtractTextFromFileAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        await using var stream = file.OpenReadStream();

        if (extension == ".pdf")
        {
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var pdfDocument = PdfDocument.Open(memoryStream);
            var textBuilder = new StringBuilder();

            foreach (var page in pdfDocument.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }

            return textBuilder.ToString();
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync();
    }

    private static string NormalizeText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var compressed = Regex.Replace(value, "[\\r\\n\\t]+", " ");
        compressed = Regex.Replace(compressed, "[^a-zA-Z0-9#.+/\\-]+", " ");
        return compressed.Trim().ToLowerInvariant();
    }

    private static decimal CalculateConfidence(string skillName, string normalizedText)
    {
        var occurrences = CountOccurrences(normalizedText, NormalizeText(skillName));
        var baseConfidence = 0.55m + Math.Min(0.45m, (decimal)occurrences * 0.15m);
        return Math.Clamp(baseConfidence, 0.55m, 1.0m);
    }

    private static int CountOccurrences(string text, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
