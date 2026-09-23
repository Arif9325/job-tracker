namespace JobTracker.Api.Dtos;

public record RegisterDto(string Email, string Password);

public record LoginDto(string Email, string Password, bool RememberMe = false);

public record AuthResponseDto(string Token, string Email);
