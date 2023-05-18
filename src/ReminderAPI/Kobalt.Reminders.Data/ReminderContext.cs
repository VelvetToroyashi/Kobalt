using System.Runtime.CompilerServices;
using Kobalt.Reminders.Data.Entities;
using Microsoft.EntityFrameworkCore;

[assembly: InternalsVisibleTo("Kobalt.Reminders.API")]
namespace Kobalt.Reminders.Data;

/// <summary>
/// Represents a database context for the reminder service.
/// </summary>
public class ReminderContext : DbContext
{
    /// <summary>
    /// Instantiates a new <see cref="ReminderContext"/>.
    /// </summary>
    /// <param name="options">The options for the DbContext.</param>
    public ReminderContext(DbContextOptions<ReminderContext> options) : base(options) { }

    public DbSet<ReminderEntity> Reminders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("kobalt_reminders");
        base.OnModelCreating(modelBuilder);
    }

}
