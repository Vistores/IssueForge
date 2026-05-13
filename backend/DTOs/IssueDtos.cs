using System.ComponentModel.DataAnnotations;
using IssueForge.Api.Models;

namespace IssueForge.Api.DTOs;

public record IssueDto(
    int Id,
    string Title,
    string Description,
    int ProjectId,
    string ProjectName,
    IssueStatus Status,
    IssuePriority Priority,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int CommentCount,
    IEnumerable<IssueAssigneeDto> Assignees);

public record IssueAssigneeDto(int MemberId, string DisplayName, string Role, string? AvatarUrl);

public class IssueCreateDto
{
    [Required]
    [MaxLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ProjectId { get; set; }

    public IssueStatus Status { get; set; } = IssueStatus.Open;
    public IssuePriority Priority { get; set; } = IssuePriority.Medium;

    public List<int> AssignedMemberIds { get; set; } = [];
}

public class IssueUpdateDto : IssueCreateDto;
