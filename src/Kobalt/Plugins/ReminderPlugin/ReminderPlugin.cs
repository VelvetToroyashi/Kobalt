using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReminderPlugin.Commands;
using ReminderPlugin.Services;
using Remora.Commands.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: RemoraPlugin(typeof(ReminderPlugin.ReminderPlugin))]

namespace ReminderPlugin;

public sealed class ReminderPlugin : IPluginDescriptor
{
    public string Name => "Kobalt Reminders";
    public string Description => "Adds reminders to Kobalt";
    public Version Version { get; } = new(0, 0, 1);

    public Result ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient
        (
            "Reminders",
            (services, client) => client.BaseAddress = new(services.GetRequiredService<IConfiguration>()["Plugins:Reminders:ApiUrl"]!)
        );
        
        serviceCollection.AddSingleton<ReminderAPIService>();
        serviceCollection.AddCommandTree().WithCommandGroup<ReminderCommands>();
        
        return Result.FromSuccess();
    }

    public async ValueTask<Result> InitializeAsync(IServiceProvider serviceProvider, CancellationToken ct = default) => default;

}
