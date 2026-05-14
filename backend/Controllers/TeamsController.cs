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
public class TeamsController(AppDbContext db, CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TeamDto>>> GetTeams()
    {
        var userId = currentUser.UserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var teams = await db.Teams
            .Include(team => team.Members)
            .ThenInclude(member => member.User)
            .Include(team => team.Projects)
            .Where(team => team.Members.Any(member => member.UserId == userId))
            .OrderByDescending(team => team.Projects.Count)
            .ThenBy(team => team.Name)
            .ToListAsync();

        return Ok(teams.Select(ToTeamDto));
    }

    [HttpPost]
    public async Task<ActionResult<TeamDto>> CreateTeam(TeamCreateDto dto)
    {
        var user = await db.Users.FindAsync(currentUser.UserId);
        if (user is null)
        {
            return Unauthorized();
        }

        var team = new Team
        {
            Name = dto.Name.Trim(),
            InviteCode = await GenerateInviteCode()
        };

        db.Teams.Add(team);
        await db.SaveChangesAsync();

        db.TeamMembers.Add(new TeamMember
        {
            TeamId = team.Id,
            UserId = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Role = "Owner",
            CanEditIssues = true,
            CanAssignIssues = true,
            IssueLimit = 0
        });

        await db.SaveChangesAsync();

        var result = await db.Teams
            .Include(savedTeam => savedTeam.Members)
            .ThenInclude(member => member.User)
            .Include(savedTeam => savedTeam.Projects)
            .FirstAsync(savedTeam => savedTeam.Id == team.Id);

        return CreatedAtAction(nameof(GetTeams), ToTeamDto(result));
    }

    [HttpPost("join")]
    public async Task<ActionResult<TeamDto>> JoinTeam(TeamJoinDto dto)
    {
        var user = await db.Users.FindAsync(currentUser.UserId);
        if (user is null)
        {
            return Unauthorized();
        }

        var normalizedCode = dto.InviteCode.Trim().ToUpperInvariant();
        var team = await db.Teams
            .Include(team => team.Members)
            .ThenInclude(member => member.User)
            .Include(team => team.Projects)
            .FirstOrDefaultAsync(team => team.InviteCode == normalizedCode);

        if (team is null)
        {
            return NotFound(new { message = "Team invite code was not found." });
        }

        var alreadyMember = team.Members.Any(member => member.UserId == user.Id);

        if (!alreadyMember)
        {
            team.Members.Add(new TeamMember
            {
                UserId = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Role = "Member"
            });

            await db.SaveChangesAsync();
        }

        return Ok(ToTeamDto(team));
    }

    [HttpPut("{teamId:int}/members/{memberId:int}")]
    public async Task<IActionResult> UpdateMember(int teamId, int memberId, TeamMemberUpdateDto dto)
    {
        if (!await currentUser.IsOwnerAsync(teamId))
        {
            return Forbid();
        }

        var member = await db.TeamMembers.FirstOrDefaultAsync(member => member.Id == memberId && member.TeamId == teamId);
        if (member is null)
        {
            return NotFound();
        }

        var currentMemberId = await currentUser.GetCurrentMemberIdAsync(teamId);
        if (member.Id == currentMemberId)
        {
            return BadRequest(new { message = "Use owner transfer instead of editing your own permissions." });
        }

        var role = NormalizeRole(dto.Role);
        if (role == "Owner")
        {
            return BadRequest(new { message = "Use the owner transfer action to make another member an owner." });
        }

        member.Role = role;
        member.CanEditIssues = role is "Owner" or "Manager" || (role == "Member" && dto.CanEditIssues);
        member.CanAssignIssues = role is "Owner" or "Manager" || (role == "Member" && dto.CanAssignIssues);
        member.IssueLimit = dto.IssueLimit;
        await AddLog(teamId, "Member permissions", $"{member.DisplayName} is now {member.Role}.");
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("{teamId:int}/transfer-owner")]
    public async Task<IActionResult> TransferOwner(int teamId, TeamOwnerTransferDto dto)
    {
        if (!await currentUser.IsOwnerAsync(teamId))
        {
            return Forbid();
        }

        var currentMemberId = await currentUser.GetCurrentMemberIdAsync(teamId);
        if (currentMemberId == dto.NewOwnerMemberId)
        {
            return BadRequest(new { message = "You already own this team." });
        }

        var members = await db.TeamMembers.Where(member => member.TeamId == teamId).ToListAsync();
        var currentOwner = members.FirstOrDefault(member => member.Id == currentMemberId);
        var newOwner = members.FirstOrDefault(member => member.Id == dto.NewOwnerMemberId);

        if (currentOwner is null || newOwner is null)
        {
            return NotFound();
        }

        currentOwner.Role = "Manager";
        currentOwner.CanEditIssues = true;
        currentOwner.CanAssignIssues = true;
        newOwner.Role = "Owner";
        newOwner.CanEditIssues = true;
        newOwner.CanAssignIssues = true;
        await AddLog(teamId, "Owner transferred", $"{newOwner.DisplayName} is now the team owner.");
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{teamId:int}")]
    public async Task<IActionResult> DeleteTeam(int teamId)
    {
        if (!await currentUser.IsOwnerAsync(teamId))
        {
            return Forbid();
        }

        var team = await db.Teams.FirstOrDefaultAsync(team => team.Id == teamId);
        if (team is null)
        {
            return NotFound();
        }

        db.Teams.Remove(team);
        await db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{teamId:int}/stats")]
    public async Task<ActionResult<IEnumerable<MemberStatsDto>>> GetStats(int teamId)
    {
        if (!await currentUser.IsTeamMemberAsync(teamId))
        {
            return Forbid();
        }

        var stats = await db.TeamMembers
            .Where(member => member.TeamId == teamId)
            .Select(member => new MemberStatsDto(
                member.Id,
                member.DisplayName,
                member.Role,
                member.User == null ? null : member.User.AvatarUrl,
                member.IssueAssignments.Count,
                member.IssueAssignments.Count(assignment => assignment.Issue != null && assignment.Issue.Status != IssueStatus.Fixed && assignment.Issue.Status != IssueStatus.Rejected),
                member.IssueAssignments.Count(assignment => assignment.Issue != null && assignment.Issue.Status == IssueStatus.Fixed),
                member.IssueAssignments.Count(assignment => assignment.Issue != null && assignment.Issue.Priority == IssuePriority.Critical)))
            .ToListAsync();

        return Ok(stats);
    }

    [HttpGet("{teamId:int}/activity")]
    public async Task<ActionResult<IEnumerable<ActivityLogDto>>> GetActivity(int teamId)
    {
        if (!await currentUser.IsTeamMemberAsync(teamId))
        {
            return Forbid();
        }

        var logs = await db.ActivityLogs
            .Where(log => log.TeamId == teamId)
            .OrderByDescending(log => log.CreatedAt)
            .Take(50)
            .Select(log => new ActivityLogDto(
                log.Id,
                log.Action,
                log.Details,
                log.ActorMember == null ? null : log.ActorMember.DisplayName,
                log.IssueId,
                log.Issue == null ? null : log.Issue.Title,
                log.CreatedAt))
            .ToListAsync();

        return Ok(logs);
    }

    private async Task<string> GenerateInviteCode()
    {
        string code;

        do
        {
            code = $"GUILD-{Random.Shared.Next(1000, 9999)}";
        }
        while (await db.Teams.AnyAsync(team => team.InviteCode == code));

        return code;
    }

    private static TeamDto ToTeamDto(Team team)
    {
        return new TeamDto(
            team.Id,
            team.Name,
            team.InviteCode,
            team.CreatedAt,
            team.Projects.Count,
            team.Members
                .OrderBy(member => member.JoinedAt)
                .Select(member => new TeamMemberDto(
                    member.Id,
                    member.UserId,
                    member.DisplayName,
                    member.Email,
                    member.Role,
                    member.CanEditIssues,
                    member.CanAssignIssues,
                    member.IssueLimit,
                    member.User == null ? null : member.User.AvatarUrl,
                    member.JoinedAt)));
    }

    private async Task AddLog(int teamId, string action, string details)
    {
        db.ActivityLogs.Add(new ActivityLog
        {
            TeamId = teamId,
            ActorMemberId = await currentUser.GetCurrentMemberIdAsync(teamId),
            Action = action,
            Details = details,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string NormalizeRole(string role)
    {
        return role.Trim() switch
        {
            "Owner" => "Owner",
            "Manager" => "Manager",
            "Viewer" => "Viewer",
            "Commenter" => "Commenter",
            _ => "Member"
        };
    }
}
