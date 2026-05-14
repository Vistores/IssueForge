using IssueForge.Api.Models;
using IssueForge.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace IssueForge.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, PasswordService passwords)
    {
        await EnsureTeamTablesAsync(db);

        var legacyDemoUser = await db.Users.FirstOrDefaultAsync(user => user.Email == "demo@game.local");
        var currentDemoUser = await db.Users.FirstOrDefaultAsync(user => user.Email == "alex.morgan@issueforge.local");
        if (legacyDemoUser is not null && currentDemoUser is null)
        {
            legacyDemoUser.Email = "alex.morgan@issueforge.local";
            await db.SaveChangesAsync();
        }

        var demoUser = await UpsertUserAsync(db, passwords, "alex.morgan@issueforge.local", "Alex Morgan", "AM", "#2563eb", "#eff6ff");
        var productLead = await UpsertUserAsync(db, passwords, "maya.product@issueforge.local", "Maya Chen", "MC", "#7c3aed", "#f5f3ff");
        var engineer = await UpsertUserAsync(db, passwords, "ivan.ops@issueforge.local", "Ivan Petrenko", "IP", "#059669", "#ecfdf5");
        var support = await UpsertUserAsync(db, passwords, "sofia.support@issueforge.local", "Sofia Reyes", "SR", "#ea580c", "#fff7ed");
        var observer = await UpsertUserAsync(db, passwords, "nora.audit@issueforge.local", "Nora Blake", "NB", "#475569", "#f8fafc");

        var team = await db.Teams.FirstOrDefaultAsync(team => team.InviteCode == "TEAM-2026")
            ?? await db.Teams.FirstOrDefaultAsync(team => team.InviteCode == "GUILD-2026");

        if (team is null)
        {
            team = new Team { Name = "Operations Hub", InviteCode = "TEAM-2026" };
            db.Teams.Add(team);
        }
        else
        {
            team.Name = "Operations Hub";
            team.InviteCode = "TEAM-2026";
        }

        await db.SaveChangesAsync();

        var staleEmptyTeams = await db.Teams
            .Include(team => team.Projects)
            .Where(existingTeam => existingTeam.Id != team.Id
                && existingTeam.Projects.Count == 0
                && (existingTeam.Name.Contains("Guild") || existingTeam.InviteCode.StartsWith("GUILD-")))
            .ToListAsync();

        for (var index = 0; index < staleEmptyTeams.Count; index++)
        {
            var staleTeam = staleEmptyTeams[index];
            staleTeam.Name = staleEmptyTeams.Count == 1 ? "Sandbox Workspace" : $"Sandbox Workspace {index + 1}";
            staleTeam.InviteCode = $"TEAM-{9000 + staleTeam.Id}";
        }

        if (staleEmptyTeams.Count > 0)
        {
            await db.SaveChangesAsync();
        }

        var staleActivity = await db.ActivityLogs
            .Where(log => log.TeamId == team.Id
                && (log.Details.Contains("Player ship")
                    || log.Details.Contains("Settings menu")
                    || log.Details.Contains("Quest export")
                    || log.Details.Contains("NPC")
                    || log.Details.Contains("asteroid")))
            .ToListAsync();

        if (staleActivity.Count > 0)
        {
            db.ActivityLogs.RemoveRange(staleActivity);
            await db.SaveChangesAsync();
        }

        var legacyDemoMemberships = await db.TeamMembers
            .Where(member => member.UserId == demoUser.Id && member.Email != demoUser.Email)
            .ToListAsync();

        if (legacyDemoMemberships.Count > 0)
        {
            foreach (var membership in legacyDemoMemberships)
            {
                membership.Email = demoUser.Email;
                membership.DisplayName = demoUser.DisplayName;
            }

            await db.SaveChangesAsync();
        }

        var owner = await UpsertMemberAsync(db, team, demoUser, "Owner", true, true, 0);
        var manager = await UpsertMemberAsync(db, team, productLead, "Manager", true, true, 8);
        var member = await UpsertMemberAsync(db, team, engineer, "Member", true, false, 6);
        var commenter = await UpsertMemberAsync(db, team, support, "Commenter", false, false, 0);
        await UpsertMemberAsync(db, team, observer, "Viewer", false, false, 0);

        var portal = await UpsertProjectAsync(db, team, "Customer Portal", "Public account area for customer onboarding, profile updates and service requests.", ["Space Raiders"]);
        var billing = await UpsertProjectAsync(db, team, "Billing Operations", "Invoices, payment retries, tax fields and internal finance workflows.", []);
        var internalTools = await UpsertProjectAsync(db, team, "Internal Tools", "Back-office utilities used by support, operations and product teams.", ["Dungeon Tools"]);
        var mobile = await UpsertProjectAsync(db, team, "Mobile Experience", "Responsive flows, mobile navigation and attachment-heavy issue reports.", []);

        var issues = new List<Issue>
        {
            await UpsertIssueAsync(db, portal, "Checkout fails when VAT field is left empty", "Steps to reproduce:\n1. Open checkout as a business customer.\n2. Leave the VAT field empty.\n3. Submit the form.\n\nExpected: the form shows a clear validation message.\nActual: the request fails and the user sees a generic error banner.", IssueStatus.InProgress, IssuePriority.Critical, ["Quest export fails when NPC name is empty"]),
            await UpsertIssueAsync(db, internalTools, "CSV export loses applied filters", "The exported file includes all records instead of the filtered result set. This makes reconciliation slower for operations because the user has to repeat the filtering manually in a spreadsheet.", IssueStatus.Open, IssuePriority.High, []),
            await UpsertIssueAsync(db, billing, "Duplicate email after payment status update", "Customers receive two nearly identical emails when an invoice moves from Pending to Paid. The notification log shows two jobs scheduled within the same minute.", IssueStatus.Open, IssuePriority.Medium, ["Settings menu does not save audio volume"]),
            await UpsertIssueAsync(db, mobile, "Mobile filter bar wraps into the board columns", "On narrow screens the filters remain usable, but the spacing between the filter card and board columns is too tight. The layout needs one more visual break before the kanban columns start.", IssueStatus.Fixed, IssuePriority.Low, ["Player ship clips through asteroid edge"]),
            await UpsertIssueAsync(db, portal, "Long comment text overflows inside the activity panel", "A single long URL or pasted support transcript should wrap inside the comment body and keep the action buttons inside the panel.", IssueStatus.Fixed, IssuePriority.High, []),
            await UpsertIssueAsync(db, internalTools, "Role selector allows confusing self-permission edits", "Owners should not accidentally reduce their own access from the team settings modal. Ownership transfer should stay explicit and use a confirmation step.", IssueStatus.Rejected, IssuePriority.Medium, [])
        };

        await UpsertAssignmentAsync(db, issues[0], owner);
        await UpsertAssignmentAsync(db, issues[0], manager);
        await UpsertAssignmentAsync(db, issues[1], member);
        await UpsertAssignmentAsync(db, issues[2], manager);
        await UpsertAssignmentAsync(db, issues[3], member);
        await UpsertAssignmentAsync(db, issues[4], owner);

        await UpsertCommentAsync(db, issues[0], "Maya Chen", "Confirmed in production logs. The API returns a 500 before validation middleware can format the response.");
        await UpsertCommentAsync(db, issues[0], "Ivan Petrenko", "Patch is ready for review.\n\nI added a guard for empty tax metadata and covered the regression with an integration test.");
        await UpsertCommentAsync(db, issues[1], "Sofia Reyes", "Support has three customer reports with the same export mismatch. Priority should stay high until finance signs off.");
        await UpsertCommentAsync(db, issues[4], "Alex Morgan", "Regression check: pasted transcripts, long URLs and multi-paragraph comments now stay inside the card boundaries.");

        await UpsertLogAsync(db, team, issues[0], manager, "Status changed", "Moved checkout validation issue to InProgress.");
        await UpsertLogAsync(db, team, issues[1], commenter, "Comment added", "Added support context to the CSV export issue.");
        await UpsertLogAsync(db, team, issues[3], member, "Issue fixed", "Adjusted mobile filter spacing and marked the issue fixed.");
        await UpsertLogAsync(db, team, issues[4], owner, "Assignment updated", "Assigned long-comment layout regression to Alex Morgan.");

        await db.SaveChangesAsync();
    }

    private static async Task<AppUser> UpsertUserAsync(
        AppDbContext db,
        PasswordService passwords,
        string email,
        string displayName,
        string initials,
        string accent,
        string background)
    {
        var user = await db.Users.FirstOrDefaultAsync(user => user.Email == email);
        if (user is null)
        {
            user = new AppUser
            {
                Email = email,
                PasswordHash = passwords.Hash("Demo123!")
            };
            db.Users.Add(user);
        }

        user.DisplayName = displayName;
        user.AvatarUrl ??= AvatarDataUrl(initials, accent, background);
        await db.SaveChangesAsync();
        return user;
    }

    private static async Task<TeamMember> UpsertMemberAsync(
        AppDbContext db,
        Team team,
        AppUser user,
        string role,
        bool canEdit,
        bool canAssign,
        int issueLimit)
    {
        var member = await db.TeamMembers.FirstOrDefaultAsync(member => member.TeamId == team.Id && member.UserId == user.Id);
        if (member is null)
        {
            member = new TeamMember { TeamId = team.Id, UserId = user.Id };
            db.TeamMembers.Add(member);
        }

        member.DisplayName = user.DisplayName;
        member.Email = user.Email;
        member.Role = role;
        member.CanEditIssues = canEdit;
        member.CanAssignIssues = canAssign;
        member.IssueLimit = issueLimit;
        await db.SaveChangesAsync();
        return member;
    }

    private static async Task<Project> UpsertProjectAsync(AppDbContext db, Team team, string name, string description, string[] previousNames)
    {
        var project = await db.Projects.FirstOrDefaultAsync(project => project.TeamId == team.Id && project.Name == name);
        if (project is null && previousNames.Length > 0)
        {
            project = await db.Projects.FirstOrDefaultAsync(project => project.TeamId == team.Id && previousNames.Contains(project.Name));
        }

        if (project is null)
        {
            project = new Project { TeamId = team.Id };
            db.Projects.Add(project);
        }

        project.Name = name;
        project.Description = description;
        await db.SaveChangesAsync();
        return project;
    }

    private static async Task<Issue> UpsertIssueAsync(
        AppDbContext db,
        Project project,
        string title,
        string description,
        IssueStatus status,
        IssuePriority priority,
        string[] previousTitles)
    {
        var issue = await db.Issues.FirstOrDefaultAsync(issue => issue.Title == title && issue.Project != null && issue.Project.TeamId == project.TeamId);
        if (issue is null && previousTitles.Length > 0)
        {
            issue = await db.Issues.FirstOrDefaultAsync(issue => previousTitles.Contains(issue.Title) && issue.Project != null && issue.Project.TeamId == project.TeamId);
        }

        if (issue is null)
        {
            issue = new Issue
            {
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            };
            db.Issues.Add(issue);
        }

        issue.ProjectId = project.Id;
        issue.Title = title;
        issue.Description = description;
        issue.Status = status;
        issue.Priority = priority;
        issue.UpdatedAt = DateTime.UtcNow.AddMinutes(-10);
        await db.SaveChangesAsync();
        return issue;
    }

    private static async Task UpsertAssignmentAsync(AppDbContext db, Issue issue, TeamMember member)
    {
        if (!await db.IssueAssignments.AnyAsync(assignment => assignment.IssueId == issue.Id && assignment.TeamMemberId == member.Id))
        {
            db.IssueAssignments.Add(new IssueAssignment { IssueId = issue.Id, TeamMemberId = member.Id });
            await db.SaveChangesAsync();
        }
    }

    private static async Task UpsertCommentAsync(AppDbContext db, Issue issue, string author, string text)
    {
        if (!await db.Comments.AnyAsync(comment => comment.IssueId == issue.Id && comment.Author == author && comment.Text == text))
        {
            db.Comments.Add(new Comment { IssueId = issue.Id, Author = author, Text = text, CreatedAt = DateTime.UtcNow.AddHours(-2) });
            await db.SaveChangesAsync();
        }
    }

    private static async Task UpsertLogAsync(AppDbContext db, Team team, Issue issue, TeamMember actor, string action, string details)
    {
        if (!await db.ActivityLogs.AnyAsync(log => log.TeamId == team.Id && log.IssueId == issue.Id && log.Action == action && log.Details == details))
        {
            db.ActivityLogs.Add(new ActivityLog
            {
                TeamId = team.Id,
                IssueId = issue.Id,
                ActorMemberId = actor.Id,
                Action = action,
                Details = details,
                CreatedAt = DateTime.UtcNow.AddMinutes(-30)
            });
            await db.SaveChangesAsync();
        }
    }

    private static string AvatarDataUrl(string initials, string accent, string background)
    {
        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 96 96">
              <rect width="96" height="96" rx="48" fill="{background}"/>
              <circle cx="48" cy="48" r="43" fill="{accent}" opacity="0.14"/>
              <text x="48" y="55" text-anchor="middle" font-family="Inter, Arial, sans-serif" font-size="28" font-weight="800" fill="{accent}">{initials}</text>
            </svg>
            """;

        return $"data:image/svg+xml,{Uri.EscapeDataString(svg)}";
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
            CREATE TABLE IF NOT EXISTS "IssueAttachments" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_IssueAttachments" PRIMARY KEY AUTOINCREMENT,
                "IssueId" INTEGER NOT NULL,
                "FileName" TEXT NOT NULL,
                "ContentType" TEXT NOT NULL,
                "Size" INTEGER NOT NULL,
                "DataUrl" TEXT NOT NULL,
                "CreatedAt" TEXT NOT NULL,
                CONSTRAINT "FK_IssueAttachments_Issues_IssueId" FOREIGN KEY ("IssueId") REFERENCES "Issues" ("Id") ON DELETE CASCADE
            );
            """);

        await db.Database.ExecuteSqlRawAsync("""
            CREATE INDEX IF NOT EXISTS "IX_IssueAttachments_IssueId" ON "IssueAttachments" ("IssueId");
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
            // SQLite throws if a column already exists. This keeps local database upgrades friendly.
        }
    }
}
