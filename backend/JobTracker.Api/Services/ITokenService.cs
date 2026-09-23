using JobTracker.Api.Models;

namespace JobTracker.Api.Services;

public interface ITokenService
{
    string CreateToken(User user, TimeSpan expiresIn);
}
