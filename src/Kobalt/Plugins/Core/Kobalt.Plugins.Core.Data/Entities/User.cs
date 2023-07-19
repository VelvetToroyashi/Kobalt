using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Remora.Rest.Core;

namespace Kobalt.Plugins.Core.Data.Entities;

/// <summary>
/// Represents a user.
/// </summary>
[Table("users", Schema = KobaltContext.Schema)]
public class User
{
    public Snowflake ID { get; set; }
    
    public string? Timezone { get; set; }
    
    public bool DisplayTimezone { get; set; }

    public List<GuildUserJoiner> Guilds { get; set; } = new();
}

public class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(p => p.ID);
    }
}