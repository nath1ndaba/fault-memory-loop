using FaultMemoryLoop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FaultMemoryLoop.Infrastructure.Persistence;

/// <summary>
/// The only entity here is Employee — the knowledge store (resolved jobs)
/// deliberately stays in Markdown files, not this database, per
/// docs/design.md. This DbContext exists specifically for the login table,
/// not as a general-purpose data store.
/// </summary>
public class FaultMemoryLoopDbContext(DbContextOptions<FaultMemoryLoopDbContext> options) : DbContext(options)
{
    public DbSet<Employee> Employees => Set<Employee>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
        });
    }
}
