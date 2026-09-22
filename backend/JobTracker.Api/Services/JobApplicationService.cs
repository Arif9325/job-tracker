using JobTracker.Api.Data;
using JobTracker.Api.Dtos;
using JobTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Services;

// All the actual business logic lives here, independent of HTTP.
// Every method takes `userId` explicitly and filters by it — this is
// what stops one logged-in user from ever seeing or editing another
// user's applications, even if they guess a valid application ID.
public class JobApplicationService : IJobApplicationService
{
    private readonly AppDbContext _db;

    public JobApplicationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<JobApplicationDto>> GetAllForUserAsync(int userId)
    {
        return await _db.JobApplications
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.DateApplied)
            .Select(a => ToDto(a))
            .ToListAsync();
    }

    public async Task<JobApplicationDto?> GetByIdAsync(int userId, int applicationId)
    {
        var app = await _db.JobApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

        return app is null ? null : ToDto(app);
    }

    public async Task<JobApplicationDto> CreateAsync(int userId, CreateJobApplicationDto dto)
    {
        var app = new JobApplication
        {
            UserId = userId,
            Company = dto.Company,
            Role = dto.Role,
            Status = dto.Status,
            DateApplied = dto.DateApplied,
            PostingUrl = dto.PostingUrl,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _db.JobApplications.Add(app);
        await _db.SaveChangesAsync();

        return ToDto(app);
    }

    public async Task<JobApplicationDto?> UpdateAsync(int userId, int applicationId, UpdateJobApplicationDto dto)
    {
        var app = await _db.JobApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

        if (app is null) return null;

        app.Company = dto.Company;
        app.Role = dto.Role;
        app.Status = dto.Status;
        app.DateApplied = dto.DateApplied;
        app.PostingUrl = dto.PostingUrl;
        app.Notes = dto.Notes;
        app.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(app);
    }

    public async Task<bool> DeleteAsync(int userId, int applicationId)
    {
        var app = await _db.JobApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

        if (app is null) return false;

        _db.JobApplications.Remove(app);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ApplicationStatsDto> GetStatsAsync(int userId)
    {
        var apps = await _db.JobApplications
            .Where(a => a.UserId == userId)
            .ToListAsync();

        return new ApplicationStatsDto(
            Total: apps.Count,
            Applied: apps.Count(a => a.Status == ApplicationStatus.Applied),
            Interviewing: apps.Count(a => a.Status == ApplicationStatus.Interviewing),
            Offer: apps.Count(a => a.Status == ApplicationStatus.Offer),
            Rejected: apps.Count(a => a.Status == ApplicationStatus.Rejected)
        );
    }

    private static JobApplicationDto ToDto(JobApplication a) => new(
        a.Id, a.Company, a.Role, a.Status, a.DateApplied,
        a.PostingUrl, a.Notes, a.CreatedAt, a.UpdatedAt
    );
}
