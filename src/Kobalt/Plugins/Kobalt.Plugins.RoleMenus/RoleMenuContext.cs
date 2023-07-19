using System.Diagnostics;
using System.Reflection;
using Kobalt.Plugins.Core.Data;
using Kobalt.Plugins.RoleMenus.Models;
using Kobalt.Shared.DatabaseConverters;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;

namespace Kobalt.Plugins.RoleMenus;

public class RoleMenuContext : DbContext
{
    internal const string Schema = "kobalt_plugins_rolemenu";
    public DbSet<RoleMenuEntity> RoleMenus { get; set; }
    
    public RoleMenuContext(DbContextOptions<RoleMenuContext> options) : base(options) { }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema);
        
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.GetSchema() == modelBuilder.Model.GetDefaultSchema()) //if (entityType.ClrType.Assembly == executing)
            {
                continue;
            }
            
            entityType.SetIsTableExcludedFromMigrations(true);
        }
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RoleMenuContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<Snowflake>().HaveConversion(typeof(SnowflakeConverter));
        configurationBuilder.Properties<Snowflake?>().HaveConversion(typeof(NullableSnowflakeConverter));

        base.ConfigureConventions(configurationBuilder);
    }
}