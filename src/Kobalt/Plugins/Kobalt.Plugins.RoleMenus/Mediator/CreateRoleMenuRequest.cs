using Kobalt.Plugins.RoleMenus.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;

namespace Kobalt.Plugins.RoleMenus.Mediator;

/// <summary>
/// Creates a new role menu.
/// </summary>
public static class CreateRoleMenu
{
    /// <summary>
    /// Represents the request to create a role menu.
    /// </summary>
    /// <param name="Name">The name of the role menu.</param>
    /// <param name="Description">The description of the role menu.</param>
    /// <param name="GuildID">The ID of the guild the role menu is registered to.</param>
    /// <param name="ChannelID">The ID of the chanel the role menu is registered to.</param>
    /// <param name="MessageID">The ID of the message the role menu is registered to.</param>
    /// <param name="MaxSelections">The maximum amount of roles that can be selected.</param>
    public record Request
    (
        string Name,
        string Description,
        Snowflake GuildID,
        Snowflake ChannelID,
        Snowflake MessageID,
        int MaxSelections
    ) : IRequest<RoleMenuEntity>;

    internal class Handler(IDbContextFactory<RoleMenuContext> dbFactory) : IRequestHandler<Request, RoleMenuEntity>
    {
        public async Task<RoleMenuEntity> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
            
            var roleMenu = new RoleMenuEntity
            {
                Name = request.Name,
                GuildID = request.GuildID,
                ChannelID = request.ChannelID,
                MessageID = request.MessageID,
                Description = request.Description,
                MaxSelections = request.MaxSelections,
            };

            db.RoleMenus.Add(roleMenu);

            await db.SaveChangesAsync(cancellationToken);

            return roleMenu;
        }
    }
}