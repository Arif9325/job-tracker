using JobTracker.Api.Services;
using Xunit;

namespace JobTracker.Api.Tests;

public class LoginAttemptTrackerTests
{
    private static (LoginAttemptTracker tracker, FakeTimeProvider time) CreateTracker()
    {
        var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero));
        return (new LoginAttemptTracker(time), time);
    }

    [Fact]
    public void IsLockedOut_False_ForAnEmailThatHasNeverFailed()
    {
        var (tracker, _) = CreateTracker();

        var lockedOut = tracker.IsLockedOut("nobody@example.com", out _);

        Assert.False(lockedOut);
    }

    [Fact]
    public void IsLockedOut_False_AfterFourFailures()
    {
        var (tracker, _) = CreateTracker();

        for (var i = 0; i < 4; i++) tracker.RecordFailure("target@example.com");

        Assert.False(tracker.IsLockedOut("target@example.com", out _));
    }

    [Fact]
    public void IsLockedOut_True_AfterFiveFailures()
    {
        var (tracker, _) = CreateTracker();

        for (var i = 0; i < 5; i++) tracker.RecordFailure("target@example.com");

        Assert.True(tracker.IsLockedOut("target@example.com", out var retryAfter));
        Assert.True(retryAfter > TimeSpan.Zero);
    }

    [Fact]
    public void RecordSuccess_ClearsTheFailureCount()
    {
        var (tracker, _) = CreateTracker();

        for (var i = 0; i < 4; i++) tracker.RecordFailure("target@example.com");
        tracker.RecordSuccess("target@example.com");

        // If the count hadn't been cleared, one more failure here would
        // be the 5th and would trigger a lockout.
        tracker.RecordFailure("target@example.com");

        Assert.False(tracker.IsLockedOut("target@example.com", out _));
    }

    [Fact]
    public void IsLockedOut_BecomesFalse_OnceTheLockoutWindowPasses()
    {
        var (tracker, time) = CreateTracker();

        for (var i = 0; i < 5; i++) tracker.RecordFailure("target@example.com");
        Assert.True(tracker.IsLockedOut("target@example.com", out _));

        time.Advance(TimeSpan.FromMinutes(15).Add(TimeSpan.FromSeconds(1)));

        Assert.False(tracker.IsLockedOut("target@example.com", out _));
    }

    [Fact]
    public void Lockout_IsPerEmail_DoesNotAffectOtherAccounts()
    {
        var (tracker, _) = CreateTracker();

        for (var i = 0; i < 5; i++) tracker.RecordFailure("target@example.com");

        Assert.True(tracker.IsLockedOut("target@example.com", out _));
        Assert.False(tracker.IsLockedOut("someone-else@example.com", out _));
    }

    [Fact]
    public void Email_IsNormalized_CaseAndWhitespaceInsensitive()
    {
        var (tracker, _) = CreateTracker();

        for (var i = 0; i < 5; i++) tracker.RecordFailure("Target@Example.com  ");

        Assert.True(tracker.IsLockedOut("  target@example.com", out _));
    }
}
