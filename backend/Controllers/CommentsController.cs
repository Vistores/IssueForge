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
[Route("api/issues/{issueId:int}/comments")]
public class CommentsController(AppDbContext db, CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetComments(int issueId)
    {
        var teamId = await currentUser.GetActiveTeamIdAsync();
        if (teamId is null)
        {
            return Forbid();
        }

        if (!await db.Issues.AnyAsync(issue => issue.Id == issueId && issue.Project != null && issue.Project.TeamId == teamId))
        {
            return NotFound();
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
        var teamId = await currentUser.GetActiveTeamIdAsync();
        if (teamId is null)
        {
            return Forbid();
        }

        var issue = await db.Issues
            .Include(issue => issue.Project)
            .FirstOrDefaultAsync(issue => issue.Id == issueId && issue.Project != null && issue.Project.TeamId == teamId);

        if (issue is null)
        {
            return NotFound();
        }

        var comment = new Comment
        {
            IssueId = issueId,
            Text = dto.Text.Trim(),
            Author = string.IsNullOrWhiteSpace(dto.Author) ? "QA Tester" : dto.Author.Trim(),
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
        var teamId = await currentUser.GetActiveTeamIdAsync();
        if (teamId is null)
        {
            return Forbid();
        }

        var comment = await db.Comments
            .Include(comment => comment.Issue)
            .ThenInclude(issue => issue!.Project)
            .FirstOrDefaultAsync(comment => comment.Id == commentId && comment.IssueId == issueId);

        if (comment is null || comment.Issue?.Project?.TeamId != teamId)
        {
            return NotFound();
        }

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
