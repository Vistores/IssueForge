using System.ComponentModel.DataAnnotations;

namespace GameIssueTracker.Api.Models;

public class Comment
{
    public int Id { get; set; }

    public int IssueId { get; set; }
    public Issue? Issue { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    [MaxLength(80)]
    public string Author { get; set; } = "QA Tester";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
