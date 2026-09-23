using System.Net;
using System.Net.Http.Json;
using JobTracker.Api.Dtos;
using Xunit;

namespace JobTracker.Api.IntegrationTests;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    [Fact]
    public async Task Register_WithValidData_ReturnsTokenAndEmail()
    {
        var email = UniqueEmail();

        var response = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterDto(email, "correct-horse-battery-staple"));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal(email, body.Email);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto(email, "correct-horse-battery-staple"));

        var second = await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto(email, "a-different-password"));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsToken()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto(email, "correct-horse-battery-staple"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto(email, "correct-horse-battery-staple", RememberMe: false));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(body?.Token);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto(email, "correct-horse-battery-staple"));

        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto(email, "totally-wrong-password", RememberMe: false));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_LocksOutAfterFiveFailedAttempts()
    {
        var email = UniqueEmail();
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto(email, "correct-horse-battery-staple"));

        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/auth/login", new LoginDto(email, "wrong-password", RememberMe: false));
        }

        // The 6th attempt should be rejected by the lockout itself, even
        // though we now use the CORRECT password — proving the lockout
        // engages regardless of whether this specific attempt would have
        // otherwise succeeded.
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginDto(email, "correct-horse-battery-staple", RememberMe: false));

        Assert.Equal((HttpStatusCode)429, response.StatusCode);
    }
}
