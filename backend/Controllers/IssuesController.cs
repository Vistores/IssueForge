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
public class IssuesController(AppDbContext db, CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<IssueDto>>> GetIssues(
        [FromQuery] IssueStatus? status,
        [FromQuery] IssuePriority? priority,
        [FromQuery] int? projectId,
        [FromQuery] int? assigneeId)
    {
        var activeTeamId = await currentUser.GetActiveTeamIdAsync();
        var teamIds = activeTeamId is null ? await currentUser.GetAccessibleTeamIdsAsync() : [activeTeamId.Value];
        if (teamIds.Count == 0)
        {
            return Forbid();
        }

        var query = db.Issues.Where(issue => issue.Project != null && teamIds.Contains(issue.Project.TeamId));

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

        if (assigneeId is not null)
        {
            query = query.Where(issue => issue.Assignments.Any(assignment => assignment.TeamMemberId == assigneeId));
        }

        var issues = await query
            .Include(issue => issue.Project)
            .Include(issue => issue.Comments)
            .Include(issue => issue.Attachments)
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
        var activeTeamId = await currentUser.GetActiveTeamIdAsync();
        var teamIds = activeTeamId is null ? await currentUser.GetAccessibleTeamIdsAsync() : [activeTeamId.Value];
        if (teamIds.Count == 0)
        {
            return Forbid();
        }

        var issue = await db.Issues
            .Include(issue => issue.Project)
            .Include(issue => issue.Comments)
            .Include(issue => issue.Attachments)
            .Include(issue => issue.Assignments)
            .ThenInclude(assignment => assignment.TeamMember)
            .ThenInclude(member => member!.User)
            .Where(issue => issue.Id == id && issue.Project != null && teamIds.Contains(issue.Project.TeamId))
            .FirstOrDefaultAsync();

        return issue is null ? NotFound() : Ok(ToIssueDto(issue));
    }

    [HttpPost]
    public async Task<ActionResult<IssueDto>> CreateIssue(IssueCreateDto dto)
    {
        var activeTeamId = await currentUser.GetActiveTeamIdAsync();
        var accessibleTeamIds = await currentUser.GetAccessibleTeamIdsAsync();
        var project = await db.Projects.FirstOrDefaultAsync(project => project.Id == dto.ProjectId && accessibleTeamIds.Contains(project.TeamId));
        var teamId = activeTeamId ?? project?.TeamId;
        if (teamId is null || project is null)
        {
            return Forbid();
        }

        if (!await currentUser.CanEditAsync(teamId.Value))
        {
            return Forbid();
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
        ReplaceAttachments(issue, dto.Attachments);
        await AddLog(teamId.Value, issue.Id, "Issue created", $"Created issue \"{issue.Title}\".");
        await db.SaveChangesAsync();

        var result = await db.Issues
            .Include(savedIssue => savedIssue.Project)
            .Include(savedIssue => savedIssue.Comments)
            .Include(savedIssue => savedIssue.Attachments)
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
        var issue = await db.Issues
            .Include(issue => issue.Project)
            .FirstOrDefaultAsync(issue => issue.Id == id && issue.Project != null);

        if (issue is null)
        {
            return NotFound();
        }

        var teamId = issue.Project!.TeamId;
        if (!await currentUser.IsTeamMemberAsync(teamId))
        {
            return Forbid();
        }

        if (!await currentUser.CanEditAsync(teamId) && !await currentUser.CanAssignAsync(teamId))
        {
            return Forbid();
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
        await ReplaceAssignments(issue, dto.AssignedMemberIds, teamId);
        if (dto.Attachments is not null)
        {
            await ReplaceAttachmentsAsync(issue, dto.Attachments);
        }
        await AddLog(teamId, issue.Id, "Issue updated", $"Updated issue \"{issue.Title}\".");

        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteIssue(int id)
    {
        var issue = await db.Issues
            .Include(issue => issue.Project)
            .FirstOrDefaultAsync(issue => issue.Id == id && issue.Project != null);

        if (issue is null)
        {
            return NotFound();
        }

        var teamId = issue.Project!.TeamId;
        if (!await currentUser.IsTeamMemberAsync(teamId) || !await currentUser.CanEditAsync(teamId))
        {
            return Forbid();
        }

        db.Issues.Remove(issue);
        await AddLog(teamId, issue.Id, "Issue deleted", $"Deleted issue \"{issue.Title}\".");
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

    private void ReplaceAttachments(Issue issue, IEnumerable<IssueAttachmentCreateDto>? attachments)
    {
        if (attachments is null)
        {
            return;
        }

        foreach (var attachment in attachments.Take(8))
        {
            db.IssueAttachments.Add(new IssueAttachment
            {
                IssueId = issue.Id,
                FileName = attachment.FileName.Trim(),
                ContentType = attachment.ContentType.Trim(),
                Size = attachment.Size,
                DataUrl = attachment.DataUrl,
                CreatedAt = DateTime.UtcNow
            });
        }
    }

    private async Task ReplaceAttachmentsAsync(Issue issue, IEnumerable<IssueAttachmentCreateDto> attachments)
    {
        var existing = await db.IssueAttachments.Where(attachment => attachment.IssueId == issue.Id).ToListAsync();
        db.IssueAttachments.RemoveRange(existing);
        ReplaceAttachments(issue, attachments);
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
                assignment.TeamMember == null || assignment.TeamMember.User == null ? null : assignment.TeamMember.User.AvatarUrl)),
            issue.Attachments.Select(attachment => new IssueAttachmentDto(
                attachment.Id,
                attachment.FileName,
                attachment.ContentType,
                attachment.Size,
                attachment.DataUrl,
                attachment.CreatedAt)));
    }
}
