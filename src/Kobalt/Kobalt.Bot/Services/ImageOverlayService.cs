﻿using Remora.Results;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Kobalt.Bot.Services;

public sealed class ImageOverlayService
{
    private readonly HttpClient _httpClient;

    public ImageOverlayService(IHttpClientFactory httpClient) => _httpClient = httpClient.CreateClient();

    public async Task<Result<Stream>> OverlayAsync(string sourceUrl, string overlayUrl, float intensity, float greyscale)
    {
        var sourceResult = await _httpClient.GetAsync(sourceUrl);

        if (!sourceResult.IsSuccessStatusCode)
        {
            return Result<Stream>.FromError(new NotFoundError("Failed to fetch source image."));
        }

        var overlayResult = await _httpClient.GetAsync(overlayUrl);

        if (!overlayResult.IsSuccessStatusCode)
        {
            return Result<Stream>.FromError(new NotFoundError("Failed to fetch overlay image."));
        }

        await using var sourceStream = await sourceResult.Content.ReadAsStreamAsync();
        await using var overlayStream = await overlayResult.Content.ReadAsStreamAsync();

        using var sourceImage = await Image.LoadAsync(sourceStream);
        using var overlayImage = await Image.LoadAsync(overlayStream);

        var imageToSize = sourceImage.Width > overlayImage.Width ? (sourceImage, overlayImage) : (overlayImage, sourceImage);

        imageToSize.Item1.Mutate(x => x.Resize(imageToSize.Item2.Width, imageToSize.Item2.Height));

        if (greyscale > 0)
        {
            sourceImage.Mutate(x => x.Grayscale(greyscale / 100));
        }

        sourceImage.Mutate(x => x.DrawImage(overlayImage, PixelColorBlendingMode.Multiply, PixelAlphaCompositionMode.SrcAtop, intensity / 100));

        var ms = new MemoryStream();

        sourceImage.SaveAsPng(ms);

        ms.Position = 0;

        return Result<Stream>.FromSuccess(ms);
    }

}
