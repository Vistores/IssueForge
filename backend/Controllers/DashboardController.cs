using IssueForge.Api.Data;
using IssueForge.Api.DTOs;
using IssueForge.Api.Models;
using IssueForge.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IssueForge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController(AppDbContext db, CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var teamId = await currentUser.GetActiveTeamIdAsync();
        if (teamId is null)
        {
            return Forbid();
        }

        var teamIssues = db.Issues.Where(issue => issue.Project != null && issue.Project.TeamId == teamId);

        var totalIssues = await teamIssues.CountAsync();
        var openIssues = await teamIssues.CountAsync(issue => issue.Status == IssueStatus.Open);
        var fixedIssues = await teamIssues.CountAsync(issue => issue.Status == IssueStatus.Fixed);
        var criticalIssues = await teamIssues.CountAsync(issue => issue.Priority == IssuePriority.Critical);

        var grouped = await teamIssues
            .GroupBy(issue => issue.Status)
            .Select(group => new StatusCountDto(group.Key.ToString(), group.Count()))
            .ToListAsync();

        var statuses = Enum.GetNames<IssueStatus>()
            .Select(status => new StatusCountDto(
                status,
                grouped.FirstOrDefault(item => item.Status == status)?.Count ?? 0));

        return Ok(new DashboardDto(totalIssues, openIssues, fixedIssues, criticalIssues, statuses));
    }
}
