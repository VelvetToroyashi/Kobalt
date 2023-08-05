using System.ComponentModel;
using Kobalt.Infrastructure.Services.Booru;
using Kobalt.Infrastructure.Types.Booru;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Interactivity;
using Remora.Rest;
using Color = System.Drawing.Color;

namespace Kobalt.Bot.Commands.NSFW;

/// <summary>
/// Command that queries results from E621.
/// </summary>
public class E621Command : CommandGroup
{
    private readonly IRestHttpClient _client;
    private readonly BooruSearchService _booru;
    private readonly IDiscordRestInteractionAPI _interactions;
    private readonly IInteractionCommandContext _context;

    public E621Command
    (
        IRestHttpClient client,
        BooruSearchService booru,
        IDiscordRestInteractionAPI interactions,
        IInteractionCommandContext context
    )
    {
        _client = client;
        _booru = booru;
        _interactions = interactions;
        _context = context;
    }

    [Command("e621")]
    //[RequireNSFW]
    [Description("Searches E621 for images.")]
    public async Task<Result> SearchAsync
    (
        [Description("The tags to search for.")]
        string tags,

        [MinValue(1)]
        [MaxValue(5)]
        [Description("How many images to return.")]
        int count = 1
    )
    {
        var respondResult = await _interactions.EditOriginalInteractionResponseAsync
        (
             _context.Interaction.ApplicationID,
             _context.Interaction.Token,
             "<:_:859424401723883560> Searching..."
        );

        var searchResult = await _booru.SearchAsync(count, tags);

        if (!searchResult.IsSuccess)
        {
            var errorMessage = $"<:_:908958943466893323> {searchResult.Error.Message}";

            return (Result)await _interactions.EditOriginalInteractionResponseAsync
            (
                _context.Interaction.ApplicationID,
                _context.Interaction.Token,
                errorMessage
            );
        }

        var embeds = GetEmbeds(tags, searchResult.Entity);
        var components = GetComponents(searchResult.Entity);

        var res = (Result)await _interactions.EditOriginalInteractionResponseAsync
        (
            _context.Interaction.ApplicationID,
            _context.Interaction.Token,
            string.Empty,
            embeds: new(embeds),
            components: new(components)
        );

        return res;
    }

    /// <summary>
    /// Generates componeonts based on the given image datas.
    /// </summary>
    /// <param name="search">The search result containing the images that will be displayed.</param>
    /// <returns>A list of action rows with buttons or other components relevant to the images.</returns>
    private IReadOnlyList<IActionRowComponent> GetComponents(QueryResultData search)
    {
        // N.B. If this is changed, update Interactivity/E621Responder.cs as well.
        var components = new IActionRowComponent[2];

        components[0] = new ActionRowComponent
        (
            new[]
            {
                new ButtonComponent(ButtonComponentStyle.Primary, "Mobile Mode", CustomID: CustomIDHelpers.CreateButtonID("e6-mobile-mode")),
                new ButtonComponent(ButtonComponentStyle.Link, "Search e621", URL: search.SearchUrl)
            }
        );

        var directLinks = new ButtonComponent[search.Posts.Count];
        var sourceLinks = new ButtonComponent[search.Posts.Count];

        for (int i = 0; i < search.Posts.Count; i++)
        {
            var post = search.Posts[i];
            var sourceLink = post.Sources.FirstOrDefault()?.ToString() ?? "https://e621.net";
            sourceLinks[i] = new(ButtonComponentStyle.Link, $"Source {i + 1}​ ​ ​ ", URL: sourceLink, IsDisabled: !post.Sources.Any());
        }

        components[1] = new ActionRowComponent(sourceLinks);

        return components;
    }

    private static IReadOnlyList<IEmbed> GetEmbeds(string tags, QueryResultData search)
    {
        var embeds = new Embed[search.Posts.Count];

        for (var i = 0; i < search.Posts.Count; i++)
        {
            var post = search.Posts[i];
            var embed = new Embed
            (
                Title: tags + $" ({i + 1}/{search.Posts.Count})",
                Url: "https://e621.net/posts/",
                Description: $"{post.Score.Up} <:_:909715386843430933> {post.Score.Down} <:_:911135418420953138>  Favorites: {post.FavCount} ",
                Image: new EmbedImage(post.File.Url.ToString()),
                Colour: Color.FromArgb(2, 37, 70)
            );

            embeds[i] = embed;
        }

        return embeds;
    }
}
