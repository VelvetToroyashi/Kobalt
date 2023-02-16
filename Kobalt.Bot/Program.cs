using System.Reflection;
using Kobalt.Bot.Handlers;
using Kobalt.Bot.Services;
using Kobalt.Bot.Services.Discord;
using Kobalt.Infrastructure.Extensions.Remora;
using Kobalt.Infrastructure.Services.Booru;
using Kobalt.Infrastructure.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Gateway.Commands;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Extensions.Extensions;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Serilog;
using Serilog.Events;
using Serilog.Templates;

var builder = Host.CreateDefaultBuilder(args)
                  .UseConsoleLifetime();

builder.ConfigureAppConfiguration
(
    (hostContext, config) =>
    {
        config.AddJsonFile("appsettings.json", true);
        config.AddJsonFile($"appsettings.{hostContext.HostingEnvironment.EnvironmentName}.json", true);
        config.AddEnvironmentVariables();
    }
);

builder.ConfigureServices(ConfigureKobaltBotServices);

builder.ConfigureLogging(ConfigureLogging);

var host = builder.Build();

await host.RunAsync();


void ConfigureKobaltBotServices(HostBuilderContext hostBuilder, IServiceCollection services)
{
    var config = hostBuilder.Configuration.Get<KobaltConfig>()!;
    services.AddSingleton(Options.Create(config));
    
    var token = config.Discord.Token;

    services.AddDiscordGateway(_ => token);
    services.AddDiscordCommands(true);
    services.Configure<InteractionResponderOptions>(s => s.UseEphemeralResponses = true);
    services.AddCommandGroupsFromAssembly(Assembly.GetExecutingAssembly());
    services.AddInteractivityFromAssembly(Assembly.GetExecutingAssembly());
    services.AddHostedService<KobaltDiscordGatewayService>();
    services.AddTransient<ImageOverlayService>();

    services.AddPostExecutionEvent<PostExecutionHandler>();

    services.AddHttpClient("booru");
    services.AddTransient<BooruSearchService>();
    
    services.Configure<DiscordGatewayClientOptions>
    (
        options =>
        {
            options.Intents |= GatewayIntents.MessageContents;
            options.Presence = new UpdatePresence
            (
                Status:ClientStatus.DND,
                IsAFK: false,
                Since: null,
                Activities: new IActivity[]
                {
                    new Activity
                    (
                        Name: "Code being written",
                        Type: ActivityType.Watching
                    )
                }
            );
        }
    );
}

void ConfigureLogging(ILoggingBuilder loggingBuilder)
{
    const string LogFormat = "[{@t:h:mm:ss ff tt}] [{@l:u3}] [{Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}] {@m}\n{@x}";
    
    Log.Logger = new LoggerConfiguration()
    #if DEBUG
                 .MinimumLevel.Debug()
    #endif
                 .MinimumLevel.Override("Microsoft.*", LogEventLevel.Debug)
                 .MinimumLevel.Override("System.*", LogEventLevel.Error)
                 .MinimumLevel.Override("Remora", LogEventLevel.Warning)
                 .WriteTo.Console(new ExpressionTemplate(LogFormat))
                 .CreateLogger();
    
    loggingBuilder.ClearProviders();
    loggingBuilder.AddSerilog(Log.Logger);
}