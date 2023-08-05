using System.ComponentModel;
using Kobalt.Bot.Autocomplete;
using Kobalt.Bot.Data.Entities.RoleMenus;
using Kobalt.Bot.Data.MediatR.RoleMenus;
using Kobalt.Bot.Services;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Services;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Commands.RoleMenus;

[Group("role-menu")]
[Ephemeral]
[Description("Commands for managing role menus.")]
[DiscordDefaultDMPermission(false)]
[DiscordDefaultMemberPermissions(DiscordPermission.ManageMessages)]
public class RoleMenuCommands
(
    IMediator mediator,
    RoleMenuService rolemenus,
    IDiscordRestGuildAPI guilds,
    IDiscordRestChannelAPI channels,
    IDiscordRestInteractionAPI interactions,
    IInteractionCommandContext context,
    SlashService slashCommands
    
) : CommandGroup
{
    private readonly Snowflake _publishCommandID = slashCommands.CommandMap
                                                                .First(cm => cm.Value.TryPickT0(out var group, out _) && group.TryGetValue("role-menu::publish", out _))
                                                                .Key
                                                                .CommandID;
    
    private readonly Snowflake _addCommandID = slashCommands.CommandMap
                                                            .First(cm => cm.Value.TryPickT0(out var group, out _) && group.TryGetValue("role-menu::add-option", out _))
                                                            .Key
                                                            .CommandID;
    
    [Command("create")]
    [Description("Registers a new role menu.")]
    public async Task<Result> CreateAsync
    (
        // Bug in Remora; use [Option("channel_id")] when it's fixed
        [DiscordTypeHint(TypeHint.Channel)]
        [ChannelTypes(ChannelType.GuildText)]
        [Description("The channel to publish the role menu in.")]
        Snowflake channel_id,
    
        [Description("The name of the role menu.")]
        string name,
        string description = "",
        
        [MinValue(0)]
        [MaxValue(25)]
        [Description("The most amount of roles someone can select.")]
        int max_roles = 0
    )
    {
        var rolemenu = await mediator.Send
        (
            new CreateRoleMenu.Request
            (
                name,
                description,
                context.Interaction.GuildID.Value,
                channel_id,
                max_roles
            )
        );

        await interactions.CreateFollowupMessageAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
            $"Consider it done. You can add options to this role menu with </role-menu add-option:{_addCommandID}>. \n" +
            $"Your role menu's ID is `{rolemenu.Id}`, but there's also autocomplete."
        );
        
        return Result.FromSuccess();
    }

    [Command("publish")]
    [Description("Publishes a role menu.")]
    public async Task<Result> PublishAsync
    (
        [AutocompleteProvider(RoleMenuAutocompleteProvider.Identifier)]
        [Description("The ID of the role menu to publish.")]
        int role_menu_id
    )
    {
        var roleMenu = await mediator.Send(new GetRoleMenu.Request(role_menu_id, context.Interaction.GuildID.Value));

        if (!roleMenu.IsSuccess)
        {
            await interactions.CreateFollowupMessageAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                roleMenu.Error!.Message
            );

            return Result.FromSuccess();    
        }

        var publishResult = await rolemenus.PublishRoleMenuAsync(roleMenu.Entity);

        if (publishResult.IsSuccess)
        {
            await interactions.CreateFollowupMessageAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                $"Published. View it [here](<https://discord.com/channels/{roleMenu.Entity.GuildID}/{roleMenu.Entity.ChannelID}/{roleMenu.Entity.MessageID}>)."
            );
        }
        else
        {
            await interactions.CreateFollowupMessageAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                publishResult.Error!.Message
            );
        }

        return Result.FromSuccess();
    }

    [Command("delete")]
    [Description("Deletes a role menu.")]
    public async Task<Result> DeleteAsync
    (
        [Description("The ID of the role menu to delete.")]
        [AutocompleteProvider(RoleMenuAutocompleteProvider.Identifier)]
        int role_menu_id
    )
    {
        var deleteResult = await mediator.Send(new DeleteRoleMenu.Request(role_menu_id, context.Interaction.GuildID.Value));
        
        if (deleteResult.IsSuccess)
        {
            await interactions.CreateFollowupMessageAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                "Gone. To ashes with it."
            );
            
            // TODO: This is probably more apt for the role menu service.
            await channels.DeleteMessageAsync
            (
                deleteResult.Entity.ChannelID,
                deleteResult.Entity.MessageID
            );
        }
        else
        {
            await interactions.CreateFollowupMessageAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                deleteResult.Error!.Message
            );
        }
        
        return Result.FromSuccess();
    }
    
    [Command("edit")]
    [Description("Edits a role menu.")]
    public async Task<Result> EditAsync
    (
        [Description("The ID of the role menu to edit.")]
        [AutocompleteProvider(RoleMenuAutocompleteProvider.Identifier)]
        int role_menu_id,
        
        [Description("The new name of the role menu.")]
        string? name = null,
        
        [Description("The new description of the role menu.")]
        string? description = null,
        
        [MinValue(0)]
        [MaxValue(25)]
        [Description("The new maximum amount of roles someone can select.")]
        int max_roles = 0
    )
    {
        var editResult = await mediator.Send
        (
            new UpdateRoleMenu.Request
            (
                role_menu_id,
                context.Interaction.GuildID.Value,
                name.AsOptional(),
                description.AsOptional(),
                max_roles
            )
        );
        
        if (editResult.IsSuccess)
        {
            var published = editResult.Entity.MessageID != 0;
            var message = published
                ? $"Done. View the changes [here](<https://discord.com/channels/{editResult.Entity.GuildID}/{editResult.Entity.ChannelID}/{editResult.Entity.MessageID}>)."
                : $"Done. Publish the role menu with </role-menu publish:{_publishCommandID}>.";
            
            await interactions.CreateFollowupMessageAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                message
            );
        }
        else
        {
            await interactions.CreateFollowupMessageAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                editResult.Error!.Message
            );
        }
        
        return Result.FromSuccess();
    }
    
    [Command("add-option")]
    [Description("Adds a role to a role menu.")]
    public async Task<Result> AddOptionAsync
    (
        
        [Description("The ID of the role menu to add the role to.")]
        [AutocompleteProvider(RoleMenuAutocompleteProvider.Identifier)]
        int role_menu_id,
        
        [Description("The role to add.")]
        IRole role,
        
        [Description("The name of the option.")]
        string name = "",
        
        [Description("The description of the option.")]
        string description = ""
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            name = role.Name;
        }
        
        var roleMenu = await mediator.Send(new GetRoleMenu.Request(role_menu_id, context.Interaction.GuildID.Value));

        if (!roleMenu.IsSuccess)
        {
            await interactions.CreateFollowupMessageAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                "I don't see a role menu with that ID, sorry."
            );
        }

        var validateRoleResult = await EnsureValidRoleAsync(role, roleMenu.Entity.Options);

        if (!validateRoleResult.IsSuccess)
        {
            return (Result)await interactions.CreateFollowupMessageAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                validateRoleResult.Error!.Message
            );
        }
        
        var addResult = await mediator.Send
        (
            new CreateRoleMenuOption.Request
            (
                role_menu_id,
                role.ID,
                name,
                description,
                Enumerable.Empty<Snowflake>(),
                Enumerable.Empty<Snowflake>()
            )
        );

        if (!addResult.IsSuccess)
        {
            await interactions.CreateFollowupMessageAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                addResult.Error!.Message
            );
            return Result.FromSuccess();
        }

        await rolemenus.UpdateRoleMenuInitiatorAsync(roleMenu.Entity);
            
        var published = roleMenu.Entity.MessageID != 0;
        var message = published
            ? $"Done. View the changes [here](<https://discord.com/channels/{roleMenu.Entity.GuildID}/{roleMenu.Entity.ChannelID}/{roleMenu.Entity.MessageID}>)."
            : $"Done. Publish the role menu with </role-menu publish:{_publishCommandID}>.";
            
        await interactions.CreateFollowupMessageAsync
        (
            context.Interaction.ApplicationID,
            context.Interaction.Token,
            message
        );

        return Result.FromSuccess();
    }
    
    [Command("remove-option")]
    [Description("Removes a role from a role menu.")]
    public async Task<Result> RemoveOptionAsync
    (
        [Description("The ID of the role menu to remove the role from.")]
        [AutocompleteProvider(RoleMenuAutocompleteProvider.Identifier)]
        int role_menu_id,
        
        [Description("The role to remove.")]
        IRole role
    )
    {
        // TODO: Handle case of [accidentally] removing all roles from the menu
        var removeResult = await mediator.Send
        (
            new DeleteRoleMenuOption.Request
            (
                role_menu_id,
                context.Interaction.GuildID.Value,
                role.ID
            )
        );

        if (removeResult.IsSuccess)
        {
            var roleMenu = await mediator.Send(new GetRoleMenu.Request(role_menu_id, context.Interaction.GuildID.Value));

            if (!roleMenu.IsSuccess)
            {
                await interactions.CreateFollowupMessageAsync
                (
                    context.Interaction.ApplicationID,
                    context.Interaction.Token,
                    $"(I was able to remove the option, but your role menu has gone missing. " +
                    $"This is a bug in Kobalt, [please report it](https://github.com/VelvetToroyashi/Kobalt/issues/new).)"
                );
            }
            
            await rolemenus.UpdateRoleMenuInitiatorAsync(roleMenu.Entity);
            
            var published = roleMenu.Entity.MessageID != 0;
            var message = published
                ? $"Done. View the changes [here](<https://discord.com/channels/{roleMenu.Entity.GuildID}/{roleMenu.Entity.ChannelID}/{roleMenu.Entity.MessageID}>)."
                : $"Done. Publish the role menu with </role-menu publish:{_publishCommandID}>.";
            
            await interactions.CreateFollowupMessageAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                message
            );
        }
        else
        {
            await interactions.CreateFollowupMessageAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                removeResult.Error!.Message
            );
        }

        return Result.FromSuccess();
    }
    
    [Command("edit-option")]
    [Description("Edits a role in a role menu.")]
    public async Task<Result> EditOptionAsync
    (
        [Description("The ID of the role menu to edit the role in.")]
        [AutocompleteProvider(RoleMenuAutocompleteProvider.Identifier)]
        int role_menu_id,
        
        [Description("The role to edit.")]
        IRole role,
        
        [Description("The new name of the role.")]
        string name = null,
        
        [Description("The new description of the role.")]
        string? description = null,
        
        [Description("The new role to replace the old one with.")]
        IRole? new_role = null
    )
    {
        var editResult = await mediator.Send
        (
            new UpdateRoleMenuOption.Request
            (
                role_menu_id,
                context.Interaction.GuildID.Value,
                role.ID,
                name.AsOptional(),
                description.AsOptional(),
                new_role?.ID ?? default(Optional<Snowflake>),
                default,
                default
            )
        );

        if (editResult.IsSuccess)
        {
            var roleMenu = await mediator.Send(new GetRoleMenu.Request(role_menu_id, context.Interaction.GuildID.Value));

            if (!roleMenu.IsSuccess)
            {
                await interactions.CreateFollowupMessageAsync
                (
                    context.Interaction.ApplicationID,
                    context.Interaction.Token,
                    $"(I was able to edit the option, but your role menu has gone missing. " +
                    $"This is a bug in Kobalt, [please report it](https://github.com/VelvetToroyashi/Kobalt/issues/new).)"
                );
            }

            await rolemenus.UpdateRoleMenuInitiatorAsync(roleMenu.Entity);

            await interactions.CreateFollowupMessageAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                $"Done. View the changes [here](https://discord.com/channels/{roleMenu.Entity.GuildID}/{roleMenu.Entity.ChannelID}/{roleMenu.Entity.MessageID})."
            );
        }
        else
        {
            await interactions.CreateFollowupMessageAsync
            (
                context.Interaction.ApplicationID,
                context.Interaction.Token,
                editResult.Error!.Message
            );
        }
        
        return Result.FromSuccess();
    }
    
    private async Task<Result> EnsureValidRoleAsync(IRole role, IEnumerable<RoleMenuOptionEntity> options)
    {
        if (options.Any(opt => opt.RoleID == role.ID))
        {
            return new InvalidOperationError("That role is already in the menu.");
        }

        if (role.ID == context.Interaction.GuildID)
        {
            return new InvalidOperationError("As much as I'd love to, everyone already has the @everyone role.");
        }

        if (role.IsManaged)
        {
            return new InvalidOperationError("That role is managed, meaning it's assigned automatically by Discord.");
        }

        var rolesResult = await guilds.GetGuildRolesAsync(context.Interaction.GuildID.Value);
        var selfMemberResult = await guilds.GetGuildMemberAsync(context.Interaction.GuildID.Value, context.Interaction.ApplicationID);

        if (!rolesResult.IsDefined(out var roles) || !selfMemberResult.IsDefined(out var selfMember))
        {
            return Result.FromError(rolesResult.Error ?? selfMemberResult.Error!);
        }

        var highestSelfRole = roles!.First(r => r.ID == selfMember!.Roles[^1]);
        
        if (role.Position >= highestSelfRole.Position)
        {
            return new InvalidOperationError("That role is higher than my highest role.");
        }
        
        return Result.FromSuccess();
    }
}