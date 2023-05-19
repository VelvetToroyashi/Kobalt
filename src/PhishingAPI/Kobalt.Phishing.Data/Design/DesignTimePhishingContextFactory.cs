using Kobalt.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kobalt.Phishing.Data.Design;

public class DesignTimePhishingContextFactory : IDesignTimeDbContextFactory<PhishingContext>
{
    public PhishingContext CreateDbContext(string[] args)
        =>
        new ServiceCollection()
       .AddLogging()
       .AddSingleton<IConfiguration>(new ConfigurationBuilder().AddUserSecrets<PhishingContext>().Build())
       .AddDbContextFactory<PhishingContext>("Phishing")
       .BuildServiceProvider()
       .GetRequiredService<IDbContextFactory<PhishingContext>>()
       .CreateDbContext();
}
