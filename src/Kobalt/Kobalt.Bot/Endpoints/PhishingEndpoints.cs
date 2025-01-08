using System.Text.RegularExpressions;
using Kobalt.Bot.Data.MediatR.Phishing;
using Kobalt.Bot.Services;
using Kobalt.Shared.Models.Phishing;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Remora.Rest.Core;

namespace Kobalt.Bot.Endpoints;

public static class PhishingEndpoints
{
    public static void AddPhishingEndpoints(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/phishing/check/{guildID}/user", async (ulong guildID, [FromBody] CheckUserRequest user, PhishingService phishing) => Results.Json(await phishing.CheckUserAsync(new Snowflake(guildID), user)));
        builder.MapPost("/phishing/check/domains", ([FromBody] IReadOnlyList<string> domains, PhishingService phishing) => Results.Json(phishing.CheckLinks(domains)));
        builder.MapPut
        (
            "/phishing/{guildID}/username",
            async (ulong guildID, [FromBody] SubmitUsernameRequest request, IMediator mediator) =>
            {
                var res = await mediator.Send(new CreateSuspiciousUsername.Request(guildID, request.UsernamePattern, request.ParseType));

                if (res.IsSuccess)
                {
                    return Results.Created();
                }

                return Results.BadRequest(new { message = res.Error.Message });
            }
        );

        builder.MapPut
        (
            "/phishing/{guildID}/avatar",
            async (ulong guildID, [FromBody] SubmitAvatarRequest request, IMediator mediator, PhishingService phishing) =>
            {
                var hashResult = await phishing.HashImageAsync(request.Url);

                if (!hashResult.IsDefined(out var hash))
                {
                    return Results.BadRequest(new { message = hashResult.Error!.Message });
                }

                var md5 = Regex.Match(request.Url, @"https?:\/\/cdn\.discordapp\.com\/avatars\/[0-9]{17,19}\/(a_?)(?<Hash>\S+)\.png").Groups["Hash"].Value;

                var res = await mediator.Send(new CreateSuspiciousAvatar.Request(guildID, md5, request.AddedBy.Value, request.Url, request.Category, hash));

                if (res.IsSuccess)
                {
                    return Results.Created();
                }

                return Results.BadRequest(res.Error);
            }
        );
    }

}
