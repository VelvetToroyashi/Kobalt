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
}
