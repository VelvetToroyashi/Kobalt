using FuzzySharp;
using Kobalt.Plugins.RoleMenus.Mediator;
using Kobalt.Plugins.RoleMenus.Models;
using MediatR;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using Remora.Discord.Commands.Contexts;

namespace Kobalt.Plugins.RoleMenus.Services;

public class RoleMenuAutocompleteProvider(IMediator mediator, IInteractionContext context) : IAutocompleteProvider
{
    public const string Identifier = "kobalt::role-menu-autocomplete-provider";
    public string Identity { get; } = Identifier;
    
    public async ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync
    (
        IReadOnlyList<IApplicationCommandInteractionDataOption> options, 
        string userInput,
        CancellationToken ct = default
    )
    {
        var guildId = context.Interaction.GuildID.Value;
        var roleMenus = await mediator.Send(new GetGuildRoleMenus.Request(guildId), ct);
        
        //TODO: Caching, perhaps at the EF Level?
        return roleMenus
               .Where(rm => string.IsNullOrWhiteSpace(userInput) || Fuzz.PartialRatio(rm.Name, userInput) > 80)
               .Select(rm => new ApplicationCommandOptionChoice(GetRoleMenuName(rm), rm.Id.ToString()))
               .ToArray();
    }
    
    private string GetRoleMenuName(RoleMenuEntity roleMenu) => $"\"{roleMenu.Name}\" ({(roleMenu.MessageID.Value is 0 ? "UNPUBLISHED" : "PUBLISHED")})";
    
}