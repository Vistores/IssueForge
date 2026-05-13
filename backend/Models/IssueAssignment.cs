namespace IssueForge.Api.Models;

public class IssueAssignment
{
    public int IssueId { get; set; }
    public Issue? Issue { get; set; }

    public int TeamMemberId { get; set; }
    public TeamMember? TeamMember { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
