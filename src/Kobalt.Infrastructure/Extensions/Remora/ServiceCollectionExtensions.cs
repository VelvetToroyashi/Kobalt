using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.Interactivity;
using Remora.Discord.Interactivity.Extensions;

namespace Kobalt.Infrastructure.Extensions.Remora;

/// <summary>
/// Extensions for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
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
