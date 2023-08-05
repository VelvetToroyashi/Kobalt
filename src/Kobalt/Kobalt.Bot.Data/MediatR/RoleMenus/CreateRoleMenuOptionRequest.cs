using Kobalt.Bot.Data.Entities.RoleMenus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Data.MediatR.RoleMenus;

public static class CreateRoleMenuOption
{
    public record Request
    (
        int RoleMenuID,
        Snowflake RoleID,
        string Name,
        string Description,
        IEnumerable<Snowflake> MutuallyInclusiveRoles,
        IEnumerable<Snowflake> MutuallyExclusiveRoles
    ) : IRequest<Result<RoleMenuOptionEntity>>;

    internal class Handler(IDbContextFactory<KobaltContext> dbFactory) : IRequestHandler<Request, Result<RoleMenuOptionEntity>>
    {
        public async Task<Result<RoleMenuOptionEntity>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            
            var option = new RoleMenuOptionEntity
            {
                RoleMenuId = request.RoleMenuID,
                Name = request.Name,
                Description = request.Description,
                RoleID = request.RoleID,
                MutuallyExclusiveRoles = request.MutuallyExclusiveRoles.ToList(),
                MutuallyInclusiveRoles = request.MutuallyInclusiveRoles.ToList(),
            };

            db.Add(option);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                return new NotFoundError($"No role menu with the ID `{request.RoleMenuID}` exists.");
            }

            return option;
        }
    }

}