using GameIssueTracker.Api.Models;
using GameIssueTracker.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace GameIssueTracker.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, PasswordService passwords)
    {
        await EnsureTeamTablesAsync(db);

        var demoUser = await db.Users.FirstOrDefaultAsync(user => user.Email == "demo@game.local");
        if (demoUser is null)
        {
            demoUser = new AppUser
            {
                DisplayName = "Demo QA Tester",
                Email = "demo@game.local",
                PasswordHash = passwords.Hash("Demo123!")
            };

            db.Users.Add(demoUser);
            await db.SaveChangesAsync();
        }

        var team = await db.Teams.FirstOrDefaultAsync(team => team.InviteCode == "GUILD-2026");
        if (team is null)
        {
            team = new Team
            {
                Name = "QA Guild",
                InviteCode = "GUILD-2026"
            };

            db.Teams.Add(team);
            await db.SaveChangesAsync();
        }

        var legacyDemoMemberships = await db.TeamMembers
            .Where(member => member.UserId == demoUser.Id && member.Email != demoUser.Email)
            .ToListAsync();

        if (legacyDemoMemberships.Count > 0)
        {
            db.TeamMembers.RemoveRange(legacyDemoMemberships);
            await db.SaveChangesAsync();
        }

        if (!await db.TeamMembers.AnyAsync(member => member.TeamId == team.Id && member.UserId == demoUser.Id))
        {
            db.TeamMembers.Add(new TeamMember
            {
                TeamId = team.Id,
                UserId = demoUser.Id,
                DisplayName = demoUser.DisplayName,
                Email = demoUser.Email,
                Role = "Owner",
                CanEditIssues = true,
                CanAssignIssues = true,
                IssueLimit = 0
            });

            await db.SaveChangesAsync();
        }
        else
        {
            var owner = await db.TeamMembers.FirstAsync(member => member.TeamId == team.Id && member.UserId == demoUser.Id);
            owner.Role = "Owner";
            owner.CanEditIssues = true;
            owner.CanAssignIssues = true;
            owner.IssueLimit = 0;
            await db.SaveChangesAsync();
        }

        if (!await db.Projects.AnyAsync(project => project.TeamId == team.Id))
        {
            var launcher = new Project
            {
                TeamId = team.Id,
                Name = "Space Raiders",
                Description = "Arcade shooter demo project used for gameplay and UI bug tracking."
            };

            var rpg = new Project
            {
                TeamId = team.Id,
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

        var demoMemberId = await db.TeamMembers
            .Where(member => member.TeamId == team.Id && member.UserId == demoUser.Id)
            .Select(member => member.Id)
            .FirstAsync();

        if (!await db.IssueAssignments.AnyAsync())
        {
            var issueIds = await db.Issues
                .Where(issue => issue.Project != null && issue.Project.TeamId == team.Id)
                .OrderBy(issue => issue.Id)
                .Take(2)
                .Select(issue => issue.Id)
                .ToListAsync();

            db.IssueAssignments.AddRange(issueIds.Select(issueId => new IssueAssignment
            {
                IssueId = issueId,
                TeamMemberId = demoMemberId
            }));

            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureTeamTablesAsync(AppDbContext db)
    {
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Users" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY AUTOINCREMENT,
                "DisplayName" TEXT NOT NULL,
                "Email" TEXT NOT NULL,
                "PasswordHash" TEXT NOT NULL,
                "AvatarUrl" TEXT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            """);
        await TrySqlAsync(db, """ALTER TABLE "Users" ADD COLUMN "AvatarUrl" TEXT NULL;""");

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "Teams" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_Teams" PRIMARY KEY AUTOINCREMENT,
                "Name" TEXT NOT NULL,
                "InviteCode" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Teams_InviteCode" ON "Teams" ("InviteCode");
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "TeamMembers" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_TeamMembers" PRIMARY KEY AUTOINCREMENT,
                "TeamId" INTEGER NOT NULL,
                "UserId" INTEGER NOT NULL,
                "DisplayName" TEXT NOT NULL,
                "Email" TEXT NOT NULL,
                "Role" TEXT NOT NULL,
                "CanEditIssues" INTEGER NOT NULL DEFAULT 1,
                "CanAssignIssues" INTEGER NOT NULL DEFAULT 0,
                "IssueLimit" INTEGER NOT NULL DEFAULT 5,
                "JoinedAt" TEXT NOT NULL,
                CONSTRAINT "FK_TeamMembers_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE
            );
            """);

        await TrySqlAsync(db, """ALTER TABLE "Projects" ADD COLUMN "TeamId" INTEGER NOT NULL DEFAULT 1;""");
        await TrySqlAsync(db, """ALTER TABLE "TeamMembers" ADD COLUMN "UserId" INTEGER NOT NULL DEFAULT 1;""");
        await TrySqlAsync(db, """ALTER TABLE "TeamMembers" ADD COLUMN "CanEditIssues" INTEGER NOT NULL DEFAULT 1;""");
        await TrySqlAsync(db, """ALTER TABLE "TeamMembers" ADD COLUMN "CanAssignIssues" INTEGER NOT NULL DEFAULT 0;""");
        await TrySqlAsync(db, """ALTER TABLE "TeamMembers" ADD COLUMN "IssueLimit" INTEGER NOT NULL DEFAULT 5;""");

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_TeamMembers_TeamId" ON "TeamMembers" ("TeamId");
            """);

        await TrySqlAsync(db, """
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_TeamMembers_TeamId_UserId" ON "TeamMembers" ("TeamId", "UserId");
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_Projects_TeamId" ON "Projects" ("TeamId");
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "IssueAssignments" (
                "IssueId" INTEGER NOT NULL,
                "TeamMemberId" INTEGER NOT NULL,
                "AssignedAt" TEXT NOT NULL,
                CONSTRAINT "PK_IssueAssignments" PRIMARY KEY ("IssueId", "TeamMemberId"),
                CONSTRAINT "FK_IssueAssignments_Issues_IssueId" FOREIGN KEY ("IssueId") REFERENCES "Issues" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_IssueAssignments_TeamMembers_TeamMemberId" FOREIGN KEY ("TeamMemberId") REFERENCES "TeamMembers" ("Id") ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_IssueAssignments_TeamMemberId" ON "IssueAssignments" ("TeamMemberId");
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS "ActivityLogs" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_ActivityLogs" PRIMARY KEY AUTOINCREMENT,
                "TeamId" INTEGER NOT NULL,
                "IssueId" INTEGER NULL,
                "ActorMemberId" INTEGER NULL,
                "Action" TEXT NOT NULL,
                "Details" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_ActivityLogs_Teams_TeamId" FOREIGN KEY ("TeamId") REFERENCES "Teams" ("Id") ON DELETE CASCADE,
                CONSTRAINT "FK_ActivityLogs_Issues_IssueId" FOREIGN KEY ("IssueId") REFERENCES "Issues" ("Id") ON DELETE SET NULL,
                CONSTRAINT "FK_ActivityLogs_TeamMembers_ActorMemberId" FOREIGN KEY ("ActorMemberId") REFERENCES "TeamMembers" ("Id") ON DELETE SET NULL
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_ActivityLogs_TeamId" ON "ActivityLogs" ("TeamId");
            """);
    }

    private static async Task TrySqlAsync(AppDbContext db, string sql)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync(sql);
        }
        catch
        {
            // SQLite throws if a column already exists. This keeps the demo database upgrade-friendly.
        }
    }
}
