using Kobalt.Bot.Data.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Data.MediatR.Guilds;

public static partial class GetGuild
{
    /// <summary>
    /// Requests a guild's anti-raid configuration.
    /// </summary>
    /// <param name="GuildID">The ID of the guild.</param>
    public record AntiRaidConfigRequest(Snowflake GuildID) : IRequest<Result<GuildAntiRaidConfigDTO>>;

    internal class AntiRaidConfigHandler : IRequestHandler<AntiRaidConfigRequest, Result<GuildAntiRaidConfigDTO>>
    {
        private readonly IDbContextFactory<KobaltContext> _context;

        public AntiRaidConfigHandler(IDbContextFactory<KobaltContext> context) => _context = context;

        public async Task<Result<GuildAntiRaidConfigDTO>> Handle(AntiRaidConfigRequest request, CancellationToken ct)
        {
            await using var context = await _context.CreateDbContextAsync(ct);

            var config = await context.Guilds
                                      .Where(g => g.ID == request.GuildID)
                                      .Select(g => g.AntiRaidConfig)
                                      .AsNoTracking()
                                      .FirstOrDefaultAsync(ct);

            if (config is null)
            {
                return new NotFoundError();
            }

            return new GuildAntiRaidConfigDTO
            (
                config.IsEnabled,
                config.BaseJoinScore,
                config.MiniumAccountAgeBypass,
                config.AccountFlagsBypass,
                config.JoinVelocityScore,
                config.MinimumAgeScore,
                config.NoAvatarScore,
                config.SuspiciousInviteScore,
                config.ThreatScoreThreshold,
                config.AntiRaidCooldownPeriod,
                config.LastJoinBufferPeriod,
                config.MinimumAccountAge
            );
        }
    }
}
