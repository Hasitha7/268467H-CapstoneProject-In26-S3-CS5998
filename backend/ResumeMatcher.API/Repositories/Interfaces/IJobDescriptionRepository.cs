using ResumeMatcher.API.Models;

namespace ResumeMatcher.API.Repositories.Interfaces;

public interface IJobDescriptionRepository
{
    Task<long> CreateAsync(JobDescriptionRequest request);
    Task<IEnumerable<JobDescriptionListItem>> ListAsync();
    Task<JobDescription?> GetByIdAsync(long id);
    Task<bool> UpdateAsync(long id, JobDescriptionRequest request);
    Task<bool> DeleteAsync(long id);
    Task ReplaceSkillsAsync(long id, IEnumerable<JobSkill> skills);
    Task<IEnumerable<Skill>> GetSkillDictionaryAsync();
    Task<long> GetOrCreateSkillAsync(string name);
}
