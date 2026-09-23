using System.Collections.Concurrent;

namespace JobTracker.Api.Services;

// A simple brute-force guard: after 5 failed logins for the same email
// within the lockout window, further attempts are rejected for 15
// minutes regardless of whether the password is now correct. This is
// deliberately per-email rather than per-IP, since IPs are easy to
// rotate but a target email address is fixed — the same idea as the
// brute-force detection rule in the log-analyzer project, applied here
// as active prevention instead of after-the-fact detection.
//
// This is registered as a singleton, so state is in-process memory: it
// resets on every app restart/redeploy and isn't shared across multiple
// server instances. A production system handling real traffic would
// back this with something shared and persistent (Redis, a database
// table) instead — a known, deliberate tradeoff for a project this size.
public class LoginAttemptTracker : ILoginAttemptTracker
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly ConcurrentDictionary<string, AttemptRecord> _attempts = new();
    private readonly TimeProvider _timeProvider;

    public LoginAttemptTracker(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    private class AttemptRecord
    {
        public int Count;
        public DateTimeOffset? LockedUntil;
        public readonly object Lock = new();
    }

    public bool IsLockedOut(string email, out TimeSpan retryAfter)
    {
        retryAfter = TimeSpan.Zero;
        var key = Normalize(email);

        if (!_attempts.TryGetValue(key, out var record) || record.LockedUntil is not { } lockedUntil)
            return false;

        var now = _timeProvider.GetUtcNow();
        if (now >= lockedUntil)
        {
            // Lockout has expired — clear it so a fresh set of attempts
            // can start counting from zero again.
            _attempts.TryRemove(key, out _);
            return false;
        }

        retryAfter = lockedUntil - now;
        return true;
    }

    public void RecordFailure(string email)
    {
        var key = Normalize(email);
        var record = _attempts.GetOrAdd(key, _ => new AttemptRecord());

        lock (record.Lock)
        {
            record.Count++;
            if (record.Count >= MaxAttempts)
            {
                record.LockedUntil = _timeProvider.GetUtcNow().Add(LockoutDuration);
            }
        }
    }

    public void RecordSuccess(string email)
    {
        _attempts.TryRemove(Normalize(email), out _);
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
