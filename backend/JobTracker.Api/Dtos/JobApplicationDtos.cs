using JobTracker.Api.Models;

namespace JobTracker.Api.Dtos;

// What we send back to the client — deliberately excludes UserId/User
// so we're not leaking internal relational details we don't need to.
public record JobApplicationDto(
    int Id,
    string Company,
    string Role,
    ApplicationStatus Status,
    DateTime DateApplied,
    string? PostingUrl,
    string? Notes,
    DateTime? NextFollowUpDate,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// What the client sends us to create a new application.
public record CreateJobApplicationDto(
    string Company,
    string Role,
    ApplicationStatus Status,
    DateTime DateApplied,
    string? PostingUrl,
    string? Notes,
    DateTime? NextFollowUpDate
);

// What the client sends us to update an existing one.
public record UpdateJobApplicationDto(
    string Company,
    string Role,
    ApplicationStatus Status,
    DateTime DateApplied,
    string? PostingUrl,
    string? Notes,
    DateTime? NextFollowUpDate
);

// Summary counts for the dashboard.
public record ApplicationStatsDto(
    int Total,
    int Applied,
    int Interviewing,
    int Offer,
    int Rejected,
    int OverdueFollowUps
);

// A single page of results, plus enough information for the client to
// build pagination controls without a separate "how many total" call.
public record PagedResultDto<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
