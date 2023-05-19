using Kobalt.Phishing.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Phishing.Data;

/// <summary>
/// Represents the database context for the phishing database.
/// </summary>
public class PhishingContext : DbContext
{
    public DbSet<SuspiciousAvatar> SuspiciousAvatars { get; set; } = null!;

    public DbSet<SuspiciousUsername> SuspiciousUsernames { get; set; } = null!;

    public PhishingContext(DbContextOptions<PhishingContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("kobalt_phishing");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PhishingContext).Assembly);
    }
}
