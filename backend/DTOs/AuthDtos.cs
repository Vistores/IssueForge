using System.ComponentModel.DataAnnotations;

namespace IssueForge.Api.DTOs;

public record AuthStatusDto(
    bool IsAuthenticated,
    bool GoogleConfigured,
    string? Name,
    string? Email,
    int? UserId,
    string? AvatarUrl);

public class AccountUpdateDto
{
    [MaxLength(250000)]
    public string? AvatarUrl { get; set; }
}

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

public record TeamMemberDto(
    int Id,
    int UserId,
    string DisplayName,
    string Email,
    string Role,
    bool CanEditIssues,
    bool CanAssignIssues,
    int IssueLimit,
    string? AvatarUrl,
    DateTime JoinedAt);

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

public class TeamMemberUpdateDto
{
    [Required]
    [MaxLength(40)]
    public string Role { get; set; } = "Member";

    public bool CanEditIssues { get; set; } = true;
    public bool CanAssignIssues { get; set; }

    [Range(0, 100)]
    public int IssueLimit { get; set; } = 5;
}

public class TeamOwnerTransferDto
{
    [Range(1, int.MaxValue)]
    public int NewOwnerMemberId { get; set; }
}

public record MemberStatsDto(
    int MemberId,
    string DisplayName,
    string Role,
    string? AvatarUrl,
    int AssignedIssues,
    int OpenIssues,
    int FixedIssues,
    int CriticalIssues);

public record ActivityLogDto(
    int Id,
    string Action,
    string Details,
    string? ActorName,
    int? IssueId,
    string? IssueTitle,
    DateTime CreatedAt);
