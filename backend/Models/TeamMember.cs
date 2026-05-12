using System.ComponentModel.DataAnnotations;

namespace GameIssueTracker.Api.Models;

public class TeamMember
{
    public int Id { get; set; }

    public int TeamId { get; set; }
    public Team? Team { get; set; }

    public int UserId { get; set; }
    public AppUser? User { get; set; }

    [Required]
    [MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(40)]
    public string Role { get; set; } = "Member";

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
