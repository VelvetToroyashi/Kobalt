using System.Reflection;
using Kobalt.Infrastructure.Parsers;
using Kobalt.Infrastructure.Types;
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using NodaTime.TimeZones;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Interactivity;
using Remora.Discord.Interactivity.Extensions;

namespace Kobalt.Infrastructure.Extensions;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds timezone-related services.
    /// </summary>
    /// <param name="services">The service collection to add the services to.</param>
    /// <returns>The service collection to chain calls with.</returns>
    public static IServiceCollection AddOffsetServices(this IServiceCollection services)
    {
        services.AddAutocompleteProvider(typeof(TimezoneAutoCompleteProvider));
        services.AddSingleton<IDateTimeZoneSource>(TzdbDateTimeZoneSource.Default)
                .AddSingleton<IDateTimeZoneProvider, DateTimeZoneCache>();

        services.AddParser<DateTimeZoneParser>();
        services.AddParser<ZonedDateTimeParser>();

        return services;
    }
    
    /// <summary>
    /// Adds all interaction responders from the given assembly.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddInteractivityFromAssembly(this IServiceCollection serviceCollection, Assembly assembly)
    {
        var typesToAdd = assembly.ExportedTypes
            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(InteractionGroup)));

        foreach (var type in typesToAdd)
        {
            serviceCollection.AddInteractiveEntity(type);
        }    
        
        return serviceCollection;
    }
}
