using GameIssueTracker.Api.Data;
using GameIssueTracker.Api.DTOs;
using GameIssueTracker.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameIssueTracker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
    {
        var projects = await db.Projects
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
        var project = await db.Projects
            .Where(project => project.Id == id)
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
        var project = new Project
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim()
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var result = new ProjectDto(project.Id, project.Name, project.Description, 0);
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateProject(int id, ProjectUpdateDto dto)
    {
        var project = await db.Projects.FindAsync(id);

        if (project is null)
        {
            return NotFound();
        }

        project.Name = dto.Name.Trim();
        project.Description = dto.Description?.Trim();

        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteProject(int id)
    {
        var project = await db.Projects.FindAsync(id);

        if (project is null)
        {
            return NotFound();
        }

        db.Projects.Remove(project);
        await db.SaveChangesAsync();

        return NoContent();
    }
}
