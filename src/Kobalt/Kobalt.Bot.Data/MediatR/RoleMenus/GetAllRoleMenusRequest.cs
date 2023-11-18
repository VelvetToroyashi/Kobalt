using Kobalt.Bot.Data.Entities.RoleMenus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Data.MediatR.RoleMenus;

public static class GetAllRoleMenus
{
    public record Request(Snowflake GuildID) : IRequest<Result<IReadOnlyList<RoleMenuEntity>>>;
 
    internal class Handler(IDbContextFactory<KobaltContext> dbFactory) : IRequestHandler<Request, Result<IReadOnlyList<RoleMenuEntity>>>
    {
        public async Task<Result<IReadOnlyList<RoleMenuEntity>>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await dbFactory.CreateDbContextAsync(cancellationToken);
            
            var roleMenus = await context.RoleMenus
                                        .Include(r => r.Options)
                                        .Where(r => r.GuildID == request.GuildID)
                                        .ToListAsync(cancellationToken);

            return roleMenus;
        }
    }
}