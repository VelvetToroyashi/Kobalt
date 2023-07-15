using Kobalt.Plugins.RoleMenus.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.RoleMenus.Mediator;

public static class UpdateRoleMenuOption
{
    /// <summary>
    /// Updates a role menu option.
    /// </summary>
    /// <param name="OptionId">The ID of the option to update.</param>
    /// <param name="Name">The new name of the option.</param>
    /// <param name="Description">The new description of the option.</param>
    /// <param name="RoleId">The new role ID of the option.</param>
    /// <param name="MutuallyInclusiveRoles">Mutually inclusive roles.</param>
    /// <param name="MutuallyExclusiveRoles">Mutually exclusive roles.</param>
    public record Request
    (
        int OptionId,
        Optional<string> Name,
        Optional<string> Description,
        Optional<Snowflake> RoleId,
        Optional<IEnumerable<Snowflake>> MutuallyInclusiveRoles,
        Optional<IEnumerable<Snowflake>> MutuallyExclusiveRoles
    ) : IRequest<Result<RoleMenuOptionEntity>>;
    
    internal class Handler(IDbContextFactory<RoleMenuContext> dbFactory) : IRequestHandler<Request, Result<RoleMenuOptionEntity>>
    {
        public async Task<Result<RoleMenuOptionEntity>> Handle(Request request, CancellationToken cancellationToken)
        {
            if 
            (
                !request.Name.HasValue &&
                !request.Description.HasValue &&
                !request.RoleId.HasValue &&
                !request.MutuallyInclusiveRoles.HasValue &&
                !request.MutuallyExclusiveRoles.HasValue
            )
            {
                return new InvalidOperationError("Name, description, role ID, mutually inclusive roles, or mutually exclusive roles must be specified.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var option = await db.FindAsync<RoleMenuOptionEntity>(new object[] { request.OptionId }, cancellationToken);
            
            if (option is null)
            {
                return new NotFoundError($"No option exists with the ID `{request.OptionId}`");
            }

            option.Name = request.Name.OrDefault(option.Name);
            option.Description = request.Description.OrDefault(option.Description);
            option.RoleID = request.RoleId.OrDefault(option.RoleID);
            option.MutuallyInclusiveRoles = request.MutuallyInclusiveRoles.OrDefault(option.MutuallyInclusiveRoles).ToList();
            option.MutuallyExclusiveRoles = request.MutuallyExclusiveRoles.OrDefault(option.MutuallyExclusiveRoles).ToList();

            return option;
        }
    }
}