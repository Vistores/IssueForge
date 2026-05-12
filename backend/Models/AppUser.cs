using System.ComponentModel.DataAnnotations;

namespace GameIssueTracker.Api.Models;

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

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TeamMember> TeamMemberships { get; set; } = new List<TeamMember>();
}
