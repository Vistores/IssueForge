using GameIssueTracker.Api.Data;
using GameIssueTracker.Api.DTOs;
using GameIssueTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameIssueTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetDashboard()
    {
        var totalIssues = await db.Issues.CountAsync();
        var openIssues = await db.Issues.CountAsync(issue => issue.Status == IssueStatus.Open);
        var fixedIssues = await db.Issues.CountAsync(issue => issue.Status == IssueStatus.Fixed);
        var criticalIssues = await db.Issues.CountAsync(issue => issue.Priority == IssuePriority.Critical);

        var grouped = await db.Issues
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
