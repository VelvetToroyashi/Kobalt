using System.ComponentModel;
using Kobalt.Plugins.Core.Services;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;

namespace Kobalt.Plugins.Core.Commands;

public class OverlayCommand : CommandGroup
{
    private readonly ImageOverlayService _overlay;
    private readonly IDiscordRestInteractionAPI _interactions;
    private readonly IInteractionCommandContext _context;

    public OverlayCommand(ImageOverlayService overlay, IDiscordRestInteractionAPI interactions, IInteractionCommandContext context)
    {
        _overlay = overlay;
        _interactions = interactions;
        _context = context;
    }

    [Command("overlay")]
    [Ephemeral]
    [Description("Overlays two images")]
    public async Task<Result> OverlayImagesAsync
    (
        [Description("The image to apply the overlay to.")]
        IAttachment @base,

        [Description("The image to overlay on top of the base image.")]
        IAttachment overlay,

        [MinValue(50)]
        [MaxValue(100)]
        [Description("The intensity of the overlay.")]
        float intensity = 100f,

        [MinValue(0)]
        [MaxValue(100)]
        [Description("The position of the overlay.")]
        float greyscale = 0f
    )
    {
        var result = await _overlay.OverlayAsync(@base.Url, overlay.Url, intensity, greyscale);

        if (result.IsSuccess)
        {
            return (Result)await _interactions.EditOriginalInteractionResponseAsync
            (
                _context.Interaction.ApplicationID,
                _context.Interaction.Token,
                attachments: new OneOf<FileData, IPartialAttachment>[] { new FileData("output.png", result.Entity, null!) }
            );
        }

        await _interactions.EditOriginalInteractionResponseAsync
        (
            _context.Interaction.ApplicationID,
            _context.Interaction.Token,
            $"There was an error processing your request. {result.Error.Message}"
        );

        return (Result)result;
    }
}
