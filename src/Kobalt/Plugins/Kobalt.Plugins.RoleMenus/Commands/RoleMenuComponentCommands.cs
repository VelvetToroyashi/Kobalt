using Kobalt.Plugins.RoleMenus.Services;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Interactivity;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Plugins.RoleMenus.Commands;

[Ephemeral]
public class RoleMenuComponentCommands
(
    IInteractionContext context,
    IDiscordRestInteractionAPI interactions,
    RoleMenuService roleMenus
) : InteractionGroup
{
    [Button(RoleMenuService.RoleMenuID)]
    public async Task<Result> HandleRoleMenuComponentAsync()
    {
        await roleMenus.DisplayRoleMenuAsync(context.Interaction);
        
        return Result.FromSuccess();
    }
    
    [SelectMenu(RoleMenuService.RoleMenuID)]
    public async Task<Result> HandleRoleMenuSelectMenuAsync(IReadOnlyList<Snowflake> values, Snowflake state)
    {
        var res = await roleMenus.AssignRoleMenuRolesAsync(state, values, context.Interaction);

        return Result.FromSuccess();
    }
}