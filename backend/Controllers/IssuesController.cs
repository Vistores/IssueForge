using GameIssueTracker.Api.Data;
using GameIssueTracker.Api.DTOs;
using GameIssueTracker.Api.Models;
using GameIssueTracker.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            .Include(issue => issue.Project)
            .Include(issue => issue.Comments)
            .Include(issue => issue.Assignments)
            .ThenInclude(assignment => assignment.TeamMember)
            .ThenInclude(member => member!.User)
            .OrderByDescending(issue => issue.UpdatedAt)
            .ToListAsync();

        return Ok(issues.Select(ToIssueDto));
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
            .Include(issue => issue.Project)
            .Include(issue => issue.Comments)
            .Include(issue => issue.Assignments)
            .ThenInclude(assignment => assignment.TeamMember)
            .ThenInclude(member => member!.User)
            .Where(issue => issue.Id == id && issue.Project != null && issue.Project.TeamId == teamId)
            .FirstOrDefaultAsync();

        return issue is null ? NotFound() : Ok(ToIssueDto(issue));
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
        await ReplaceAssignments(issue, dto.AssignedMemberIds, teamId.Value);
        await AddLog(teamId.Value, issue.Id, "Issue created", $"Created issue \"{issue.Title}\".");
        await db.SaveChangesAsync();

        var result = await db.Issues
            .Include(savedIssue => savedIssue.Project)
            .Include(savedIssue => savedIssue.Comments)
            .Include(savedIssue => savedIssue.Assignments)
            .ThenInclude(assignment => assignment.TeamMember)
            .ThenInclude(member => member!.User)
            .Where(savedIssue => savedIssue.Id == issue.Id)
            .FirstAsync();

        return CreatedAtAction(nameof(GetIssue), new { id = issue.Id }, ToIssueDto(result));
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
        await ReplaceAssignments(issue, dto.AssignedMemberIds, teamId.Value);
        await AddLog(teamId.Value, issue.Id, "Issue updated", $"Updated issue \"{issue.Title}\".");

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
        await AddLog(teamId.Value, issue.Id, "Issue deleted", $"Deleted issue \"{issue.Title}\".");
        await db.SaveChangesAsync();

        return NoContent();
    }

    private async Task ReplaceAssignments(Issue issue, IEnumerable<int> assignedMemberIds, int teamId)
    {
        var ids = assignedMemberIds.Distinct().ToList();
        var validIds = await db.TeamMembers
            .Where(member => member.TeamId == teamId && ids.Contains(member.Id))
            .Select(member => member.Id)
            .ToListAsync();

        var existing = await db.IssueAssignments
            .Where(assignment => assignment.IssueId == issue.Id)
            .ToListAsync();

        db.IssueAssignments.RemoveRange(existing.Where(assignment => !validIds.Contains(assignment.TeamMemberId)));

        foreach (var memberId in validIds.Except(existing.Select(assignment => assignment.TeamMemberId)))
        {
            db.IssueAssignments.Add(new IssueAssignment
            {
                IssueId = issue.Id,
                TeamMemberId = memberId,
                AssignedAt = DateTime.UtcNow
            });
        }
    }

    private async Task AddLog(int teamId, int? issueId, string action, string details)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            TeamId = teamId,
            IssueId = issueId,
            ActorMemberId = await currentUser.GetCurrentMemberIdAsync(teamId),
            Action = action,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static IssueDto ToIssueDto(Issue issue)
    {
        return new IssueDto(
            issue.Id,
            issue.Title,
            issue.Description,
            issue.ProjectId,
            issue.Project == null ? string.Empty : issue.Project.Name,
            issue.Status,
            issue.Priority,
            issue.CreatedAt,
            issue.UpdatedAt,
            issue.Comments.Count,
            issue.Assignments.Select(assignment => new IssueAssigneeDto(
                assignment.TeamMemberId,
                assignment.TeamMember == null ? "Team member" : assignment.TeamMember.DisplayName,
                assignment.TeamMember == null ? "Member" : assignment.TeamMember.Role,
                assignment.TeamMember == null || assignment.TeamMember.User == null ? null : assignment.TeamMember.User.AvatarUrl)));
    }
}
