using ResumeMatcher.API.Models;

namespace ResumeMatcher.API.Services.Interfaces;

public interface IJobDescriptionService
{
    Task<long> CreateAsync(JobDescriptionRequest request);
    Task<IEnumerable<JobDescriptionListItem>> ListAsync();
    Task<JobDescription?> GetByIdAsync(long id);
    Task<bool> UpdateAsync(long id, JobDescriptionRequest request);
    Task<bool> DeleteAsync(long id);
    Task<JobDescription?> ExtractSkillsAsync(long id);
    Task<JobDescription?> UpdateSkillsAsync(long id, JobSkillUpdateRequest request);
}
