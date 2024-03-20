using EFCore.BulkExtensions;
using Kobalt.Infractions.Data.Entities;
using Kobalt.Infractions.Shared;
using Kobalt.Infractions.Shared.DTOs;
using Kobalt.Infractions.Shared.Payloads;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace Kobalt.Infractions.Data.MediatR;

public static class UpdateGuildInfractionRules
{
    public record Request(ulong GuildID, IEnumerable<InfractionRuleDTO> Matches) : IRequest<Result<IReadOnlyList<InfractionRuleDTO>>>;
    
    internal class Handler : IRequestHandler<Request, Result<IReadOnlyList<InfractionRuleDTO>>>
    {
        private readonly IDbContextFactory<InfractionContext> _context;

        public Handler(IDbContextFactory<InfractionContext> context) => _context = context;

        public async Task<Result<IReadOnlyList<InfractionRuleDTO>>> Handle(Request request, CancellationToken ct)
        {
            await using var context = await _context.CreateDbContextAsync(ct);
            await context.InfractionRules.Where(g => g.GuildID == request.GuildID).ExecuteDeleteAsync(ct);

            var entities = request.Matches.Select
            (
                m => new InfractionRule
                {
                    GuildID = request.GuildID, 
                    RuleName = m.RuleName,
                    MatchType = m.MatchType,
                    MatchValue = m.MatchValue,
                    ActionType = m.ActionType,
                    MatchTimeSpan = m.EffectiveTimespan,
                    ActionDuration = m.ActionDuration,
                }
            );
            
            await context.BulkInsertOrUpdateOrDeleteAsync(entities, c => c.SetSynchronizeFilter<InfractionRuleDTO>(g => g.GuildID == request.GuildID), cancellationToken: ct);

            return default;
        }
    }
    
}