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
    public DbSet<IssueAssignment> IssueAssignments => Set<IssueAssignment>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

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

        modelBuilder.Entity<IssueAssignment>()
            .HasKey(assignment => new { assignment.IssueId, assignment.TeamMemberId });

        modelBuilder.Entity<IssueAssignment>()
            .HasOne(assignment => assignment.Issue)
            .WithMany(issue => issue.Assignments)
            .HasForeignKey(assignment => assignment.IssueId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<IssueAssignment>()
            .HasOne(assignment => assignment.TeamMember)
            .WithMany(member => member.IssueAssignments)
            .HasForeignKey(assignment => assignment.TeamMemberId)
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

        modelBuilder.Entity<ActivityLog>()
            .HasOne(log => log.Team)
            .WithMany(team => team.ActivityLogs)
            .HasForeignKey(log => log.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActivityLog>()
            .HasOne(log => log.Issue)
            .WithMany()
            .HasForeignKey(log => log.IssueId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
