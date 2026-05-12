using System.ComponentModel.DataAnnotations;

namespace GameIssueTracker.Api.Models;

public class ActivityLog
{
    public int Id { get; set; }

    public int TeamId { get; set; }
    public Team? Team { get; set; }

    public int? IssueId { get; set; }
    public Issue? Issue { get; set; }

    public int? ActorMemberId { get; set; }
    public TeamMember? ActorMember { get; set; }

    [Required]
    [MaxLength(80)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Details { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
