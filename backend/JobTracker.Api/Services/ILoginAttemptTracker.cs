namespace JobTracker.Api.Services;

public interface ILoginAttemptTracker
{
    // Returns true if this email is currently locked out, and how much
    // longer until it isn't.
    bool IsLockedOut(string email, out TimeSpan retryAfter);
    void RecordFailure(string email);
    void RecordSuccess(string email);
}
