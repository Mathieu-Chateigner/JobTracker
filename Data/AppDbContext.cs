using Microsoft.EntityFrameworkCore;
using JobTracker.Models;

namespace JobTracker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<JobApplication> JobApplications => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobApplication>()
            .Property(j => j.SalaryExpectation)
            .HasColumnType("TEXT"); // SQLite-friendly
    }
}
