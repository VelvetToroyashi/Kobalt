using Kobalt.Bot.Data.Entities.RoleMenus;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Data.MediatR.RoleMenus;

public static class UpdateRoleMenuOption
{
    /// <summary>
    /// Updates a role menu option.
    /// </summary>
    /// <param name="RoleMenuID">The ID of the role menu the option belongs to.</param>
    /// <param name="RoleID">The ID of the option to update.</param>
    /// <param name="GuildID">The ID of the guild the option belongs to.</param>
    /// <param name="Name">The new name of the option.</param>
    /// <param name="Description">The new description of the option.</param>
    /// <param name="NewRoleID">The new role ID of the option.</param>
    /// <param name="MutuallyInclusiveRoles">Mutually inclusive roles.</param>
    /// <param name="MutuallyExclusiveRoles">Mutually exclusive roles.</param>
    public record Request
    (
        int RoleMenuID,
        Snowflake RoleID,
        Snowflake GuildID,
        Optional<string> Name,
        Optional<string> Description,
        Optional<Snowflake> NewRoleID,
        Optional<IEnumerable<Snowflake>> MutuallyInclusiveRoles,
        Optional<IEnumerable<Snowflake>> MutuallyExclusiveRoles
    ) : IRequest<Result<RoleMenuOptionEntity>>;
    
    internal class Handler(IDbContextFactory<KobaltContext> dbFactory) : IRequestHandler<Request, Result<RoleMenuOptionEntity>>
    {
        public async Task<Result<RoleMenuOptionEntity>> Handle(Request request, CancellationToken cancellationToken)
        {
            if 
            (
                !request.Name.HasValue &&
                !request.Description.HasValue &&
                !request.NewRoleID.HasValue &&
                !request.MutuallyInclusiveRoles.HasValue &&
                !request.MutuallyExclusiveRoles.HasValue
            )
            {
                return new InvalidOperationError("Name, description, role ID, mutually inclusive roles, or mutually exclusive roles must be specified.");
            }

            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var option = await db.Set<RoleMenuOptionEntity>()
                                 .FirstOrDefaultAsync
                                 (
                                     c =>
                                     c.RoleMenuId == request.RoleMenuID &&
                                     c.RoleID == request.RoleID && 
                                     c.RoleMenu.GuildID == request.GuildID,
                                     cancellationToken
                                 );
            
            if (option is null)
            {
                return new NotFoundError($"No option exists with the ID `{request.RoleID}`");
            }

            option.Name = request.Name.OrDefault(option.Name);
            option.Description = request.Description.OrDefault(option.Description);
            option.RoleID = request.NewRoleID.OrDefault(option.RoleID);
            option.MutuallyInclusiveRoles = request.MutuallyInclusiveRoles.OrDefault(option.MutuallyInclusiveRoles).ToList();
            option.MutuallyExclusiveRoles = request.MutuallyExclusiveRoles.OrDefault(option.MutuallyExclusiveRoles).ToList();

            await db.SaveChangesAsync(cancellationToken);
            
            return option;
        }
    }
}