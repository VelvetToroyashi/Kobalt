using Kobalt.Bot.Data.Entities.RoleMenus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Data.MediatR.RoleMenus;

public static class DeleteRoleMenu
{
    public record Request(int MenuID, Snowflake GuildID) : IRequest<Result<RoleMenuEntity>>;
    
    internal class Handler(IDbContextFactory<KobaltContext> dbFactory) : IRequestHandler<Request, Result<RoleMenuEntity>>
    {
        public async Task<Result<RoleMenuEntity>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var menu = await db.RoleMenus.FindAsync(new object[] { request.MenuID }, cancellationToken);

            if (menu is null || menu.GuildID != request.GuildID)
            {
                return new NotFoundError($"No role menu with the ID `{request.MenuID}` exists.");
            }

            db.Remove(menu);

            await db.SaveChangesAsync(cancellationToken);

            return menu;
        }
    }
    
}