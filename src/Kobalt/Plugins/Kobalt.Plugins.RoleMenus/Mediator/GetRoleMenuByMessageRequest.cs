using Kobalt.Plugins.RoleMenus.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.RoleMenus.Mediator;

public static class GetRoleMenuByMessage
{
    /// <summary>
    /// Requests a rolemenu by the attached message's ID.
    /// </summary>
    /// <param name="MessageID">The ID of the message the role menu is attached to.</param>
    public record Request(Snowflake MessageID) : IRequest<Result<RoleMenuEntity>>;
    
    internal class Handler(IDbContextFactory<RoleMenuContext> dbFactory) : IRequestHandler<Request, Result<RoleMenuEntity>>
    {
        public async Task<Result<RoleMenuEntity>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await dbFactory.CreateDbContextAsync(cancellationToken);
            
            var roleMenu = await context.RoleMenus
                                       .Include(r => r.Options)
                                       .FirstOrDefaultAsync(r => r.MessageID == request.MessageID, cancellationToken);

            if (roleMenu is null)
            {
                return new NotFoundError($"No role menu with the message ID `{request.MessageID}` was found.");
            }

            return roleMenu;
        }
    }
    
}