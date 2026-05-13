using System.ComponentModel.DataAnnotations;

namespace IssueForge.Api.Models;

public class AppUser
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string DisplayName { get; set; } = string.Empty;

    [Required]
    [MaxLength(180)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(260)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
}
