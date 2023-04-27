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

    public async ValueTask<Result> InitializeAsync(IServiceProvider serviceProvider, CancellationToken ct = default) => default;


}
