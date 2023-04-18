using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Kobalt.Infrastructure.Services;

// https://andrewlock.net/running-async-tasks-on-app-startup-in-asp-net-core-part-2/
// This code was given to me by Maxine on Discord, thanks ily /p

public interface IStartupTask
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStartupTask<T>(this IServiceCollection services)
        where T : class, IStartupTask
        => services.AddTransient<IStartupTask, T>();
        
    public static IServiceCollection AddStartupTask(this IServiceCollection services, Func<IServiceProvider, Task> startupTask)
        => services.AddTransient<IStartupTask>(s => new DelegateStartupTask(s, startupTask));

    private class DelegateStartupTask : IStartupTask
    {
        private readonly IServiceProvider _services;
        private readonly Func<IServiceProvider, Task> _startupTask;

        public DelegateStartupTask(IServiceProvider services, Func<IServiceProvider, Task> startupTask)
        {
            _services = services;
            _startupTask = startupTask;
        }

        public Task ExecuteAsync(CancellationToken cancellationToken = default) => _startupTask(_services);
    }
}

public static class StartupTaskWebHostExtensions
{
    public static IHostBuilder AddStartupTaskSupport(this IHostBuilder hostBuilder)
    {
        return hostBuilder.ConfigureServices(static (_, services) =>
        {
            services.Decorate<IHost, HostDecorator>();
        });
    }
    
    // From Remora, modified a bit
    /// <summary>
    /// Registers a decorator service, replacing the existing interface.
    /// </summary>
    /// <remarks>
    /// Implementation based off of
    /// https://greatrexpectations.com/2018/10/25/decorators-in-net-core-with-dependency-injection/.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <typeparam name="TInterface">The interface type to decorate.</typeparam>
    /// <typeparam name="TDecorator">The decorator type.</typeparam>
    /// <returns>The service collection, with the decorated service.</returns>
    public static IServiceCollection Decorate<
        TInterface,
        [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)] TDecorator
    >(this IServiceCollection services)
        where TInterface : class
        where TDecorator : class, TInterface
    {
        var wrappedDescriptor = services.First(s => s.ServiceType == typeof(TInterface));

        var factory = CreateServiceFactory(wrappedDescriptor);

        var objectFactory = ActivatorUtilities.CreateFactory(typeof(TDecorator), new[] { typeof(TInterface) });
        services.Replace(ServiceDescriptor.Describe
        (
            typeof(TInterface),
            s => (TInterface)objectFactory(s, new[] { factory(s) }),
            wrappedDescriptor.Lifetime
        ));

        return services;
    }

    private static Func<IServiceProvider, object> CreateServiceFactory(ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationInstance != null)
        {
            return _ => descriptor.ImplementationInstance;
        }

        if (descriptor.ImplementationFactory != null)
        {
            return descriptor.ImplementationFactory;
        }

        ArgumentNullException.ThrowIfNull(descriptor.ImplementationType);

        ObjectFactory? factory = null;

        return services =>
        {
            if (services.GetService(descriptor.ImplementationType) is {} service)
            {
                return service;
            }

            factory ??= ActivatorUtilities.CreateFactory(descriptor.ImplementationType, Array.Empty<Type>());

            return factory(services, Array.Empty<object?>());
        };
    }
}

file sealed class HostDecorator : IHost, IAsyncDisposable
{
    private readonly IHost _hostImplementation;

    public IServiceProvider Services => _hostImplementation.Services;

    public HostDecorator(IHost hostImplementation)
    {
        _hostImplementation = hostImplementation;
    }

    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
    public async Task StartAsync(CancellationToken cancellationToken = new())
    {
        foreach (var startupTask in Services.GetRequiredService<IEnumerable<IStartupTask>>())
        {
            await startupTask.ExecuteAsync(cancellationToken);
            switch (startupTask)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }

        await _hostImplementation.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = new())
    {
        return _hostImplementation.StopAsync(cancellationToken);
    }
    
    public void Dispose()
    {
        _hostImplementation.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_hostImplementation is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else
        {
            _hostImplementation.Dispose();
        }
    }
}
