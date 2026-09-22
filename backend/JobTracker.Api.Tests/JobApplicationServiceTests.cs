using JobTracker.Api.Data;
using JobTracker.Api.Dtos;
using JobTracker.Api.Models;
using JobTracker.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace JobTracker.Api.Tests;

public class JobApplicationServiceTests
{
    // Each test gets its own fresh, isolated in-memory database (named
    // by a random GUID) so tests never interfere with each other, even
    // when run in parallel.
    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_PersistsApplication_ForCorrectUser()
    {
        await using var db = CreateDbContext();
        var service = new JobApplicationService(db);

        var dto = new CreateJobApplicationDto(
            "Acme Corp", "Junior Developer", ApplicationStatus.Applied,
            DateTime.UtcNow, "https://acme.example/jobs/1", "Referred by a friend");

        var created = await service.CreateAsync(userId: 1, dto);

        Assert.Equal("Acme Corp", created.Company);
        Assert.Equal(ApplicationStatus.Applied, created.Status);

        var stored = await db.JobApplications.SingleAsync();
        Assert.Equal(1, stored.UserId);
    }

    [Fact]
    public async Task GetAllForUserAsync_OnlyReturnsThatUsersApplications()
    {
        await using var db = CreateDbContext();
        var service = new JobApplicationService(db);

        await service.CreateAsync(userId: 1, new CreateJobApplicationDto(
            "Company A", "Role A", ApplicationStatus.Applied, DateTime.UtcNow, null, null));
        await service.CreateAsync(userId: 2, new CreateJobApplicationDto(
            "Company B", "Role B", ApplicationStatus.Applied, DateTime.UtcNow, null, null));

        var user1Apps = await service.GetAllForUserAsync(userId: 1);

        Assert.Single(user1Apps);
        Assert.Equal("Company A", user1Apps[0].Company);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenApplicationBelongsToAnotherUser()
    {
        await using var db = CreateDbContext();
        var service = new JobApplicationService(db);

        var created = await service.CreateAsync(userId: 1, new CreateJobApplicationDto(
            "Company A", "Role A", ApplicationStatus.Applied, DateTime.UtcNow, null, null));

        // A different user trying to fetch user 1's application by ID
        // must get nothing back — this is the core of the "one user
        // can't see another user's data" guarantee.
        var result = await service.GetByIdAsync(userId: 2, created.Id);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_ChangesFields_AndUpdatesTimestamp()
    {
        await using var db = CreateDbContext();
        var service = new JobApplicationService(db);

        var created = await service.CreateAsync(userId: 1, new CreateJobApplicationDto(
            "Company A", "Role A", ApplicationStatus.Applied, DateTime.UtcNow, null, null));

        var updateDto = new UpdateJobApplicationDto(
            "Company A", "Role A", ApplicationStatus.Interviewing, DateTime.UtcNow, null, "Phone screen went well");

        var updated = await service.UpdateAsync(userId: 1, created.Id, updateDto);

        Assert.NotNull(updated);
        Assert.Equal(ApplicationStatus.Interviewing, updated!.Status);
        Assert.Equal("Phone screen went well", updated.Notes);
        Assert.True(updated.UpdatedAt >= updated.CreatedAt);
    }

    [Fact]
    public async Task DeleteAsync_RemovesApplication_AndReturnsTrue()
    {
        await using var db = CreateDbContext();
        var service = new JobApplicationService(db);

        var created = await service.CreateAsync(userId: 1, new CreateJobApplicationDto(
            "Company A", "Role A", ApplicationStatus.Applied, DateTime.UtcNow, null, null));

        var deleted = await service.DeleteAsync(userId: 1, created.Id);
        var stillThere = await service.GetByIdAsync(userId: 1, created.Id);

        Assert.True(deleted);
        Assert.Null(stillThere);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenApplicationDoesNotExist()
    {
        await using var db = CreateDbContext();
        var service = new JobApplicationService(db);

        var deleted = await service.DeleteAsync(userId: 1, applicationId: 999);

        Assert.False(deleted);
    }

    [Fact]
    public async Task GetStatsAsync_CountsApplicationsByStatus()
    {
        await using var db = CreateDbContext();
        var service = new JobApplicationService(db);

        await service.CreateAsync(1, new CreateJobApplicationDto("A", "R", ApplicationStatus.Applied, DateTime.UtcNow, null, null));
        await service.CreateAsync(1, new CreateJobApplicationDto("B", "R", ApplicationStatus.Applied, DateTime.UtcNow, null, null));
        await service.CreateAsync(1, new CreateJobApplicationDto("C", "R", ApplicationStatus.Interviewing, DateTime.UtcNow, null, null));
        await service.CreateAsync(1, new CreateJobApplicationDto("D", "R", ApplicationStatus.Offer, DateTime.UtcNow, null, null));

        var stats = await service.GetStatsAsync(userId: 1);

        Assert.Equal(4, stats.Total);
        Assert.Equal(2, stats.Applied);
        Assert.Equal(1, stats.Interviewing);
        Assert.Equal(1, stats.Offer);
        Assert.Equal(0, stats.Rejected);
    }
}
