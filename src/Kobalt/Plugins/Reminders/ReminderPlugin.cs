using Kobalt.Plugins.Reminders.Commands;
using Kobalt.Plugins.Reminders.Services;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Rest.Json;
using Remora.Results;
using Constants = Remora.Discord.API.Constants;

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
            (services, client) => client.BaseAddress = new Uri(services.GetRequiredService<IConfiguration>()["Plugins:Reminders:ApiUrl"]!)
        );

        serviceCollection.AddAutocompleteProvider(typeof(ReminderAutoCompleteProvider));
        serviceCollection.AddSingleton<ReminderAPIService>();

        serviceCollection.AddCommandTree().WithCommandGroup<ReminderCommands>();

        var config = serviceCollection.BuildServiceProvider().GetRequiredService<IConfiguration>();
        AddRabbitMQ(serviceCollection, config!);

        return Result.FromSuccess();
    }

    public async ValueTask<Result> InitializeAsync(IServiceProvider serviceProvider, CancellationToken ct = default) => default;

    // ReSharper disable once InconsistentNaming
    void AddRabbitMQ(IServiceCollection services, IConfiguration config)
    {
        services.AddMassTransit
        (
            bus =>
            {
                bus.AddConsumer<ReminderAPIService>();
                bus.SetSnakeCaseEndpointNameFormatter();
                bus.UsingRabbitMq(Configure);
            }
        );

        void Configure(IBusRegistrationContext ctx, IRabbitMqBusFactoryConfigurator rmq)
        {
            rmq.ConfigureEndpoints(ctx);
            rmq.Host(new Uri(config.GetConnectionString("RabbitMQ")!));

            rmq.ExchangeType = ExchangeType.Direct;

            rmq.Durable = true;
            rmq.ConfigureJsonSerializerOptions
            (
                json =>
                {
                    json.Converters.Insert(0, new SnowflakeConverter(Constants.DiscordEpoch));
                    return json;
                }
            );
        }
    }
}
