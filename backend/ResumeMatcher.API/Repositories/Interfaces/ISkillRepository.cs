using ResumeMatcher.API.Models;

namespace ResumeMatcher.API.Repositories.Interfaces;

public interface ISkillRepository
{
    Task<IEnumerable<Skill>> ListAsync();
    Task<IEnumerable<string>> GetCategoriesAsync();
    Task<IEnumerable<Skill>> SearchAsync(string query);
    Task<long> CreateAsync(SkillRequest request);
    Task<bool> UpdateAsync(long id, SkillRequest request);
    Task<Skill?> GetByIdAsync(long id);
    Task<IEnumerable<Skill>> GetAllAsync();
}
