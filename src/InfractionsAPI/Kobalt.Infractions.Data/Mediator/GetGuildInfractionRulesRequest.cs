using Kobalt.Infractions.Data;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Infractions.Infrastructure.Mediator;

public record GetGuildInfractionRulesRequest(ulong GuildID) : IRequest<IEnumerable<InfractionRuleDTO>>;

public class GetGuildInfractionRulesHandler : IRequestHandler<GetGuildInfractionRulesRequest, IEnumerable<InfractionRuleDTO>>
{
    private readonly InfractionContext _context;

    public GetGuildInfractionRulesHandler(InfractionContext context)
    {
        _context = context;
    }

    public async ValueTask<IEnumerable<InfractionRuleDTO>> Handle(GetGuildInfractionRulesRequest request, CancellationToken cancellationToken)
    {
        var rules = await _context.InfractionRules
            .Where(x => x.GuildID == request.GuildID)
            .ToListAsync(cancellationToken);

        return rules.Select
        (
            x => new InfractionRuleDTO
            (
                x.Id,
                x.GuildID,
                x.ActionType,
                x.MatchTimeSpan,
                x.MatchValue,
                x.MatchType,
                x.ActionDuration
            )
        );
    }
}
