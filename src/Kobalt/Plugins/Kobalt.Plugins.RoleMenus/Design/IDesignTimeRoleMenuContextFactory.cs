using Kobalt.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kobalt.Plugins.RoleMenus.Design;

public class IDesignTimeKobaltContextFactory : IDesignTimeDbContextFactory<RoleMenuContext>
{
    public RoleMenuContext CreateDbContext(string[] args) 
        => new ServiceCollection()
           .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddUserSecrets<RoleMenuContext>().Build())
           .AddDbContextFactory<RoleMenuContext>("Kobalt")
           .BuildServiceProvider()
           .GetRequiredService<IDbContextFactory<RoleMenuContext>>()
           .CreateDbContext();
}
