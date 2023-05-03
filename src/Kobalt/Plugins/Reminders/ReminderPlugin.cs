using Kobalt.Plugins.Reminders.Commands;
using Kobalt.Plugins.Reminders.Services;
using Kobalt.Shared.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: RemoraPlugin(typeof(Kobalt.Plugins.Reminders.ReminderPlugin))]

namespace Kobalt.Plugins.Reminders;

public sealed class ReminderPlugin : PluginDescriptor
{
    public override string Name => "Kobalt Reminders";
    public override string Description => "Adds reminders to Kobalt";

    public override Result ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddHttpClient
        (
            "Reminders",
            (s, c) =>
            {
                var address = s.GetService<IConfiguration>()!["Plugins:Reminders:ApiUrl"] ??
                              throw new KeyNotFoundException("The API url was not configured.");

                c.BaseAddress = new Uri(address);
            }
        );

        serviceCollection.AddRabbitMQ();
        serviceCollection.AddSingleton<ReminderAPIService>();
        serviceCollection.AddCommandTree().WithCommandGroup<ReminderCommands>();
        serviceCollection.AddAutocompleteProvider(typeof(ReminderAutoCompleteProvider));

        return Result.FromSuccess();
    }
}
