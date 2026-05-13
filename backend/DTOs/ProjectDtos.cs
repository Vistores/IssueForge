using System.ComponentModel.DataAnnotations;

namespace IssueForge.Api.DTOs;

public record ProjectDto(int Id, string Name, string? Description, int IssueCount);

public class ProjectCreateDto
{
    [Required]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class ProjectUpdateDto : ProjectCreateDto;
