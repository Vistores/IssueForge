using System.ComponentModel.DataAnnotations;

namespace IssueForge.Api.Models;

public class IssueAttachment
{
    public int Id { get; set; }

    public int IssueId { get; set; }
    public Issue? Issue { get; set; }

    [Required]
    [MaxLength(180)]
    public string FileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string ContentType { get; set; } = string.Empty;

    public long Size { get; set; }

    [Required]
    [MaxLength(2000000)]
    public string DataUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
