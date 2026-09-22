namespace JobTracker.Api.Models;

public class JobApplication
{
    public int Id { get; set; }
    public string Company { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Applied;
    public DateTime DateApplied { get; set; }
    public string? PostingUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Foreign key — every application belongs to exactly one user.
    public int UserId { get; set; }
    public User? User { get; set; }
}
