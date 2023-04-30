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

public class InfractionPlugin : PluginDescriptor
{
    public override string Name => "Kobalt Infractions";
    public override string Description => "Adds infractions to Kobalt.";

    public override Result ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient
        (
            "Infractions",
            (s, c) =>
            {
                var address = s.GetService<IConfiguration>()!["Plugins:Infractions:ApiUrl"] ??
                              throw new KeyNotFoundException("The API url was not configured.");

                c.BaseAddress = new Uri(address);
            }
        );

        serviceCollection.AddSingleton<InfractionAPIService>();

        serviceCollection.AddCommandTree().WithCommandGroup<ModerationCommands>();

        return Result.FromSuccess();
    }
}
