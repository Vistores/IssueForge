using GameIssueTracker.Api.Data;
using GameIssueTracker.Api.DTOs;
using GameIssueTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameIssueTracker.Api.Controllers;

[ApiController]
[Route("api/issues/{issueId:int}/comments")]
public class CommentsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CommentDto>>> GetComments(int issueId)
    {
        if (!await db.Issues.AnyAsync(issue => issue.Id == issueId))
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
        var issue = await db.Issues.FindAsync(issueId);

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
        var comment = await db.Comments
            .FirstOrDefaultAsync(comment => comment.Id == commentId && comment.IssueId == issueId);

        if (comment is null)
        {
            return NotFound();
        }

        db.Comments.Remove(comment);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
