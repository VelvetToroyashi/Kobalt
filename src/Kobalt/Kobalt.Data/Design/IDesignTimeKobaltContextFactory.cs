using Kobalt.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kobalt.Data.Design;

public class IDesignTimeKobaltContextFactory : IDesignTimeDbContextFactory<KobaltContext>
{
    public KobaltContext CreateDbContext(string[] args) 
        => new ServiceCollection()
           .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddUserSecrets<KobaltContext>().Build())
           .AddDbContextFactory<KobaltContext>("Kobalt")
           .BuildServiceProvider()
           .GetRequiredService<IDbContextFactory<KobaltContext>>()
           .CreateDbContext();
}
