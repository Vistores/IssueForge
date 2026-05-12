using System.ComponentModel.DataAnnotations;

namespace GameIssueTracker.Api.DTOs;

public record AuthStatusDto(
    bool IsAuthenticated,
    bool GoogleConfigured,
    string? Name,
    string? Email,
    int? UserId);

public class RegisterDto
{
    [Required]
    [MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    [MaxLength(120)]
    public string Password { get; set; } = string.Empty;
}

public class LoginDto
{
    [Required]
    [EmailAddress]
    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Password { get; set; } = string.Empty;
}

public record TeamMemberDto(int Id, string DisplayName, string Email, string Role, DateTime JoinedAt);

public record TeamDto(int Id, string Name, string InviteCode, DateTime CreatedAt, int ProjectCount, IEnumerable<TeamMemberDto> Members);

public class TeamCreateDto
{
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;
}

public class TeamJoinDto
{
    [Required]
    [MaxLength(12)]
    public string InviteCode { get; set; } = string.Empty;
}
