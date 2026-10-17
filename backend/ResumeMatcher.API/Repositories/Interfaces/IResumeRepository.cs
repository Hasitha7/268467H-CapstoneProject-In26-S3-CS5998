using ResumeMatcher.API.Models;

namespace ResumeMatcher.API.Repositories;

public interface IResumeRepository
{
    Task<long> InsertResumeAsync(string fileName, string extractedText);
    Task<IEnumerable<ResumeListItem>> GetResumesAsync();
    Task<Resume?> GetResumeByIdAsync(long resumeId);
    Task<bool> DeleteResumeAsync(long resumeId);
    Task<long> GetOrCreateSkillAsync(string skillName);
    Task UpsertResumeSkillAsync(long resumeId, long skillId, decimal confidence);
    Task ReplaceResumeSkillsAsync(long resumeId, IEnumerable<ExtractedSkill> skills);
    Task<IEnumerable<ExtractedSkill>> GetResumeSkillsAsync(long resumeId);
    Task<IEnumerable<Skill>> GetAllSkillsAsync();
}
