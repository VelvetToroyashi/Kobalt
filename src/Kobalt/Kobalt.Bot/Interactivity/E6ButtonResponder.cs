using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Interactivity;

namespace Kobalt.Bot.Interactivity;

public class E6ButtonResponder : InteractionGroup
{
    private readonly IInteractionContext _context;
    private readonly IDiscordRestInteractionAPI _api;

    public E6ButtonResponder(IInteractionContext context, IDiscordRestInteractionAPI api)
    {
        _context = context;
        _api = api;
    }

    /// <summary>
    /// Converts the embeds to be seperate (mobile-friendly).
    /// </summary>
    /// <returns></returns>
    [Button("e6-mobile-mode")]
    [SuppressInteractionResponse(true)]
    public Task<Result> MobileModeAsync()
    {
        return _api.CreateInteractionResponseAsync
        (
            _context.Interaction.ID,
            _context.Interaction.Token,
            new InteractionResponse
            (
                InteractionCallbackType.UpdateMessage,
                new
                (
                    new InteractionMessageCallbackData
                    (
                        Components: new(GetComponents(_context.Interaction.Message.Value.Components.Value)),
                        Embeds: _context
                                .Interaction
                                .Message
                                .Value
                                .Embeds
                                .Cast<Embed>()
                                .Select(e => e with { Url = default })
                                .ToArray()
                    )
                )
            )
        );
    }

    /// <summary>
    /// Converts the embeds to be joined (desktop-friendly).
    /// </summary>
    [Button("e6-desktop-mode")]
    [SuppressInteractionResponse(true)]
    public Task<Result> DesktopModeAsync()
    {
        return _api.CreateInteractionResponseAsync
        (
            _context.Interaction.ID,
            _context.Interaction.Token,
            new InteractionResponse
            (
                InteractionCallbackType.UpdateMessage,
                new(
                    new InteractionMessageCallbackData
                    (
                        Components: new(GetComponents(_context.Interaction.Message.Value.Components.Value)),
                        Embeds: _context
                                .Interaction
                                .Message
                                .Value
                                .Embeds
                                .Cast<Embed>()
                                .Select((e, n) => e with { Url = $"https://e621.net/posts?page={n / 4}" })
                                .ToArray()
                    )
                )
            )
        );
    }

    /// <summary>
    /// Updates a component list based on the current state of the components.
    /// </summary>
    /// <param name="components">The components to update</param>
    /// <returns>A new list of components, containing a mixture of the old components and new (updated) components.</returns>
    private static IReadOnlyList<IActionRowComponent> GetComponents(IReadOnlyList<IMessageComponent> components)
    {
        var realComponents = components.Cast<ActionRowComponent>().ToArray();
        var returnComponents = new IActionRowComponent[realComponents.Length];

        for (int i = 0; i < realComponents.Length; i++)
        {
            var buildingComponents = new List<IMessageComponent>();

            for (int j = 0; j < realComponents[i].Components.Count; j++)
            {
                var button = (IButtonComponent)realComponents[i].Components[j];

                if (!button.CustomID.IsDefined(out var customID))
                {
                    buildingComponents.Add(button);
                    continue;
                }

                if (customID.EndsWith("e6-mobile-mode"))
                {
                    buildingComponents.Add
                    (
                        new ButtonComponent
                        (
                            ButtonComponentStyle.Primary,
                            "Desktop Mode",
                            default,
                            CustomIDHelpers.CreateButtonID("e6-desktop-mode")
                        )
                    );
                }
                else if (customID.EndsWith("e6-desktop-mode"))
                {
                    buildingComponents.Add
                    (
                        new ButtonComponent
                        (
                            ButtonComponentStyle.Primary,
                            "Mobile Mode",
                            default,
                            CustomIDHelpers.CreateButtonID("e6-mobile-mode")
                        )
                    );
                }

            }

            returnComponents[i] = new ActionRowComponent(buildingComponents);
        }

        return returnComponents;
    }
}
