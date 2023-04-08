using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
                     .WriteTo.Console(new ExpressionTemplate(LogFormat))
                     .CreateLogger();

        loggingBuilder.ClearProviders();
        loggingBuilder.AddSerilog(Log.Logger);
    }
}
