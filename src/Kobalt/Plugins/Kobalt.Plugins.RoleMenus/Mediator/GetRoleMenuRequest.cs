using Kobalt.Plugins.RoleMenus.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.RoleMenus.Mediator;

public static class GetRoleMenu
{
    /// <summary>
    /// Requests a role menu.
    /// </summary>
    /// <param name="RoleMenuID"></param>
    public record Request(int RoleMenuID, Snowflake GuildID) : IRequest<Result<RoleMenuEntity>>;
    
    internal class Handler(IDbContextFactory<RoleMenuContext> dbFactory) : IRequestHandler<Request, Result<RoleMenuEntity>>
    {
        public async Task<Result<RoleMenuEntity>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await dbFactory.CreateDbContextAsync(cancellationToken);
            
            var roleMenu = await context.RoleMenus
                                       .Include(r => r.Options)
                                       .FirstOrDefaultAsync(r => r.Id == request.RoleMenuID, cancellationToken);

            if (roleMenu is null || roleMenu.GuildID != request.GuildID)
            {
                return new NotFoundError($"No role menu with the message ID `{request.RoleMenuID}` was found.");
            }

            return roleMenu;
        }
    }
    
    
}