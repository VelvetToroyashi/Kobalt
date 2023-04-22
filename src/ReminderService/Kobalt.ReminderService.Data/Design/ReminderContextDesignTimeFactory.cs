using Kobalt.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kobalt.ReminderService.Data.Design;

/// <summary>
/// Instantiates a <see cref="ReminderContext"/> for design-time purposes.
/// </summary>
public class ReminderContextDesignTimeFactory : IDesignTimeDbContextFactory<ReminderContext>
{
    public ReminderContext CreateDbContext(string[] args)
        =>
        new ServiceCollection()
        .AddLogging()
        .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                              .AddUserSecrets<ReminderContext>()
                              .Build())
       .AddDbContextFactory<ReminderContext>("Reminders")
       .BuildServiceProvider()
       .GetRequiredService<IDbContextFactory<ReminderContext>>()
       .CreateDbContext();
}
