using System.ComponentModel.DataAnnotations;

namespace GameIssueTracker.Api.Models;

public class Project
{
    public int Id { get; set; }

    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
