using System.Runtime.CompilerServices;
using Kobalt.Data.Entities;
using Kobalt.Shared.DatabaseConverters;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Remora.Rest.Core;

[assembly: InternalsVisibleTo("Kobalt.Bot")]
[assembly: InternalsVisibleTo("Kobalt.Data.Tests")]

namespace Kobalt.Data;

public class KobaltContext : DbContext
{
    public DbSet<User> Users { get; set; } = default!;


    public KobaltContext(DbContextOptions<KobaltContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
