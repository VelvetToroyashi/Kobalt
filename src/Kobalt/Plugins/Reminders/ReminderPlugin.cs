using Kobalt.Plugins.Reminders.Commands;
using Kobalt.Plugins.Reminders.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: RemoraPlugin(typeof(Kobalt.Plugins.Reminders.ReminderPlugin))]

namespace Kobalt.Plugins.Reminders;

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

        serviceCollection.AddAutocompleteProvider(typeof(ReminderAutoCompleteProvider));
        serviceCollection.AddSingleton<ReminderAPIService>();
        serviceCollection.AddHostedService(s => s.GetRequiredService<ReminderAPIService>());
        
        serviceCollection.AddCommandTree().WithCommandGroup<ReminderCommands>();
        
        return Result.FromSuccess();
    }

    public async ValueTask<Result> InitializeAsync(IServiceProvider serviceProvider, CancellationToken ct = default) => default;

}
