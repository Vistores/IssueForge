using GameIssueTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameIssueTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>()
            .HasIndex(user => user.Email)
            .IsUnique();

        modelBuilder.Entity<Team>()
            .HasMany(team => team.Projects)
            .WithOne(project => project.Team)
            .HasForeignKey(project => project.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Project>()
            .HasMany(project => project.Issues)
            .WithOne(issue => issue.Project)
            .HasForeignKey(issue => issue.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Issue>()
            .HasMany(issue => issue.Comments)
            .WithOne(comment => comment.Issue)
            .HasForeignKey(comment => comment.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Issue>()
            .Property(issue => issue.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Issue>()
            .Property(issue => issue.Priority)
            .HasConversion<string>();

        modelBuilder.Entity<Team>()
            .HasIndex(team => team.InviteCode)
            .IsUnique();

        modelBuilder.Entity<Team>()
            .HasMany(team => team.Members)
            .WithOne(member => member.Team)
            .HasForeignKey(member => member.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AppUser>()
            .HasMany(user => user.TeamMemberships)
            .WithOne(member => member.User)
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TeamMember>()
            .HasIndex(member => new { member.TeamId, member.UserId })
            .IsUnique();
    }
}
