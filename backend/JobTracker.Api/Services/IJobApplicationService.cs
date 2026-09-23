using JobTracker.Api.Dtos;

namespace JobTracker.Api.Services;

public interface IJobApplicationService
{
    Task<PagedResultDto<JobApplicationDto>> GetAllForUserAsync(int userId, int page, int pageSize, bool includeArchived);
    Task<JobApplicationDto?> GetByIdAsync(int userId, int applicationId);
    Task<JobApplicationDto> CreateAsync(int userId, CreateJobApplicationDto dto);
    Task<JobApplicationDto?> UpdateAsync(int userId, int applicationId, UpdateJobApplicationDto dto);
    Task<bool> ArchiveAsync(int userId, int applicationId);
    Task<bool> RestoreAsync(int userId, int applicationId);
    Task<bool> DeleteAsync(int userId, int applicationId);
    Task<ApplicationStatsDto> GetStatsAsync(int userId);
}
