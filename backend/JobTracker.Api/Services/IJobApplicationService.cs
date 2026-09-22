using JobTracker.Api.Dtos;

namespace JobTracker.Api.Services;

public interface IJobApplicationService
{
    Task<List<JobApplicationDto>> GetAllForUserAsync(int userId);
    Task<JobApplicationDto?> GetByIdAsync(int userId, int applicationId);
    Task<JobApplicationDto> CreateAsync(int userId, CreateJobApplicationDto dto);
    Task<JobApplicationDto?> UpdateAsync(int userId, int applicationId, UpdateJobApplicationDto dto);
    Task<bool> DeleteAsync(int userId, int applicationId);
    Task<ApplicationStatsDto> GetStatsAsync(int userId);
}
