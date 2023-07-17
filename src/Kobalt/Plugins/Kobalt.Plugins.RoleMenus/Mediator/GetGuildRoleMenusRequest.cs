using Kobalt.Plugins.RoleMenus.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;

namespace Kobalt.Plugins.RoleMenus.Mediator;

public static class GetGuildRoleMenus
{
    /// <summary>
    /// Gets all role menus for a guild.
    /// </summary>
    /// <param name="GuildID">The ID of the guild to return role menus for, if any.</param>
    /// <remarks>
    /// This request is not intended for use-cases that require the options of the role menu;
    /// this is primarily intended to list off all role menus for a guild.
    /// </remarks>
    public record Request(Snowflake GuildID) : IRequest<IReadOnlyList<RoleMenuEntity>>;
    
    internal class Handler(IDbContextFactory<RoleMenuContext> dbFactory) : IRequestHandler<Request, IReadOnlyList<RoleMenuEntity>>
    {
        public async Task<IReadOnlyList<RoleMenuEntity>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await dbFactory.CreateDbContextAsync(cancellationToken);
            
            var roleMenus = await context.RoleMenus
                                         .Where(r => r.GuildID == request.GuildID)
                                         .ToListAsync(cancellationToken);

            return roleMenus;
        }
    }
}