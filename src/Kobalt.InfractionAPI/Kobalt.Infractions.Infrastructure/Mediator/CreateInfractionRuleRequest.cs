using Kobalt.Infractions.Data;
using Kobalt.Infractions.Data.Entities;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Kobalt.Infractions.Infrastructure.Mediator.Errors;
using Kobalt.Infractions.Shared;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace Kobalt.Infractions.Infrastructure.Mediator;

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
    private readonly InfractionContext _context;

    public CreateInfractionRuleRequestHandler(InfractionContext context)
    {
        _context = context;
    }

    public async ValueTask<Result<InfractionRuleDTO>> Handle(CreateInfractionRuleRequest request, CancellationToken cancellationToken)
    {
        var matchingRule = await _context
                                 .InfractionRules
                                 .Where
                                 (
                                     ir =>
                                     ir.GuildID           == request.GuildID    &&
                                     ir.MatchType         == request.MatchType  &&
                                     ir.MatchValue        == request.MatchValue &&
                                     ir.MatchTimeSpan == request.EffectiveTimespan
                                 )
                                 .FirstOrDefaultAsync(cancellationToken);

        if (matchingRule is not null)
        {
            return new RuleAlreadyExistsError(matchingRule.MatchValue, matchingRule.MatchType);
        }
        
        var rule = new InfractionRule
        {
            GuildID           = request.GuildID,
            ActionType        = request.ActionType,
            MatchTimeSpan = request.EffectiveTimespan,
            MatchValue        = request.MatchValue,
            MatchType         = request.MatchType,
            ActionDuration    = request.ActionDuration
        };
        
        _context.Add(rule);
        await _context.SaveChangesAsync(cancellationToken);

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