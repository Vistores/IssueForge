using GameIssueTracker.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GameIssueTracker.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Issue> Issues => Set<Issue>();
    public DbSet<Comment> Comments => Set<Comment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
    }
}
