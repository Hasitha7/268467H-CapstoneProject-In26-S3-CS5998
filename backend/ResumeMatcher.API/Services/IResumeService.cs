using Microsoft.AspNetCore.Http;
using ResumeMatcher.API.Models;

namespace ResumeMatcher.API.Services;

public interface IResumeService
{
    Task<ResumeUploadResult> UploadResumeAsync(IFormFile file);
    Task<IEnumerable<ResumeListItem>> ListResumesAsync();
    Task<Resume?> GetResumeByIdAsync(long resumeId);
    Task DeleteResumeAsync(long resumeId);
    Task<SkillExtractionResult> ExtractSkillsAsync(long resumeId);
}
