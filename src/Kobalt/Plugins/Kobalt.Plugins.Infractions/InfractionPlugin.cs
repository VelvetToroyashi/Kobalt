using Kobalt.Plugins.Infractions;
using Kobalt.Plugins.Infractions.Commands;
using Kobalt.Plugins.Infractions.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
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

    public Result ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient("Infractions", (s, c) => c.BaseAddress = new(s.GetService<IConfiguration>()!["Plugins:Infractions:ApiUrl"]!));
        serviceCollection.AddSingleton<InfractionAPIService>();

        serviceCollection.AddCommandTree().WithCommandGroup<KickCommand>();

        return Result.FromSuccess();
    }

    public async ValueTask<Result> InitializeAsync(IServiceProvider serviceProvider, CancellationToken ct = default) => default;
}
