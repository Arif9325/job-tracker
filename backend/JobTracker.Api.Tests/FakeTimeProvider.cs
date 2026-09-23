namespace JobTracker.Api.Tests;

// A minimal controllable clock for tests: nothing here ever races
// against the real wall clock, and lockout-expiry logic can be tested
// by advancing time deterministically instead of Thread.Sleep-ing for
// real minutes.
public class FakeTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FakeTimeProvider(DateTimeOffset start)
    {
        _now = start;
    }

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan span) => _now += span;
}
