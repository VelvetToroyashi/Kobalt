using Kobalt.Plugins.Infractions;
using Microsoft.Extensions.DependencyInjection;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: RemoraPlugin(typeof(InfractionPlugin))]

namespace Kobalt.Plugins.Infractions;

public class InfractionPlugin : IPluginDescriptor
{
    public string Name => "Kobalt Infractions";
    public string Description => "Adds infractions to Kobalt.";
    public Version Version { get; } = new(0, 0, 1);
    
    public Result ConfigureServices(IServiceCollection serviceCollection) => default;

    public async ValueTask<Result> InitializeAsync(IServiceProvider serviceProvider, CancellationToken ct = default) => default;
}
