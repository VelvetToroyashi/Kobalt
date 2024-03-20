using Kobalt.Infractions.Shared.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Infractions.Data.MediatR;

public record GetGuildInfractionRulesRequest(ulong GuildID) : IRequest<IEnumerable<InfractionRuleDTO>>;

public class GetGuildInfractionRulesHandler : IRequestHandler<GetGuildInfractionRulesRequest, IEnumerable<InfractionRuleDTO>>
{
    private readonly IDbContextFactory<InfractionContext> _context;

    public GetGuildInfractionRulesHandler(IDbContextFactory<InfractionContext> context) => _context = context;

    public async Task<IEnumerable<InfractionRuleDTO>> Handle(GetGuildInfractionRulesRequest request, CancellationToken cancellationToken)
    {
        await using var context = await _context.CreateDbContextAsync(cancellationToken);

        var rules = await context.InfractionRules
                                 .Where(x => x.GuildID == request.GuildID)
                                 .ToListAsync(cancellationToken);

        var dtos = rules.Select
        (
            x => new InfractionRuleDTO
            (
                x.Id,
                x.GuildID,
                x.RuleName,
                x.ActionType,
                x.MatchTimeSpan,
                x.MatchValue,
                x.MatchType,
                x.ActionDuration
            )
        );

        return dtos.ToArray();
    }
}
