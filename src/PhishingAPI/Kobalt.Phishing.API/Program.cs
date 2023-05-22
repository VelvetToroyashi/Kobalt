using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Kobalt.Phishing.API.Services;
using Kobalt.Phishing.Data;
using Kobalt.Phishing.Data.MediatR;
using Kobalt.Phishing.Shared.Models;
using Kobalt.Shared.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Remora.Discord.API;
using Remora.Rest.Json;
using Remora.Rest.Json.Policies;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddSerilogLogging();

var configure = (JsonSerializerOptions options) =>
{
    options.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    options.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
    options.Converters.Insert(0, new SnowflakeConverter(Constants.DiscordEpoch));
    options.Converters.Insert(1, new ISO8601DateTimeOffsetConverter());
};

builder.Services
       .Configure(configure)
       .ConfigureHttpJsonOptions(opt => configure(opt.SerializerOptions));

builder.Services.AddHttpClient
(
    "Phishing", client => client
                         .DefaultRequestHeaders
                         .TryAddWithoutValidation("User-Agent", "Kobalt Anti-Phish by Velvet Toroyashi")
);

builder.Services
       .AddMediatR
        (
            config =>
            {
                config.RegisterServicesFromAssemblyContaining<Program>();
                config.Lifetime = ServiceLifetime.Singleton;
            }
        )
       .AddMemoryCache()
       .AddSingleton<PhishingService>()
       .AddHostedService(s => s.GetRequiredService<PhishingService>());

builder.Services.AddDbContextFactory<PhishingContext>("Phishing");

var app = builder.Build();

app.MapPost("/phishing/check/{guildID}/user", async (ulong guildID, [FromBody] CheckUserRequest user, PhishingService phishing) => Results.Json(await phishing.CheckUserAsync(guildID, user)));
app.MapPost("/phishing/check/domains", ([FromBody] IReadOnlyList<string> domains, PhishingService phishing) => Results.Json(phishing.CheckLinksAsync(domains)));
app.MapPut
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

app.MapPut
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

app.Run();
