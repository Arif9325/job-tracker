using JobTracker.Api.Data;
using JobTracker.Api.Dtos;
using JobTracker.Api.Models;
using JobTracker.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthController(AppDbContext db, ITokenService tokenService)
    {
        _db = db;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Email and password are required.");

        if (dto.Password.Length < 8)
            return BadRequest("Password must be at least 8 characters.");

        var emailNormalized = dto.Email.Trim().ToLowerInvariant();

        var alreadyExists = await _db.Users.AnyAsync(u => u.Email == emailNormalized);
        if (alreadyExists)
            return Conflict("An account with that email already exists.");

        var user = new User
        {
            Email = emailNormalized,
            // BCrypt automatically generates and embeds a random salt per
            // password, so two users with the same password get different
            // hashes — this is what makes rainbow-table attacks useless.
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // A fresh registration gets a short-lived token by default — the
        // person is already sitting right there; "remember me" is a
        // choice offered on the login form for returning visits instead.
        var token = _tokenService.CreateToken(user, TimeSpan.FromHours(12));
        return Ok(new AuthResponseDto(token, user.Email));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var emailNormalized = dto.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == emailNormalized);

        // Deliberately vague error message — we don't want to reveal
        // whether the email exists at all to someone guessing accounts.
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid email or password.");

        // "Remember me" controls how long the TOKEN stays valid; the
        // frontend separately controls WHERE it's stored (localStorage
        // vs sessionStorage) so it also survives closing the browser.
        // Both pieces have to agree, or a long-lived token stored only
        // per-tab would still vanish on browser close, or vice versa.
        var expiresIn = dto.RememberMe ? TimeSpan.FromDays(30) : TimeSpan.FromHours(12);
        var token = _tokenService.CreateToken(user, expiresIn);
        return Ok(new AuthResponseDto(token, user.Email));
    }
}
