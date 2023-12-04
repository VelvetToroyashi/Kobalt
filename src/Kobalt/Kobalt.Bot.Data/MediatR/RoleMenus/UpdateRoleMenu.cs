using Kobalt.Bot.Data.Entities.RoleMenus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Data.MediatR.RoleMenus;

public static class UpdateRoleMenu
{
    public record Request
    (
        int MenuId,
        Snowflake GuildID,
        Optional<string> Name = default,
        Optional<string> Description = default,
        Optional<int> MaxSelections = default,
        Optional<Snowflake> MessageID = default,
        Optional<IReadOnlyList<RoleMenuOptionEntity>> Options = default
    ) : IRequest<Result<RoleMenuEntity>>;
    
    internal class Handler(IDbContextFactory<KobaltContext> dbFactory) : IRequestHandler<Request, Result<RoleMenuEntity>>
    {
        public async Task<Result<RoleMenuEntity>> Handle(Request request, CancellationToken cancellationToken)
        {
            if (!request.Name.HasValue && !request.Description.HasValue && !request.MaxSelections.HasValue && !request.MessageID.HasValue)
            {
                return new InvalidOperationError("Name, description, max selections, or message ID must be specified.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var roleMenu = await db.RoleMenus.FindAsync(new object[] { request.MenuId }, cancellationToken);
            
            if (roleMenu is null || roleMenu.GuildID != request.GuildID)
            {
                return new NotFoundError($"No menu exists with the ID `{request.MenuId}`");
            }

            roleMenu.Name = request.Name.OrDefault(roleMenu.Name);
            roleMenu.Description = request.Description.OrDefault(roleMenu.Description);
            roleMenu.MaxSelections = request.MaxSelections.OrDefault(roleMenu.MaxSelections);
            roleMenu.MessageID = request.MessageID.OrDefault(roleMenu.MessageID);

            await db.SaveChangesAsync(cancellationToken);
            
            return roleMenu;
        }
    }
}