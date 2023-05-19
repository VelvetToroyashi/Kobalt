using System.Text.Json;
using System.Text.Json.Serialization;
using Kobalt.Phishing.Shared.Models;
using Kobalt.Shared.Extensions;
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

var app = builder.Build();

app.MapPost("/phishing/check/{guildID}/user", (ulong guildID, [FromQuery] string username, [FromQuery] ulong? id, [FromQuery] string? hash) => new { });
app.MapPut("/phishing/{guildID}/username", (ulong guildID, [FromBody] SubmitUsernameRequest request) => new { });

app.Run();
