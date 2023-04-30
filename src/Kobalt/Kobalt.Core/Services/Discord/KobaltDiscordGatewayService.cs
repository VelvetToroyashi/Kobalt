using Kobalt.Infrastructure.Types;
using Microsoft.Extensions.Options;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;

namespace Kobalt.Core.Services.Discord;

/// <summary>
/// A service that handles starting the <see cref="DiscordGatewayClient"/>.
/// This class is also responsible for registering slash commands.
/// </summary>
/// <remarks>
/// This class differs from the DiscordService class present in
/// Remora.Discord.Hosting in that it's shard-aware. This means that it will
/// start the client with the appropriate shard (and also resume the session if applicable).
/// </remarks>
public sealed class KobaltDiscordGatewayService : BackgroundService
{
    private readonly SlashService _slashAPI;
    private readonly DiscordGatewayClient _gateway;
    private readonly IOptions<KobaltConfig> _config;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<KobaltDiscordGatewayService> _logger;

    public KobaltDiscordGatewayService
    (
        SlashService slashApi,
        DiscordGatewayClient gateway,
        IOptions<KobaltConfig> config,
        IHostApplicationLifetime lifetime,
        ILogger<KobaltDiscordGatewayService> logger
    )
    {
        _slashAPI = slashApi;
        _gateway = gateway;
        _config = config;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Cancelling via CT throws. This is an issue in Remora.
        // https://github.com/Remora/Remora.Discord/issues/248

        var slashResult = await RegisterSlashCommandsAsync();

        if (!slashResult.IsSuccess)
        {
            _lifetime.StopApplication();
        }

        Result gatewayResult;
        try
        {
            _logger.LogInformation("Starting gateway client...");
            gatewayResult = await _gateway.RunAsync(stoppingToken);
        }
        catch (Exception e)
        {
            gatewayResult = e;
        }

        if (!gatewayResult.IsSuccess)
        {
            _lifetime.StopApplication();
        }
    }

    private async Task<Result> RegisterSlashCommandsAsync()
    {
        _logger.LogDebug("Attempting to register slash commands.");

        var result = await _slashAPI.UpdateSlashCommandsAsync();

        if (!result.IsSuccess)
        {
            _logger.LogError("Failed to register slash commands: {Error}", result.Error);
            return result;
        }

        _logger.LogDebug("Successfully registered slash commands.");
        return Result.FromSuccess();
    }
}
