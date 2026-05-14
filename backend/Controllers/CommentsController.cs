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
[Route("api/issues/{issueId:int}/comments")]
public class CommentsController(AppDbContext db, CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetComments(int issueId)
    {
        var issueTeamId = await db.Issues
            .Where(issue => issue.Id == issueId && issue.Project != null)
            .Select(issue => (int?)issue.Project!.TeamId)
            .FirstOrDefaultAsync();

        if (issueTeamId is null || !await currentUser.IsTeamMemberAsync(issueTeamId.Value))
        {
            return issueTeamId is null ? NotFound() : Forbid();
        }

        var comments = await db.Comments
            .Where(comment => comment.IssueId == issueId)
            .OrderByDescending(comment => comment.CreatedAt)
            .Select(comment => new CommentDto(
                comment.Id,
                comment.IssueId,
                comment.Text,
                comment.Author,
                comment.CreatedAt))
            .ToListAsync();

        return Ok(comments);
    }

    [HttpPost]
    public async Task<ActionResult<CommentDto>> CreateComment(int issueId, CommentCreateDto dto)
    {
        var issue = await db.Issues
            .Include(issue => issue.Project)
            .FirstOrDefaultAsync(issue => issue.Id == issueId && issue.Project != null);

        if (issue is null)
        {
            return NotFound();
        }

        var teamId = issue.Project!.TeamId;
        if (!await currentUser.CanCommentAsync(teamId))
        {
            return Forbid();
        }

        var member = await currentUser.GetCurrentMemberAsync(teamId);
        var comment = new Comment
        {
            IssueId = issueId,
            Text = dto.Text.Trim(),
            Author = member?.DisplayName ?? User.Identity?.Name ?? "Team member",
            CreatedAt = DateTime.UtcNow
        };

        issue.UpdatedAt = DateTime.UtcNow;
        db.Comments.Add(comment);
        await db.SaveChangesAsync();

        var result = new CommentDto(comment.Id, comment.IssueId, comment.Text, comment.Author, comment.CreatedAt);
        return CreatedAtAction(nameof(GetComments), new { issueId }, result);
    }

    [HttpDelete("{commentId:int}")]
    public async Task<IActionResult> DeleteComment(int issueId, int commentId)
    {
        var comment = await db.Comments
            .Include(comment => comment.Issue)
            .ThenInclude(issue => issue!.Project)
            .FirstOrDefaultAsync(comment => comment.Id == commentId && comment.IssueId == issueId);

        if (comment is null || comment.Issue?.Project is null)
        {
            return NotFound();
        }

        if (!await currentUser.CanEditAsync(comment.Issue.Project.TeamId))
        {
            return Forbid();
        }

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
