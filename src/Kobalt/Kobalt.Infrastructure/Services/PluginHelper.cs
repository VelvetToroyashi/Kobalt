using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Remora.Plugins;
using Remora.Plugins.Services;
using Remora.Results;

namespace Kobalt.Infrastructure.Services;

/// <summary>
/// A helper class for loading and configuring plugins.
/// </summary>
[PublicAPI]
public static class PluginHelper
{
    /// <summary>
    /// Loads and configures all plugins.
    /// </summary>
    /// <param name="services">The service container to add plugins to.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    public static Result LoadPlugins(IServiceCollection services)
    {
        var assemblyFolder = Directory.GetParent(Assembly.GetEntryAssembly()!.Location)!.FullName;
        var pluginService = new PluginService(Options.Create(new PluginServiceOptions(new[] { $"{assemblyFolder}/plugins" }, false)));
        
        var pluginTree = pluginService.LoadPluginTree();
        var configureResult = pluginTree.ConfigureServices(services);

        if (!configureResult.IsSuccess)
        {
            return configureResult;
        }

        services.AddSingleton(pluginTree);
        services.AddSingleton(pluginService);

        return Result.FromSuccess();
    }
    
    /// <summary>
    /// Iniitalizes all loaded plugins.
    /// </summary>
    /// <param name="serviceProvider">The built service container to initalize plugins with.</param>
    /// <returns>A result representing the outcome of the operation.</returns>
    public static async Task<Result> InitializePluginsAsync(IServiceProvider serviceProvider)
    {
        var pluginTree = serviceProvider.GetRequiredService<PluginTree>();
        
        var initializeResult = await pluginTree.InitializeAsync(serviceProvider);

        if (!initializeResult.IsSuccess)
        {
            return initializeResult;
        }

        var migrateResult = await pluginTree.MigrateAsync(serviceProvider);

        return migrateResult;
    }
}
