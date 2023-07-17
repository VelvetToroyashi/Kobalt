using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.RoleMenus.Mediator;

public static class DeleteRoleMenu
{
    public record Request(int MenuID, Snowflake GuildID) : IRequest<Result>;
    
    internal class Handler(IDbContextFactory<RoleMenuContext> dbFactory) : IRequestHandler<Request, Result>
    {
        public async Task<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var menu = await db.RoleMenus.FindAsync(new object[] { request.MenuID }, cancellationToken);

            if (menu is null || menu.GuildID != request.GuildID)
            {
                return new NotFoundError($"No role menu with the ID `{request.MenuID}` exists.");
            }

            db.Remove(menu);

            await db.SaveChangesAsync(cancellationToken);

            return Result.FromSuccess();
        }
    }
    
}