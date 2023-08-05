using Kobalt.Infractions.Shared;
using Kobalt.Infractions.Shared.DTOs;
using Kobalt.Infractions.Shared.Payloads;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace Kobalt.Infractions.Data.MediatR;

// PATCH /infractions/guilds/{guildID}/rules/{id}
public record UpdateGuildInfractionRuleRequest(int Id, ulong GuildID, InfractionRuleUpdatePayload Update) : IRequest<Result<InfractionRuleDTO>>;

public class UpdateGuildInfractionRuleRequestHandler : IRequestHandler<UpdateGuildInfractionRuleRequest, Result<InfractionRuleDTO>>
{
    private readonly IDbContextFactory<InfractionContext> _context;

    public UpdateGuildInfractionRuleRequestHandler(IDbContextFactory<InfractionContext> context) => _context = context;

    public async Task<Result<InfractionRuleDTO>> Handle(UpdateGuildInfractionRuleRequest request, CancellationToken ct)
    {
        await using var context = await _context.CreateDbContextAsync(ct);

        var update = request.Update;
        var rule = await context.InfractionRules
                                 .FirstOrDefaultAsync(ir => ir.Id == request.Id && ir.GuildID == request.GuildID, ct);

        if (rule is null)
        {
            return new NotFoundError("Infraction rule not found");
        }

        if (update.ActionDuration.TryGet(out var actionDuration))
        {
            if (actionDuration < TimeSpan.Zero || actionDuration < TimeSpan.FromMinutes(1))
            {
                return new InvalidOperationError("The infraction action duration cannot be negative or less than one minute");
            }

            rule.ActionDuration = actionDuration;
        }

        if (update.MatchTimeSpan.TryGet(out var matchTimespan))
        {
            if (matchTimespan < TimeSpan.Zero || matchTimespan < TimeSpan.FromMinutes(1))
            {
                return new InvalidOperationError("The infraction match timespan cannot be negative or less than one minute");
            }

            rule.MatchTimeSpan = matchTimespan;
        }

        if (update.ActionType.IsDefined(out var actionType))
        {
            if (actionType is InfractionType.Unmute or InfractionType.Unban or InfractionType.Pardon)
            {
                return new InvalidOperationError($"Invalid action type (expected kick, ban, mute, or warning, got {actionType})");
            }

            rule.ActionType = actionType;
        }

        if (update.MatchType.IsDefined(out var matchType))
        {
            if (matchType is InfractionType.Unmute or InfractionType.Unban or InfractionType.Pardon)
            {
                return new InvalidOperationError($"Invalid match type (expected kick, ban, mute, or warning, got {matchType})");
            }

            rule.MatchType = matchType;
        }

        if (update.MatchValue.TryGet(out var matchValue))
        {
            if (matchValue < 2)
            {
                return new InvalidOperationError("An infraction rule must match at least two infractions.");
            }

            rule.MatchValue = matchValue;
        }

        context.Update(rule);
        await context.SaveChangesAsync(ct);

        return new InfractionRuleDTO
        (
            rule.Id,
            rule.GuildID,
            rule.ActionType,
            rule.MatchTimeSpan,
            rule.MatchValue,
            rule.MatchType,
            rule.ActionDuration
        );
    }
}
