using GameIssueTracker.Api.Data;
using GameIssueTracker.Api.DTOs;
using GameIssueTracker.Api.Models;
using GameIssueTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GameIssueTracker.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class IssuesController(AppDbContext db, CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<IssueDto>>> GetIssues(
        [FromQuery] IssueStatus? status,
        [FromQuery] IssuePriority? priority,
        [FromQuery] int? projectId)
    {
        var teamId = await currentUser.GetActiveTeamIdAsync();
        if (teamId is null)
        {
            return Forbid();
        }

        var query = db.Issues.Where(issue => issue.Project != null && issue.Project.TeamId == teamId);

        if (status is not null)
        {
            query = query.Where(issue => issue.Status == status);
        }

        if (priority is not null)
        {
            query = query.Where(issue => issue.Priority == priority);
        }

        if (projectId is not null)
        {
            query = query.Where(issue => issue.ProjectId == projectId);
        }

        var issues = await query
            .OrderByDescending(issue => issue.UpdatedAt)
            .Select(ToIssueDto)
            .ToListAsync();

        return Ok(issues);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<IssueDto>> GetIssue(int id)
    {
        var teamId = await currentUser.GetActiveTeamIdAsync();
        if (teamId is null)
        {
            return Forbid();
        }

        var issue = await db.Issues
            .Where(issue => issue.Id == id && issue.Project != null && issue.Project.TeamId == teamId)
            .Select(ToIssueDto)
            .FirstOrDefaultAsync();

        return issue is null ? NotFound() : Ok(issue);
    }

    [HttpPost]
    public async Task<ActionResult<IssueDto>> CreateIssue(IssueCreateDto dto)
    {
        var teamId = await currentUser.GetActiveTeamIdAsync();
        if (teamId is null)
        {
            return Forbid();
        }

        if (!await db.Projects.AnyAsync(project => project.Id == dto.ProjectId && project.TeamId == teamId))
        {
            ModelState.AddModelError(nameof(dto.ProjectId), "Selected project does not exist.");
            return ValidationProblem(ModelState);
        }

        var now = DateTime.UtcNow;
        var issue = new Issue
        {
            Title = dto.Title.Trim(),
            Description = dto.Description.Trim(),
            ProjectId = dto.ProjectId,
            Status = dto.Status,
            Priority = dto.Priority,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Issues.Add(issue);
        await db.SaveChangesAsync();

        var result = await db.Issues
            .Where(savedIssue => savedIssue.Id == issue.Id)
            .Select(ToIssueDto)
            .FirstAsync();

        return CreatedAtAction(nameof(GetIssue), new { id = issue.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateIssue(int id, IssueUpdateDto dto)
    {
        var teamId = await currentUser.GetActiveTeamIdAsync();
        if (teamId is null)
        {
            return Forbid();
        }

        var issue = await db.Issues
            .Include(issue => issue.Project)
            .FirstOrDefaultAsync(issue => issue.Id == id && issue.Project != null && issue.Project.TeamId == teamId);

        if (issue is null)
        {
            return NotFound();
        }

        if (!await db.Projects.AnyAsync(project => project.Id == dto.ProjectId && project.TeamId == teamId))
        {
            ModelState.AddModelError(nameof(dto.ProjectId), "Selected project does not exist.");
            return ValidationProblem(ModelState);
        }

        issue.Title = dto.Title.Trim();
        issue.Description = dto.Description.Trim();
        issue.ProjectId = dto.ProjectId;
        issue.Status = dto.Status;
        issue.Priority = dto.Priority;
        issue.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteIssue(int id)
    {
        var teamId = await currentUser.GetActiveTeamIdAsync();
        if (teamId is null)
        {
            return Forbid();
        }

        var issue = await db.Issues
            .Include(issue => issue.Project)
            .FirstOrDefaultAsync(issue => issue.Id == id && issue.Project != null && issue.Project.TeamId == teamId);

        if (issue is null)
        {
            return NotFound();
        }

        db.Issues.Remove(issue);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private static readonly Expression<Func<Issue, IssueDto>> ToIssueDto = issue =>
        new(
            issue.Id,
            issue.Title,
            issue.Description,
            issue.ProjectId,
            issue.Project == null ? string.Empty : issue.Project.Name,
            issue.Status,
            issue.Priority,
            issue.CreatedAt,
            issue.UpdatedAt,
            issue.Comments.Count);
}
