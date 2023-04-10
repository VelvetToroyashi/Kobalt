using Mediator;
using Remora.Results;

namespace Kobalt.Infractions.Data.Mediator;

public record RemoveGuildInfractionRuleRequest(int Id, ulong GuildID) : IRequest<Result>;

public class RemoveGuildInfractionRuleRequestHandler : IRequestHandler<RemoveGuildInfractionRuleRequest, Result>
{
    private readonly InfractionContext _context;

    public RemoveGuildInfractionRuleRequestHandler(InfractionContext context)
    {
        _context = context;
    }

    public async ValueTask<Result> Handle(RemoveGuildInfractionRuleRequest request, CancellationToken ct = default)
    {
        var rule = await _context.InfractionRules.FindAsync(request.Id, ct);

        if (rule is null)
        {
            return new NotFoundError("Infraction rule not found");
        }

        if (rule.GuildID != request.GuildID)
        {
            return new NotFoundError("Infraction rule not found");
        }

        _context.InfractionRules.Remove(rule);

        await _context.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }
}
