using Kobalt.Plugins.RoleMenus;
using Microsoft.Extensions.DependencyInjection;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: RemoraPlugin(typeof(RoleMenuPlugin))]
namespace Kobalt.Plugins.RoleMenus;

public class RoleMenuPlugin : PluginDescriptor, IMigratablePlugin
{
    public override string Name => "RoleMenus";
    public override string Description => "Adds role-menu functionality to Kobalt.";
    public Task<Result> MigrateAsync(IServiceProvider serviceProvider, CancellationToken ct = new CancellationToken()) => throw new NotImplementedException();
}