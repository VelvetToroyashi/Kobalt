using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kobalt.ReminderService.Data.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to add services for the reminder database.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the reminder database context.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns>The service collection to chain calls with.</returns>
    public static IServiceCollection AddReminderDbContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPooledDbContextFactory<ReminderContext>(c => c.UseNpgsql(configuration.GetConnectionString("Reminders")).UseSnakeCaseNamingConvention());
        return services;
    }
}
