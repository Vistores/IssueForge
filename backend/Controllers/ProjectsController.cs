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
public class ProjectsController(AppDbContext db, CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
    {
        var teamId = await currentUser.GetActiveTeamIdAsync();
        if (teamId is null)
        {
            return Forbid();
        }

        var projects = await db.Projects
            .Where(project => project.TeamId == teamId)
            .OrderBy(project => project.Name)
            .Select(project => new ProjectDto(
                project.Id,
                project.Name,
                project.Description,
                project.Issues.Count))
            .ToListAsync();

        return Ok(projects);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDto>> GetProject(int id)
    {
        var teamId = await currentUser.GetActiveTeamIdAsync();
        if (teamId is null)
        {
            return Forbid();
        }

        var project = await db.Projects
            .Where(project => project.Id == id && project.TeamId == teamId)
            .Select(project => new ProjectDto(
                project.Id,
                project.Name,
                project.Description,
                project.Issues.Count))
            .FirstOrDefaultAsync();

        return project is null ? NotFound() : Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> CreateProject(ProjectCreateDto dto)
    {
        var teamId = await currentUser.GetActiveTeamIdAsync();
        if (teamId is null)
        {
            return Forbid();
        }

        var project = new Project
        {
            TeamId = teamId.Value,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim()
        };

        db.Projects.Add(project);
        db.ActivityLogs.Add(new ActivityLog
        {
            TeamId = teamId.Value,
            ActorMemberId = await currentUser.GetCurrentMemberIdAsync(teamId.Value),
            Action = "Project created",
            Details = $"Created project \"{project.Name}\".",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var result = new ProjectDto(project.Id, project.Name, project.Description, 0);
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProject(int id, ProjectUpdateDto dto)
    {
        var teamId = await currentUser.GetActiveTeamIdAsync();
        if (teamId is null)
        {
            return Forbid();
        }

        var project = await db.Projects.FirstOrDefaultAsync(project => project.Id == id && project.TeamId == teamId);

        if (project is null)
        {
            return NotFound();
        }

        project.Name = dto.Name.Trim();
        project.Description = dto.Description?.Trim();
        db.ActivityLogs.Add(new ActivityLog
        {
            TeamId = teamId.Value,
            ActorMemberId = await currentUser.GetCurrentMemberIdAsync(teamId.Value),
            Action = "Project updated",
            Details = $"Updated project \"{project.Name}\".",
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var teamId = await currentUser.GetActiveTeamIdAsync();
        if (teamId is null)
        {
            return Forbid();
        }

        var project = await db.Projects.FirstOrDefaultAsync(project => project.Id == id && project.TeamId == teamId);

        if (project is null)
        {
            return NotFound();
        }

        db.Projects.Remove(project);
        db.ActivityLogs.Add(new ActivityLog
        {
            TeamId = teamId.Value,
            ActorMemberId = await currentUser.GetCurrentMemberIdAsync(teamId.Value),
            Action = "Project deleted",
            Details = $"Deleted project \"{project.Name}\".",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return NoContent();
    }
}
