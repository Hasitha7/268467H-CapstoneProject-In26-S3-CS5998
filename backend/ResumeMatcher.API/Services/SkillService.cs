using ResumeMatcher.API.Models;
using ResumeMatcher.API.Repositories.Interfaces;
using ResumeMatcher.API.Services.Interfaces;

namespace ResumeMatcher.API.Services;

public class SkillService : ISkillService
{
    private readonly ISkillRepository _repository;

    public SkillService(ISkillRepository repository) => _repository = repository;

    public Task<IEnumerable<Skill>> ListAsync() => _repository.ListAsync();
    public Task<IEnumerable<string>> GetCategoriesAsync() => _repository.GetCategoriesAsync();
    public async Task<IEnumerable<Skill>> SearchAsync(string query)
    {
        var skills = await _repository.GetAllAsync();
        return skills.Where(skill =>
            skill.SkillName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            SkillTextMatcher.Find(skill.SkillName, query) is not null);
    }

    public async Task<long> CreateAsync(SkillRequest request)
    {
        Validate(request);
        await EnsureNameAvailableAsync(request.SkillName, null);
        return await _repository.CreateAsync(request);
    }

    public async Task<bool> UpdateAsync(long id, SkillRequest request)
    {
        Validate(request);
        if (await _repository.GetByIdAsync(id) is null)
        {
            return false;
        }
        await EnsureNameAvailableAsync(request.SkillName, id);
        return await _repository.UpdateAsync(id, request);
    }

    private async Task EnsureNameAvailableAsync(string name, long? exceptId)
    {
        var duplicate = (await _repository.GetAllAsync()).Any(skill =>
            skill.SkillId != exceptId && skill.SkillName.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (duplicate)
        {
            throw new InvalidOperationException($"Skill '{name.Trim()}' already exists.");
        }
    }

    private static void Validate(SkillRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SkillName))
        {
            throw new ArgumentException("SkillName is required.");
        }
        if (request.SkillName.Trim().Length > 150)
        {
            throw new ArgumentException("SkillName must be 150 characters or fewer.");
        }
        if (request.Category?.Length > 100)
        {
            throw new ArgumentException("Category must be 100 characters or fewer.");
        }
    }
}
