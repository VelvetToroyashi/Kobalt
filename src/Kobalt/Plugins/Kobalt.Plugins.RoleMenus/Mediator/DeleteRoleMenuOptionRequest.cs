using Kobalt.Plugins.RoleMenus.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.RoleMenus.Mediator;

public static class DeleteRoleMenuOption
{
    /// <summary>
    /// Deletes a role menu option.
    /// </summary>
    /// <param name="MenuID">The ID of the option to be deleted.</param>
    public record Request(int MenuID, Snowflake GuildID, Snowflake RoleID) : IRequest<Result>;
    
    internal class Handler(IDbContextFactory<RoleMenuContext> dbFactory) : IRequestHandler<Request, Result>
    {
        public async Task<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var option = await db.Set<RoleMenuOptionEntity>()
                                 .FirstOrDefaultAsync(c => c.RoleMenuId == request.MenuID && c.RoleMenu.GuildID == request.GuildID && c.RoleID == request.RoleID, cancellationToken);

            if (option is null)
            {
                return new NotFoundError($"No role menu option with the ID `{request.MenuID}` exists.");
            }

            db.Remove(option);

            await db.SaveChangesAsync(cancellationToken);

            return Result.FromSuccess();
        }
    }
}