namespace ResumeMatcher.API.Repositories.Interfaces;

public interface IReportingRepository
{
    Task<int> GetResumeCountAsync();
    Task<int> GetJobCountAsync();
    Task<int> GetMatchCountAsync();
}
