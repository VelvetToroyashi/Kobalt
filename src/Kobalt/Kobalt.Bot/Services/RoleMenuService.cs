using Kobalt.Bot.Data.Entities.RoleMenus;
using Kobalt.Bot.Data.MediatR.RoleMenus;
using MediatR;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Interactivity;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Bot.Services;

public class RoleMenuService
(
    IMediator mediator,
    IDiscordRestGuildAPI guilds,
    IDiscordRestChannelAPI channels,
    IDiscordRestInteractionAPI interactions,
    ILogger<RoleMenuService> logger
)
{
    public const string RoleMenuID = "kobalt-role-menu";

    public const string DefaultRoleMenuMessage = """
                                                 **Role-Menu**
                                                 
                                                 Press the button to acquire some roles.
                                                 """;

    private static readonly IReadOnlyList<IMessageComponent> RoleMenuInitiatorComponents =
        new IMessageComponent[]
        {
            new ActionRowComponent
            (
                new[]
                {
                    new ButtonComponent
                    (
                        ButtonComponentStyle.Primary,
                        "Get Roles",
                        CustomID: CustomIDHelpers.CreateButtonID(RoleMenuID)
                    ),
                }
            ),
        };
    
    /// <summary>
    /// Publishes a role menu if it does not exist.
    /// </summary>
    /// <param name="roleMenu">The role menu to publish.</param>
    /// <returns>
    /// A result that may or not have succeeded.
    /// Generally, either the role menu is already published, or there was a permission issue regarding publishing.
    /// </returns>
    public async Task<Result> PublishRoleMenuAsync(RoleMenuEntity roleMenu)
    {
        // Zero is a sentinel value for an unpublished role menu;
        // if the menu has already been published, we should check if the message still exists.
        if (roleMenu.MessageID != 0)
        {
            var message = await channels.GetChannelMessageAsync(roleMenu.ChannelID, roleMenu.MessageID);
            
            if (message.IsSuccess)
            {
                return new InvalidOperationError("This role menu has already been published.");
            }
        }

        if (!roleMenu.Options.Any())
        {
            return new InvalidOperationError("The role menu must have at least one option.");
        }

        var content = string.IsNullOrWhiteSpace(roleMenu.Description) ? DefaultRoleMenuMessage : roleMenu.Description;

        var result = await channels.CreateMessageAsync
        (
            roleMenu.ChannelID,
            content: content,
            components: new Optional<IReadOnlyList<IMessageComponent>>(RoleMenuInitiatorComponents)
        );

        if (result.IsSuccess)
        {
            var updateResult = await mediator.Send(new UpdateRoleMenu.Request(roleMenu.Id, roleMenu.GuildID, MessageID: result.Entity.ID));
            
            if (!updateResult.IsSuccess)
            {
                logger.LogError("Failed to update role menu with new message ID ({RoleMenuID})", roleMenu.Id);
                return new InvalidOperationError("Something's gone wrong while publishing the role menu; " +
                                                 "this is probably a bug in Kobalt, please [report it](<https://github.com/VelvetToroyashi/Kobalt/issues/new>).");
            }
        }
        
        return (Result)result;
    }

    /// <summary>
    /// Updates the content of an existing role menu.
    /// </summary>
    /// <param name="roleMenu">The role menu to be updated.</param>
    /// <returns>A result that may or not have succeeded.</returns>
    public async Task<Result> UpdateRoleMenuInitiatorAsync(RoleMenuEntity roleMenu)
    {
        var content = string.IsNullOrWhiteSpace(roleMenu.Description) ? DefaultRoleMenuMessage : roleMenu.Description;
        
        var res = await channels.EditMessageAsync(roleMenu.ChannelID, roleMenu.MessageID, content: content);

        if (!res.IsSuccess)
        {
            return new NotFoundError("Sorry, but I can't edit that menu. Does it still exist?");
        }
        
        return Result.FromSuccess();
    }

    /// <summary>
    /// Displays the role menu dropdown.
    /// </summary>
    /// <param name="interaction">The initial interaction that initiates the role menu flow.</param>
    /// <returns>A result that may or not have succeeded.</returns>
    public async Task<Result> DisplayRoleMenuAsync(IInteraction interaction)
    {
        if (interaction.Type is not InteractionType.MessageComponent)
        {
            return new InvalidOperationError("This interaction is not a message component.");
        }
        
        if (interaction.Data.Value.AsT1.CustomID.Split("::")[^1] != RoleMenuID)
        {
            return Result.FromSuccess();
        }
        
        var getRoleMenu = await mediator.Send(new GetRoleMenuByMessage.Request(interaction.Message.Value.ID));

        if (!getRoleMenu.IsDefined(out var roleMenu))
        {
            logger.LogWarning("Parent role menu message no longer exists, but the dropdown does, old message? ({GuildID})", interaction.GuildID.Value);
                
            return new NotFoundError("Sorry! This role menu no longer exists; " +
                                     "this is probably a bug in Kobalt—please [report it](<https://github.com/VelvetToroyashi/Kobalt/issues/new>).");
        }
        
        var description = string.IsNullOrWhiteSpace(getRoleMenu.Entity.Description) ? DefaultRoleMenuMessage : getRoleMenu.Entity.Description;
        var options = roleMenu.Options.Select(it => GetSelectOption(it, interaction)).ToArray();

        var maxSelections = roleMenu.MaxSelections is 0 ? options.Length : roleMenu.MaxSelections;

        var dropdown = new StringSelectComponent
        (
            CustomIDHelpers.CreateSelectMenuIDWithState(RoleMenuID, interaction.Message.Value.ID.ToString()),
            options,
            "Self-assignable roles",
            MaxValues: maxSelections
        );

        var result = await interactions.CreateFollowupMessageAsync
        (
            interaction.ApplicationID,
            interaction.Token,
            content: description,
            components: new IMessageComponent[] { new ActionRowComponent(new[] { dropdown })},
            flags: MessageFlags.Ephemeral
        );
        
        return (Result)result;
    }

    /// <summary>
    /// Assigns roles to a user based on the selected options.
    /// </summary>
    /// <param name="sourceMessageID">The ID of the parent message.</param>
    /// <param name="selectedRoleIDs"></param>
    /// <param name="interaction"></param>
    /// <returns>A result that may or not have succeeded</returns>
    public async Task<Result> AssignRoleMenuRolesAsync
    (
        Snowflake sourceMessageID,
        IReadOnlyList<Snowflake> selectedRoleIDs,
        IInteraction interaction
    )
    {
        var getRoleMenu = await mediator.Send(new GetRoleMenuByMessage.Request(sourceMessageID));
        
        var guildID = interaction.GuildID.Value;
        var userID = interaction.Member.Value.User.Value.ID;

        if (!getRoleMenu.IsDefined(out var roleMenu))
        {
            logger.LogWarning("Parent role menu message no longer exists, but the dropdown does, old message? ({GuildID})", guildID);
                
            return new NotFoundError("Sorry! This role menu no longer exists; this is probably a bug in Kobalt—please [report it]([https://github.com/VelvetToroyashi/Kobalt/issues/new>).");
        }
        
        var selectedOptions = roleMenu.Options.Where(it => selectedRoleIDs.Contains(it.RoleID)).ToArray();
        var validityResult = RoleHelper.EnsureRoleValidity(selectedOptions, interaction.Member.Value.Roles);

        if (!validityResult.IsSuccess)
        {
            return (Result)await interactions.CreateFollowupMessageAsync
            (
                interaction.ApplicationID,
                interaction.Token,
                validityResult.Error!.Message,
                flags: MessageFlags.Ephemeral
            );
        }

        var normalizedRoles = RoleHelper.DetermineCorrectRoles
        (
            interaction.Member.Value.Roles,
            selectedRoleIDs, 
            roleMenu.Options.Select(r => r.RoleID).ToArray()
        );
        
        var roleUpdateResult = await guilds.ModifyGuildMemberAsync
        (
            guildID,
            userID,
            roles: new Optional<IReadOnlyList<Snowflake>?>(normalizedRoles)
        );

        if (!roleUpdateResult.IsSuccess)
        {
            return (Result)await interactions.CreateFollowupMessageAsync
            (
                interaction.ApplicationID,
                interaction.Token,
                "Sorry. I wasn't able to update your roles. Perhaps one has gone missing, or I don't have permission to assign it.",
                flags: MessageFlags.Ephemeral
            );
        }
        
        var newDropdown = RoleHelper.GetDropdownFromSelections(selectedOptions, interaction);
        
        await interactions.EditOriginalInteractionResponseAsync
        (
            interaction.ApplicationID,
            interaction.Token,
            "Enjoy the new roles.",
            components: new IMessageComponent[] { new ActionRowComponent(new[] { newDropdown })}
        );

        return Result.FromSuccess();
    }
    
    private ISelectOption GetSelectOption(RoleMenuOptionEntity option, IInteraction interaction)
    {
        var roles = interaction.Member.Value.Roles;
        
        return new SelectOption(option.Name, option.RoleID.ToString(), option.Description, IsDefault: roles.Contains(option.RoleID));
    }
}

file record RoleValidationError(string Message) : ResultError(Message);

file static class RoleHelper
{
    public static Result EnsureRoleValidity(IReadOnlyList<RoleMenuOptionEntity> selectedOptions, IReadOnlyList<Snowflake> userRoles)
    {
        var optionIDs = selectedOptions.Select(s => s.RoleID);
        var concatenatedUserRoles = userRoles.Concat(optionIDs).ToArray();
        
        foreach (var option in selectedOptions)
        {
            if (option.MutuallyInclusiveRoles.Intersect(concatenatedUserRoles).Count() < option.MutuallyInclusiveRoles.Count)
            {
                var missingInclusiveRoles = option.MutuallyInclusiveRoles.Except(concatenatedUserRoles);
                return new RoleValidationError($"Unfortunately, <@&{option.RoleID}> requires the following role(s): " +
                                               $"{string.Join("\n- ", missingInclusiveRoles.Select(it => $"<@&{it}>"))}");
            }
            
            if (option.MutuallyExclusiveRoles.Intersect(concatenatedUserRoles).Any())
            {
                var conflictingExclusiveRoles = option.MutuallyExclusiveRoles.Intersect(concatenatedUserRoles);
                return new RoleValidationError($"Unfortunately, <@&{option.RoleID}> cannot be selected with the following role(s): " +
                                               $"{string.Join("\n- ", conflictingExclusiveRoles.Select(it => $"<@&{it}>"))}");
            }
        }

        return Result.FromSuccess();
    }
    
    /// <summary>
    /// Determines the user's new roles based on their current and selected roles.
    /// </summary>
    /// <param name="userRoles">The user's current roles.</param>
    /// <param name="selectedRoles">The user's selected roles.</param>
    /// <param name="potentialRoles">The roles that can be selected.</param>
    /// <returns>A list containing the user's new roles, based on the input.</returns>
    public static IReadOnlyList<Snowflake> DetermineCorrectRoles
    (
        IReadOnlyList<Snowflake> userRoles,
        IReadOnlyList<Snowflake> selectedRoles,
        IReadOnlyList<Snowflake> potentialRoles
    )
    {
        var rolesToRemove = userRoles.Intersect(potentialRoles).Except(selectedRoles).ToArray();
        var rolesToAdd = selectedRoles.Except(userRoles).ToArray();

        var roles = new List<Snowflake>(userRoles);

        roles.AddRange(rolesToAdd);
        roles.RemoveAll(it => rolesToRemove.Contains(it));

        return roles;
    }

    public static StringSelectComponent GetDropdownFromSelections(RoleMenuOptionEntity[] selectedOptions, IInteraction interaction)
    {
        var selectedRoleIDs = selectedOptions.Select(it => it.RoleID.ToString()).ToArray();
        var dropdown = (IStringSelectComponent)((IActionRowComponent)interaction.Message.Value.Components.Value[0]).Components[0];

        var ret = new StringSelectComponent(dropdown.CustomID, dropdown.Options.Select(GetSelectOption).ToArray(), dropdown.Placeholder, MaxValues: dropdown.MaxValues);

        ISelectOption GetSelectOption(ISelectOption opt)
        {
            var selected = selectedRoleIDs.Contains(opt.Value);
            
            return new SelectOption(opt.Label, opt.Value, opt.Description, IsDefault: selected);
        }
        
        return ret;
    }
}