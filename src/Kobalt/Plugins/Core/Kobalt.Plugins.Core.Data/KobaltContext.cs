using System.Runtime.CompilerServices;
using Kobalt.Plugins.Core.Data.Entities;
using Kobalt.Shared.DatabaseConverters;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Remora.Rest.Core;

[assembly: InternalsVisibleTo("Kobalt.Core")]
[assembly: InternalsVisibleTo("Kobalt.Plugins.Core.Data.Tests")]

namespace Kobalt.Plugins.Core.Data;

public class KobaltContext : DbContext
{
    internal const string Schema = "kobalt_core";
    
    public DbSet<User> Users { get; set; } = default!;
    public DbSet<Guild> Guilds { get; set; } = default!;
    public DbSet<LogChannel> LogChannels { get; set; } = default!;


    public KobaltContext(DbContextOptions<KobaltContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KobaltContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Snowflake>().HaveConversion(typeof(SnowflakeConverter));
        configurationBuilder.Properties<Snowflake?>().HaveConversion(typeof(NullableSnowflakeConverter));

        configurationBuilder.Properties<Offset>().HaveConversion(typeof(OffsetConverter));
        configurationBuilder.Properties<Offset?>().HaveConversion(typeof(NullableOffsetConverter));

        base.ConfigureConventions(configurationBuilder);
    }
}
