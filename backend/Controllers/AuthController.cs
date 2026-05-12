using System.Security.Claims;
using GameIssueTracker.Api.Data;
using GameIssueTracker.Api.DTOs;
using GameIssueTracker.Api.Models;
using GameIssueTracker.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameIssueTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IConfiguration configuration, AppDbContext db, PasswordService passwords) : ControllerBase
{
    private const string FrontendUrl = "http://localhost:4200/team";

    [HttpGet("status")]
    public ActionResult<AuthStatusDto> GetStatus()
    {
        var googleConfigured = !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"])
            && !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]);

        return Ok(new AuthStatusDto(
            User.Identity?.IsAuthenticated == true,
            googleConfigured,
            User.Identity?.Name,
            User.FindFirstValue(ClaimTypes.Email),
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null));
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthStatusDto>> Register(RegisterDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(user => user.Email == email))
        {
            ModelState.AddModelError(nameof(dto.Email), "A user with this email already exists.");
            return ValidationProblem(ModelState);
        }

        var user = new AppUser
        {
            DisplayName = dto.DisplayName.Trim(),
            Email = email,
            PasswordHash = passwords.Hash(dto.Password),
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        await SignInUser(user);

        return Ok(new AuthStatusDto(true, IsGoogleConfigured(), user.DisplayName, user.Email, user.Id));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthStatusDto>> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(user => user.Email == email);

        if (user is null || !passwords.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        await SignInUser(user);
        return Ok(new AuthStatusDto(true, IsGoogleConfigured(), user.DisplayName, user.Email, user.Id));
    }

    [HttpGet("google")]
    public IActionResult GoogleLogin([FromQuery] string? inviteCode)
    {
        if (!IsGoogleConfigured())
        {
            return BadRequest(new { message = "Google OAuth is not configured. Add Authentication:Google:ClientId and ClientSecret." });
        }

        var redirect = string.IsNullOrWhiteSpace(inviteCode)
            ? FrontendUrl
            : $"{FrontendUrl}?inviteCode={Uri.EscapeDataString(inviteCode)}";

        var properties = new AuthenticationProperties { RedirectUri = redirect };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private async Task SignInUser(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }

    private bool IsGoogleConfigured()
    {
        return !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"])
            && !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientSecret"]);
    }
}
