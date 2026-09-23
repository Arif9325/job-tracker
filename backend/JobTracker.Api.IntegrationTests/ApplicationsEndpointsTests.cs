using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using JobTracker.Api.Dtos;
using JobTracker.Api.Models;
using Xunit;

namespace JobTracker.Api.IntegrationTests;

public class ApplicationsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly System.Text.Json.JsonSerializerOptions Json = JsonTestOptions.Default;

    public ApplicationsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Registers a fresh, unique user and returns an HttpClient already
    // carrying that user's bearer token — every test starts from a
    // clean slate and can't see another test's data.
    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto(email, "correct-horse-battery-staple"));
        var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.Token);
        return client;
    }

    [Fact]
    public async Task GetApplications_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/applications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_CreateListUpdateArchiveRestoreDelete_WorksEndToEnd()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Create
        var createDto = new CreateJobApplicationDto(
            "Acme Corp", "Backend Developer", ApplicationStatus.Applied,
            DateTime.UtcNow, "https://acme.example/jobs/1", "Referred by a friend", null);
        var createResponse = await client.PostAsJsonAsync("/api/applications", createDto, Json);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JobApplicationDto>(Json);
        Assert.NotNull(created);

        // Appears in the active list
        var listResponse = await client.GetFromJsonAsync<PagedResultDto<JobApplicationDto>>(
            "/api/applications?page=1&pageSize=10", Json);
        Assert.Single(listResponse!.Items);
        Assert.Equal(1, listResponse.TotalCount);

        // Update
        var updateDto = new UpdateJobApplicationDto(
            "Acme Corp", "Senior Backend Developer", ApplicationStatus.Interviewing,
            DateTime.UtcNow, "https://acme.example/jobs/1", "Phone screen scheduled", DateTime.UtcNow.AddDays(3));
        var updateResponse = await client.PutAsJsonAsync($"/api/applications/{created!.Id}", updateDto, Json);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<JobApplicationDto>(Json);
        Assert.Equal("Senior Backend Developer", updated!.Role);
        Assert.Equal(ApplicationStatus.Interviewing, updated.Status);

        // Archive — should disappear from the active list...
        var archiveResponse = await client.PostAsync($"/api/applications/{created.Id}/archive", null);
        Assert.Equal(HttpStatusCode.NoContent, archiveResponse.StatusCode);

        var activeAfterArchive = await client.GetFromJsonAsync<PagedResultDto<JobApplicationDto>>(
            "/api/applications?page=1&pageSize=10", Json);
        Assert.Empty(activeAfterArchive!.Items);

        // ...but still show up when explicitly asking for archived ones.
        var archivedList = await client.GetFromJsonAsync<PagedResultDto<JobApplicationDto>>(
            "/api/applications?page=1&pageSize=10&includeArchived=true", Json);
        Assert.Single(archivedList!.Items);

        // Permanent delete succeeds now that it's archived.
        var deleteResponse = await client.DeleteAsync($"/api/applications/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getAfterDelete = await client.GetAsync($"/api/applications/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getAfterDelete.StatusCode);
    }

    [Fact]
    public async Task Delete_Fails_OnAnApplicationThatIsNotArchived()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/applications", new CreateJobApplicationDto(
            "Company A", "Role A", ApplicationStatus.Applied, DateTime.UtcNow, null, null, null), Json);
        var created = await createResponse.Content.ReadFromJsonAsync<JobApplicationDto>(Json);

        // Never archived — permanent delete should be refused.
        var deleteResponse = await client.DeleteAsync($"/api/applications/{created!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task OneUsersApplications_AreInvisibleToAnotherUser()
    {
        var userA = await CreateAuthenticatedClientAsync();
        var userB = await CreateAuthenticatedClientAsync();

        var createResponse = await userA.PostAsJsonAsync("/api/applications", new CreateJobApplicationDto(
            "Secret Co", "Confidential Role", ApplicationStatus.Applied, DateTime.UtcNow, null, null, null), Json);
        var created = await createResponse.Content.ReadFromJsonAsync<JobApplicationDto>(Json);

        var userBsList = await userB.GetFromJsonAsync<PagedResultDto<JobApplicationDto>>("/api/applications", Json);
        var userBsDirectGet = await userB.GetAsync($"/api/applications/{created!.Id}");

        Assert.Empty(userBsList!.Items);
        Assert.Equal(HttpStatusCode.NotFound, userBsDirectGet.StatusCode);
    }
}
