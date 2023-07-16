using Kobalt.Plugins.RoleMenus;
using Kobalt.Plugins.RoleMenus.Commands;
using Kobalt.Plugins.RoleMenus.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.Interactivity.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: RemoraPlugin(typeof(RoleMenuPlugin))]
namespace Kobalt.Plugins.RoleMenus;

public class RoleMenuPlugin : PluginDescriptor, IMigratablePlugin
{
    public override string Name => "RoleMenus";
    public override string Description => "Adds role-menu functionality to Kobalt.";

    public override Result ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<RoleMenuService>()
                .AddInteractionGroup<RoleMenuComponentCommands>();
        
        return Result.FromSuccess();
    }

    public async Task<Result> MigrateAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        var db = serviceProvider.GetRequiredService<RoleMenuContext>();
        await db.Database.MigrateAsync(ct);
        
        return Result.FromSuccess();
    } 
}