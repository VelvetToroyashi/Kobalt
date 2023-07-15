using Kobalt.Plugins.Core.Data;
using Kobalt.Plugins.RoleMenus.Models;
using Kobalt.Shared.DatabaseConverters;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;

namespace Kobalt.Plugins.RoleMenus;

public class RoleMenuContext : DbContext
{
    public DbSet<RoleMenuEntity> RoleMenus { get; set; }
    
    public RoleMenuContext(DbContextOptions<RoleMenuContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("kobalt_plugins_rolemenu");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KobaltContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Snowflake>().HaveConversion(typeof(SnowflakeConverter));
        configurationBuilder.Properties<Snowflake?>().HaveConversion(typeof(NullableSnowflakeConverter));

        base.ConfigureConventions(configurationBuilder);
    }
}