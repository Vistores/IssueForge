namespace IssueForge.Api.DTOs;

public record StatusCountDto(string Status, int Count);

public record DashboardDto(
    int TotalIssues,
    int OpenIssues,
    int FixedIssues,
    int CriticalIssues,
    IEnumerable<StatusCountDto> IssuesByStatus);
