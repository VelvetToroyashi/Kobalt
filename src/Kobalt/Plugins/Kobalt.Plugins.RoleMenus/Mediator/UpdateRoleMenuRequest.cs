using Kobalt.Plugins.RoleMenus.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.RoleMenus.Mediator;

public static class UpdateRoleMenuRequest
{
    public record Request
    (
        int MenuId,
        Optional<string> Name,
        Optional<string> Description,
        Optional<int> MaxSelections
    ) : IRequest<Result<RoleMenuEntity>>;
    
    internal class Handler(IDbContextFactory<RoleMenuContext> dbFactory) : IRequestHandler<Request, Result<RoleMenuEntity>>
    {
        public async Task<Result<RoleMenuEntity>> Handle(Request request, CancellationToken cancellationToken)
        {
            if (!request.Name.HasValue && !request.Description.HasValue && !request.MaxSelections.HasValue)
            {
                return new InvalidOperationError("Name, description, or max selections must be specified.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var roleMenu = await db.RoleMenus.FindAsync(new object[] { request.MenuId }, cancellationToken);
            
            if (roleMenu is null)
            {
                return new NotFoundError($"No menu exists with the ID `{request.MenuId}`");
            }

            roleMenu.Name = request.Name.OrDefault(roleMenu.Name);
            roleMenu.Description = request.Description.OrDefault(roleMenu.Description);
            roleMenu.MaxSelections = request.MaxSelections.OrDefault(roleMenu.MaxSelections);

            return roleMenu;
        }
    }
}