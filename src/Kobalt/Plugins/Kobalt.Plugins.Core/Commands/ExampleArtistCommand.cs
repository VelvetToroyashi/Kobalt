using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Contexts;
using Color = System.Drawing.Color;

namespace Kobalt.Core.Commands;

[Group("artist")]
public class ExampleArtistCommand : CommandGroup
{
    private readonly IInteractionContext _context;
    private readonly IDiscordRestInteractionAPI _api;

    public ExampleArtistCommand(IInteractionContext context, IDiscordRestInteractionAPI api)
    {
        _context = context;
        _api = api;
    }

    [Command("lookup")]
    public async Task<Result> CheckAsync()
    {
        var embed = new Embed
        (
            Title: "Artist Lookup Result",
            Thumbnail: new EmbedThumbnail("https://cdn.discordapp.com/avatars/255834596766253057/dd5d5eda52b3a2a53c1816086feeb986.png?size=4096"),
            Description:
                """
                The images submitted most likely belong to this artist.

                Platforms:
                <:_:861863654097682452> [@Example_art](https://example.com)
                <:_:866329296020701218> [@ExampleArtist](https://example.com)
                <:_:861124851435831317> [~ExampleArtist (FurAffinity)](https://example.com)

                This artist has been verified since <t:1676602838:D>.
                """,
            Colour: Color.LimeGreen

        );

        return (Result)await _api.EditOriginalInteractionResponseAsync(_context.Interaction.ApplicationID, _context.Interaction.Token, embeds: new[] { embed });
    }
}
