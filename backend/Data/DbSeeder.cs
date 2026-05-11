using GameIssueTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameIssueTracker.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Projects.AnyAsync())
        {
            return;
        }

        var launcher = new Project
        {
            Name = "Space Raiders",
            Description = "Arcade shooter demo project used for gameplay and UI bug tracking."
        };

        var rpg = new Project
        {
            Name = "Dungeon Tools",
            Description = "Internal tools for RPG quest editing and QA validation."
        };

        db.Projects.AddRange(launcher, rpg);
        await db.SaveChangesAsync();

        var issues = new List<Issue>
        {
            new()
            {
                Title = "Player ship clips through asteroid edge",
                Description = "Collision feels too small on large asteroids in level 2.",
                ProjectId = launcher.Id,
                Status = IssueStatus.Open,
                Priority = IssuePriority.High
            },
            new()
            {
                Title = "Settings menu does not save audio volume",
                Description = "Volume slider resets to 100% after restarting the game.",
                ProjectId = launcher.Id,
                Status = IssueStatus.InProgress,
                Priority = IssuePriority.Medium
            },
            new()
            {
                Title = "Quest export fails when NPC name is empty",
                Description = "The editor should show validation instead of throwing an export error.",
                ProjectId = rpg.Id,
                Status = IssueStatus.Fixed,
                Priority = IssuePriority.Critical
            }
        };

        db.Issues.AddRange(issues);
        await db.SaveChangesAsync();

        db.Comments.AddRange(
            new Comment { IssueId = issues[0].Id, Author = "Olena", Text = "Reproduced with keyboard and gamepad controls." },
            new Comment { IssueId = issues[2].Id, Author = "Max", Text = "Fixed by adding editor-side validation." }
        );

        await db.SaveChangesAsync();
    }
}
