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
    private readonly TimeProvider _timeProvider;

    public JobApplicationService(AppDbContext db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<PagedResultDto<JobApplicationDto>> GetAllForUserAsync(
        int userId, int page, int pageSize, bool includeArchived)
    {
        var query = _db.JobApplications
            .Where(a => a.UserId == userId && a.IsArchived == includeArchived);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(a => a.DateApplied)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => ToDto(a))
            .ToListAsync();

        return new PagedResultDto<JobApplicationDto>(items, totalCount, page, pageSize);
    }

    public async Task<JobApplicationDto?> GetByIdAsync(int userId, int applicationId)
    {
        var app = await _db.JobApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId);

        return app is null ? null : ToDto(app);
    }

    public async Task<JobApplicationDto> CreateAsync(int userId, CreateJobApplicationDto dto)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var app = new JobApplication
        {
            UserId = userId,
            Company = dto.Company,
            Role = dto.Role,
            Status = dto.Status,
            DateApplied = dto.DateApplied,
            PostingUrl = dto.PostingUrl,
            Notes = dto.Notes,
            NextFollowUpDate = dto.NextFollowUpDate,
            CreatedAt = now,
            UpdatedAt = now,
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
        app.NextFollowUpDate = dto.NextFollowUpDate;
        app.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;

        await _db.SaveChangesAsync();
        return ToDto(app);
    }

    public async Task<bool> ArchiveAsync(int userId, int applicationId)
    {
        var app = await _db.JobApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId && !a.IsArchived);

        if (app is null) return false;

        app.IsArchived = true;
        app.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RestoreAsync(int userId, int applicationId)
    {
        var app = await _db.JobApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId && a.IsArchived);

        if (app is null) return false;

        app.IsArchived = false;
        app.UpdatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        await _db.SaveChangesAsync();
        return true;
    }

    // Deliberately only permits permanent deletion of an ALREADY-archived
    // application — archiving is the everyday "remove this" action;
    // permanent delete is one extra deliberate step further, from the
    // archived view, so it's hard to lose data by accident.
    public async Task<bool> DeleteAsync(int userId, int applicationId)
    {
        var app = await _db.JobApplications
            .FirstOrDefaultAsync(a => a.Id == applicationId && a.UserId == userId && a.IsArchived);

        if (app is null) return false;

        _db.JobApplications.Remove(app);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ApplicationStatsDto> GetStatsAsync(int userId)
    {
        var today = _timeProvider.GetUtcNow().UtcDateTime.Date;

        var apps = await _db.JobApplications
            .Where(a => a.UserId == userId && !a.IsArchived)
            .ToListAsync();

        var overdueFollowUps = apps.Count(a =>
            a.NextFollowUpDate.HasValue &&
            a.NextFollowUpDate.Value.Date < today &&
            a.Status != ApplicationStatus.Rejected &&
            a.Status != ApplicationStatus.Offer);

        return new ApplicationStatsDto(
            Total: apps.Count,
            Applied: apps.Count(a => a.Status == ApplicationStatus.Applied),
            Interviewing: apps.Count(a => a.Status == ApplicationStatus.Interviewing),
            Offer: apps.Count(a => a.Status == ApplicationStatus.Offer),
            Rejected: apps.Count(a => a.Status == ApplicationStatus.Rejected),
            OverdueFollowUps: overdueFollowUps
        );
    }

    private static JobApplicationDto ToDto(JobApplication a) => new(
        a.Id, a.Company, a.Role, a.Status, a.DateApplied,
        a.PostingUrl, a.Notes, a.NextFollowUpDate, a.IsArchived,
        a.CreatedAt, a.UpdatedAt
    );
}
