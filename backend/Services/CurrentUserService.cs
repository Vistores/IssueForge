using System.Security.Claims;
using GameIssueTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GameIssueTracker.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor, AppDbContext db)
{
    public int? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }

    public int? RequestedTeamId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.Request.Headers["X-Team-Id"].FirstOrDefault();
            return int.TryParse(value, out var teamId) ? teamId : null;
        }
    }

    public async Task<bool> IsTeamMemberAsync(int teamId)
    {
        var userId = UserId;
        return userId is not null && await db.TeamMembers.AnyAsync(member => member.TeamId == teamId && member.UserId == userId);
    }

    public async Task<int?> GetCurrentMemberIdAsync(int teamId)
    {
        var userId = UserId;
        if (userId is null)
        {
            return null;
        }

        return await db.TeamMembers
            .Where(member => member.TeamId == teamId && member.UserId == userId)
            .Select(member => (int?)member.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<int?> GetActiveTeamIdAsync()
    {
        var teamId = RequestedTeamId;
        if (teamId is not null && await IsTeamMemberAsync(teamId.Value))
        {
            return teamId;
        }

        return null;
    }
}
