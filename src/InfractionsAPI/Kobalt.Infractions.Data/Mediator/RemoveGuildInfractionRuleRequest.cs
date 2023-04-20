using Mediator;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace Kobalt.Infractions.Data.Mediator;

public record RemoveGuildInfractionRuleRequest(int Id, ulong GuildID) : IRequest<Result>;

public class RemoveGuildInfractionRuleRequestHandler : IRequestHandler<RemoveGuildInfractionRuleRequest, Result>
{
    private readonly IDbContextFactory<InfractionContext> _context;

    public RemoveGuildInfractionRuleRequestHandler(IDbContextFactory<InfractionContext> context)
    {
        _context = context;
    }

    public async ValueTask<Result> Handle(RemoveGuildInfractionRuleRequest request, CancellationToken ct = default)
    {
        await using var context = await _context.CreateDbContextAsync(ct);
        
        var rule = await context.InfractionRules.FindAsync(request.Id, ct);

        if (rule is null)
        {
            return new NotFoundError("Infraction rule not found");
        }

        if (rule.GuildID != request.GuildID)
        {
            return new NotFoundError("Infraction rule not found");
        }

        context.InfractionRules.Remove(rule);
        await context.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }
}
