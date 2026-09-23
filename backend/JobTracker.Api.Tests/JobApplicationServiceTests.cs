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

    private static JobApplicationService CreateService(AppDbContext db, TimeProvider? timeProvider = null) =>
        new(db, timeProvider ?? TimeProvider.System);

    private static CreateJobApplicationDto MakeCreateDto(
        string company = "Company A",
        string role = "Role A",
        ApplicationStatus status = ApplicationStatus.Applied,
        DateTime? dateApplied = null,
        DateTime? nextFollowUpDate = null) =>
        new(company, role, status, dateApplied ?? DateTime.UtcNow, null, null, nextFollowUpDate);

    [Fact]
    public async Task CreateAsync_PersistsApplication_ForCorrectUser()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var dto = MakeCreateDto("Acme Corp", "Junior Developer");
        var created = await service.CreateAsync(userId: 1, dto);

        Assert.Equal("Acme Corp", created.Company);
        Assert.Equal(ApplicationStatus.Applied, created.Status);
        Assert.False(created.IsArchived);

        var stored = await db.JobApplications.SingleAsync();
        Assert.Equal(1, stored.UserId);
    }

    [Fact]
    public async Task GetAllForUserAsync_OnlyReturnsThatUsersApplications()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.CreateAsync(userId: 1, MakeCreateDto("Company A", "Role A"));
        await service.CreateAsync(userId: 2, MakeCreateDto("Company B", "Role B"));

        var result = await service.GetAllForUserAsync(userId: 1, page: 1, pageSize: 10, includeArchived: false);

        Assert.Single(result.Items);
        Assert.Equal("Company A", result.Items[0].Company);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetAllForUserAsync_Paginates_Correctly()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        for (var i = 0; i < 15; i++)
        {
            await service.CreateAsync(1, MakeCreateDto($"Company {i}", "Role", dateApplied: DateTime.UtcNow.AddDays(-i)));
        }

        var page1 = await service.GetAllForUserAsync(1, page: 1, pageSize: 10, includeArchived: false);
        var page2 = await service.GetAllForUserAsync(1, page: 2, pageSize: 10, includeArchived: false);

        Assert.Equal(15, page1.TotalCount);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(5, page2.Items.Count);
        // Newest (least days ago) first, so page 1's last item should be
        // strictly newer than page 2's first item — confirms no overlap
        // and no gap between pages.
        Assert.True(page1.Items[^1].DateApplied >= page2.Items[0].DateApplied);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenApplicationBelongsToAnotherUser()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var created = await service.CreateAsync(userId: 1, MakeCreateDto());

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
        var service = CreateService(db);

        var created = await service.CreateAsync(userId: 1, MakeCreateDto());
        var followUp = DateTime.UtcNow.AddDays(7);

        var updateDto = new UpdateJobApplicationDto(
            "Company A", "Role A", ApplicationStatus.Interviewing, DateTime.UtcNow, null,
            "Phone screen went well", followUp);

        var updated = await service.UpdateAsync(userId: 1, created.Id, updateDto);

        Assert.NotNull(updated);
        Assert.Equal(ApplicationStatus.Interviewing, updated!.Status);
        Assert.Equal("Phone screen went well", updated.Notes);
        Assert.Equal(followUp.Date, updated.NextFollowUpDate!.Value.Date);
        Assert.True(updated.UpdatedAt >= updated.CreatedAt);
    }

    [Fact]
    public async Task ArchiveAsync_HidesFromDefaultList_ButKeepsTheRecord()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var created = await service.CreateAsync(1, MakeCreateDto());

        var archived = await service.ArchiveAsync(1, created.Id);
        var activeList = await service.GetAllForUserAsync(1, 1, 10, includeArchived: false);
        var archivedList = await service.GetAllForUserAsync(1, 1, 10, includeArchived: true);

        Assert.True(archived);
        Assert.Empty(activeList.Items);
        Assert.Single(archivedList.Items);
    }

    [Fact]
    public async Task RestoreAsync_MovesApplicationBackToActiveList()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var created = await service.CreateAsync(1, MakeCreateDto());
        await service.ArchiveAsync(1, created.Id);

        var restored = await service.RestoreAsync(1, created.Id);
        var activeList = await service.GetAllForUserAsync(1, 1, 10, includeArchived: false);

        Assert.True(restored);
        Assert.Single(activeList.Items);
    }

    [Fact]
    public async Task DeleteAsync_Fails_WhenApplicationIsNotArchivedYet()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var created = await service.CreateAsync(1, MakeCreateDto());

        // Permanent delete is only allowed on an already-archived
        // application — this is the "hard to lose data by accident"
        // guarantee the archive-first design is meant to provide.
        var deleted = await service.DeleteAsync(1, created.Id);

        Assert.False(deleted);
        Assert.NotNull(await service.GetByIdAsync(1, created.Id));
    }

    [Fact]
    public async Task DeleteAsync_Succeeds_OnceArchived()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var created = await service.CreateAsync(1, MakeCreateDto());
        await service.ArchiveAsync(1, created.Id);

        var deleted = await service.DeleteAsync(1, created.Id);

        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(1, created.Id));
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenApplicationDoesNotExist()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var deleted = await service.DeleteAsync(userId: 1, applicationId: 999);

        Assert.False(deleted);
    }

    [Fact]
    public async Task GetStatsAsync_CountsApplicationsByStatus_ExcludingArchived()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        await service.CreateAsync(1, MakeCreateDto("A", "R", ApplicationStatus.Applied));
        await service.CreateAsync(1, MakeCreateDto("B", "R", ApplicationStatus.Applied));
        await service.CreateAsync(1, MakeCreateDto("C", "R", ApplicationStatus.Interviewing));
        var offer = await service.CreateAsync(1, MakeCreateDto("D", "R", ApplicationStatus.Offer));
        await service.ArchiveAsync(1, offer.Id);

        var stats = await service.GetStatsAsync(userId: 1);

        // The archived Offer shouldn't count toward anything.
        Assert.Equal(3, stats.Total);
        Assert.Equal(2, stats.Applied);
        Assert.Equal(1, stats.Interviewing);
        Assert.Equal(0, stats.Offer);
        Assert.Equal(0, stats.Rejected);
    }

    [Fact]
    public async Task GetStatsAsync_CountsOverdueFollowUps_ButNotForRejectedOrOffer()
    {
        var fakeTime = new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        await using var db = CreateDbContext();
        var service = CreateService(db, fakeTime);

        var overdueYesterday = await service.CreateAsync(1, MakeCreateDto("A", "R", ApplicationStatus.Applied));
        await service.UpdateAsync(1, overdueYesterday.Id, new UpdateJobApplicationDto(
            "A", "R", ApplicationStatus.Applied, DateTime.UtcNow, null, null,
            new DateTime(2026, 6, 14)));

        // Overdue by date, but Rejected — shouldn't count as an
        // actionable follow-up anymore.
        var overdueButRejected = await service.CreateAsync(1, MakeCreateDto("B", "R", ApplicationStatus.Rejected));
        await service.UpdateAsync(1, overdueButRejected.Id, new UpdateJobApplicationDto(
            "B", "R", ApplicationStatus.Rejected, DateTime.UtcNow, null, null,
            new DateTime(2026, 6, 1)));

        var notYetDue = await service.CreateAsync(1, MakeCreateDto("C", "R", ApplicationStatus.Applied));
        await service.UpdateAsync(1, notYetDue.Id, new UpdateJobApplicationDto(
            "C", "R", ApplicationStatus.Applied, DateTime.UtcNow, null, null,
            new DateTime(2026, 6, 20)));

        var stats = await service.GetStatsAsync(userId: 1);

        Assert.Equal(1, stats.OverdueFollowUps);
    }
}
