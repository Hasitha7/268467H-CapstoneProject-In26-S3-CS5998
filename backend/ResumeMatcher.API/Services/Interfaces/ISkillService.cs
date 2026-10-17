using ResumeMatcher.API.Models;

namespace ResumeMatcher.API.Services.Interfaces;

public interface ISkillService
{
    Task<IEnumerable<Skill>> ListAsync();
    Task<IEnumerable<string>> GetCategoriesAsync();
    Task<IEnumerable<Skill>> SearchAsync(string query);
    Task<long> CreateAsync(SkillRequest request);
    Task<bool> UpdateAsync(long id, SkillRequest request);
}
