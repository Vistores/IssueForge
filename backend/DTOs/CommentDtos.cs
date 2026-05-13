using System.ComponentModel.DataAnnotations;

namespace IssueForge.Api.DTOs;

public record CommentDto(int Id, int IssueId, string Text, string Author, DateTime CreatedAt);

public class CommentCreateDto
{
    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    [MaxLength(80)]
    public string? Author { get; set; }
}
