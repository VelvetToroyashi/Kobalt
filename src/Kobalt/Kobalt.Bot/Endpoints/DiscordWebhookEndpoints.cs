using System.Text;
using Kobalt.Infrastructure.Services;
using Kobalt.Infrastructure.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RemoraHTTPInteractions.Services;

namespace Kobalt.Bot.Endpoints;

public static class DiscordWebhookEndpoints
{
    public static void MapDiscordWebhookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost
        (
            "/interaction",
            async (HttpContext ctx, WebhookInteractionHelper handler, IOptions<KobaltConfig> config) =>
            {
                if (!config.Value.Bot.EnableHTTPInteractions)
                {
                    ctx.Response.StatusCode = 403;

                    return;
                }

                var hasHeaders = DiscordHeaders.TryExtractHeaders(ctx.Request.Headers, out var timestamp, out var signature);
                var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();

                if (!hasHeaders ||
                    !DiscordHeaders.VerifySignature
                    (
                        body,
                        timestamp!,
                        signature!,
                        config.Value.Discord.PublicKey!
                    ))
                {
                    ctx.Response.StatusCode = 401;
                    Console.WriteLine("Interaction Validation Failed.");

                    return;
                }

                var result = await handler.HandleInteractionAsync(body);

                if (!result.IsDefined(out var content))
                {
                    ctx.Response.StatusCode = 500;
                    Console.WriteLine($"Interaction Handling Failed. Result: {result.Error}");

                    return;
                }

                if (!content.Item2.HasValue)
                {
                    ctx.Response.Headers.ContentType = "application/json";
                    await ctx.Response.WriteAsync(content.Item1);
                }
                else
                {
                    var ret = new MultipartResult().AddPayload(new MemoryStream(Encoding.UTF8.GetBytes(result.Entity.Item1)));

                    foreach ((var key, var value) in result.Entity.Item2.Value)
                        ret.Add(key, value);

                    await ret.ExecuteResultAsync(new ActionContext(ctx, ctx.GetRouteData(), new()));
                }
            }
        );
    }

    // TODO: Implement proper AUTHORIZATION_CREATE when Remora adds support for it.
}
