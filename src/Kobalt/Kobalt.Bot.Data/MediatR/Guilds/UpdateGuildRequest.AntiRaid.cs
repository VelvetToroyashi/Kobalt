using Kobalt.Infractions.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Data.MediatR.Guilds;

public static partial class UpdateGuild
{
    public static class AntiRaid
    {
        /// <summary>
        /// Represents a request to update the anti-raid config of a guild.
        /// </summary>
        /// <param name="GuildID">The ID of the guild whose settings are being updated.</param>
        /// <param name="IsEnabled">Specifies whether or not anti-raid is enabled.</param>
        /// <param name="MinimumAccountAgeBypass">Represents the minimum account age required to bypass anti-raid.</param>
        /// <param name="AccountFlagsBypass">Represents the account flags (badges) that are allowed to bypass anti-raid.</param>
        /// <param name="BaseJoinScore">The base score applied to all users that join the server.</param>
        /// <param name="JoinVelocityScore">The score given to a user for joining the server shortly after another user.</param>
        /// <param name="MinimumAgeScore">The score given to a user for their account age being too new.</param>
        /// <param name="NoAvatarScore">The score given to a user for not having an avatar.</param>
        /// <param name="SuspiciousInviteScore">The score given to a user for joining the server from a suspicious invite.</param>
        /// <param name="ThreatScoreThreshold">The threshold score to consider all users to be a threat.</param>
        /// <param name="AntiRaidCooldownPeriod">The cooldown period from the last join for a raid to no longer be considered active, and to clear the raid score.</param>
        /// <param name="LastJoinBufferPeriod">The period of time between joins that should be considered suspicious.</param>
        /// <param name="MinimumAccountAge">The minimum age of an account that should be considered suspicious.</param>
        public record Request
        (
            Snowflake GuildID, 
            Optional<bool> IsEnabled, 
            Optional<TimeSpan?> MinimumAccountAgeBypass, 
            Optional<UserFlags?> AccountFlagsBypass, 
            Optional<int> BaseJoinScore, 
            Optional<int> JoinVelocityScore, 
            Optional<int> MinimumAgeScore, 
            Optional<int> NoAvatarScore, 
            Optional<int> SuspiciousInviteScore, 
            Optional<int> ThreatScoreThreshold, 
            Optional<TimeSpan> AntiRaidCooldownPeriod, 
            Optional<TimeSpan> LastJoinBufferPeriod, 
            Optional<TimeSpan> MinimumAccountAge
        ) : IRequest<Result>;
        
        internal class Handler(IDbContextFactory<KobaltContext> context) : IRequestHandler<Request, Result>
        {
            public async Task<Result> Handle(Request request, CancellationToken cancellationToken)
            {
                await using var db = await context.CreateDbContextAsync(cancellationToken);

                var config = await db.Guilds
                    .Where(x => x.ID == request.GuildID)
                    .Select(x => x.AntiRaidConfig)
                    .FirstOrDefaultAsync(cancellationToken);

                if (config is null)
                {
                    return new NotFoundError();
                }

                if (request.IsEnabled.IsDefined(out var isEnabled))
                {
                    config.IsEnabled = isEnabled;
                }
                
                if (request.MinimumAccountAgeBypass.IsDefined(out var minimumAccountAgeBypass))
                {
                    config.MiniumAccountAgeBypass = minimumAccountAgeBypass;
                }
                
                if (request.AccountFlagsBypass.IsDefined(out var accountFlagsBypass))
                {
                    config.AccountFlagsBypass = accountFlagsBypass;
                }
                
                if (request.BaseJoinScore.IsDefined(out var baseJoinScore))
                {
                    config.BaseJoinScore = baseJoinScore;
                }
                
                if (request.JoinVelocityScore.IsDefined(out var joinVelocityScore))
                {
                    config.JoinVelocityScore = joinVelocityScore;
                }
                
                if (request.MinimumAgeScore.IsDefined(out var minimumAgeScore))
                {
                    config.MinimumAgeScore = minimumAgeScore;
                }
                
                if (request.NoAvatarScore.IsDefined(out var noAvatarScore))
                {
                    config.NoAvatarScore = noAvatarScore;
                }
                
                if (request.SuspiciousInviteScore.IsDefined(out var suspiciousInviteScore))
                {
                    config.SuspiciousInviteScore = suspiciousInviteScore;
                }
                
                if (request.ThreatScoreThreshold.IsDefined(out var threatScoreThreshold))
                {
                    config.ThreatScoreThreshold = threatScoreThreshold;
                }
                
                if (request.AntiRaidCooldownPeriod.IsDefined(out var antiRaidCooldownPeriod))
                {
                    config.AntiRaidCooldownPeriod = antiRaidCooldownPeriod;
                }
                
                if (request.LastJoinBufferPeriod.IsDefined(out var lastJoinBufferPeriod))
                {
                    config.LastJoinBufferPeriod = lastJoinBufferPeriod;
                }
                
                if (request.MinimumAccountAge.IsDefined(out var minimumAccountAge))
                {
                    config.MinimumAccountAge = minimumAccountAge;
                }

                await db.SaveChangesAsync(cancellationToken);

                return Result.FromSuccess();
            }
        }
    }
}