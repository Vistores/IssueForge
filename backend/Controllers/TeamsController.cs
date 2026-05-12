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
            .Include(team => team.Projects)
            .Where(team => team.Members.Any(member => member.UserId == userId))
            .OrderBy(team => team.Name)
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
            Role = "Owner"
        });

        await db.SaveChangesAsync();

        var result = await db.Teams
            .Include(savedTeam => savedTeam.Members)
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
                    member.DisplayName,
                    member.Email,
                    member.Role,
                    member.JoinedAt)));
    }
}
