using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Remora.Rest.Json;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

namespace Kobalt.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a consistent logging configuration to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The configured service collection to chain calls with.</returns>
    public static IServiceCollection AddSerilogLogging(this IServiceCollection services)
    {
        services.AddLogging(ConfigureLogging);
        return services;
    }

    /// <summary>
    /// Adds RabbitMQ (via MassTransit) to the service collection.
    /// </summary>
    /// <param name="services">The services to add RabbitMQ to.</param>
    /// <param name="addConsumers">A delegate to configure the bus further, most commonly adding consumers.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddRabbitMQ(this IServiceCollection services, Action<IBusRegistrationConfigurator>? addConsumers = null)
    {
        services.AddMassTransit
        (
            bus =>
            {
                bus.SetSnakeCaseEndpointNameFormatter();
                bus.UsingRabbitMq(Configure);
                addConsumers?.Invoke(bus);
            }
        );

        void Configure(IBusRegistrationContext ctx, IRabbitMqBusFactoryConfigurator rmq)
        {
            var connString = ctx.GetRequiredService<IConfiguration>().GetConnectionString("RabbitMQ");

            if (string.IsNullOrEmpty(connString))
            {
                throw new InvalidOperationException("RabbitMQ connection string was not present in the configuration.");
            }

            rmq.ConfigureEndpoints(ctx);
            rmq.Host(new Uri(connString));

            rmq.ExchangeType = ExchangeType.Direct;

            rmq.Durable = true;
            rmq.ConfigureJsonSerializerOptions
            (
                json =>
                {
                    json.Converters.Insert(0, new SnowflakeConverter(1420070400000));
                    return json;
                }
            );
        }

        return services;
    }

    /// <summary>
    /// Adds a <see cref="AddDbContextFactory{TContext}"/> to the service collection, using the connection string from the configuration.
    /// </summary>
    /// <param name="services">The service provider to add the db context factory to.</param>
    /// <param name="connectionStringName">The name of the connection string to pull from the <see cref="IConfiguration"/>.</param>
    /// <typeparam name="TContext">The context to add.</typeparam>
    /// <returns>The service collection to chain calls with.</returns>
    public static IServiceCollection AddDbContextFactory<TContext>(this IServiceCollection services, string connectionStringName)
        where TContext : DbContext
        => services.AddPooledDbContextFactory<TContext>
        (
            (sp, db) =>
            {
                var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString(connectionStringName);
                db.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
            }
        );

    /// <summary>
    /// Configures a logging builder, adding Serilog.
    /// </summary>
    /// <param name="loggingBuilder">The builder to configure.</param>
    private static void ConfigureLogging(ILoggingBuilder loggingBuilder)
    {
        const string LogFormat = "[{@t:h:mm:ss ff tt}] [{@l:u3}] [{Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}] {@m}\n{@x}";

        Log.Logger = new LoggerConfiguration()
                     #if DEBUG
                     .MinimumLevel.Debug()
                     #endif
                     .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                     .MinimumLevel.Override("System.Net", LogEventLevel.Error)
                     .MinimumLevel.Override("Remora", LogEventLevel.Warning)
                     .MinimumLevel.Override("MassTransit", LogEventLevel.Information)
                     .WriteTo.Console(new ExpressionTemplate(LogFormat))
                     .CreateLogger();

        loggingBuilder.ClearProviders();
        loggingBuilder.AddSerilog(Log.Logger);
    }
}
