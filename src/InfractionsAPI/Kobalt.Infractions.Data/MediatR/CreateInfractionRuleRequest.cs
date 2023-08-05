using Kobalt.Infractions.Data.Entities;
using Kobalt.Infractions.Shared;
using Kobalt.Infractions.Shared.DTOs;
using Kobalt.Infractions.Shared.Errors;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace Kobalt.Infractions.Data.MediatR;

// POST /infractions/guilds/{guildID}/rules
public record CreateInfractionRuleRequest
(
    ulong GuildID,
    InfractionType ActionType,
    TimeSpan? EffectiveTimespan,
    int MatchValue,
    InfractionType MatchType,
    TimeSpan? ActionDuration
) : IRequest<Result<InfractionRuleDTO>>;

public class CreateInfractionRuleRequestHandler : IRequestHandler<CreateInfractionRuleRequest, Result<InfractionRuleDTO>>
{
    private readonly IDbContextFactory<InfractionContext> _context;

    public CreateInfractionRuleRequestHandler(IDbContextFactory<InfractionContext> context) => _context = context;

    public async Task<Result<InfractionRuleDTO>> Handle(CreateInfractionRuleRequest request, CancellationToken cancellationToken)
    {
        await using var context = await _context.CreateDbContextAsync(cancellationToken);
        var matchingRule = await context
                                 .InfractionRules
                                 .Where
                                 (
                                     ir =>
                                     ir.GuildID       == request.GuildID    &&
                                     ir.MatchType     == request.MatchType  &&
                                     ir.MatchValue    == request.MatchValue &&
                                     ir.MatchTimeSpan == request.EffectiveTimespan
                                 )
                                 .FirstOrDefaultAsync(cancellationToken);

        if (matchingRule is not null)
        {
            return new RuleAlreadyExistsError(matchingRule.MatchValue, matchingRule.MatchType);
        }

        var rule = new InfractionRule
        {
            GuildID        = request.GuildID,
            ActionType     = request.ActionType,
            MatchTimeSpan  = request.EffectiveTimespan,
            MatchValue     = request.MatchValue,
            MatchType      = request.MatchType,
            ActionDuration = request.ActionDuration
        };

        context.Add(rule);
        await context.SaveChangesAsync(cancellationToken);

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
