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
    string? Notes
);

// What the client sends us to update an existing one.
public record UpdateJobApplicationDto(
    string Company,
    string Role,
    ApplicationStatus Status,
    DateTime DateApplied,
    string? PostingUrl,
    string? Notes
);

// Summary counts for the dashboard.
public record ApplicationStatsDto(
    int Total,
    int Applied,
    int Interviewing,
    int Offer,
    int Rejected
);
