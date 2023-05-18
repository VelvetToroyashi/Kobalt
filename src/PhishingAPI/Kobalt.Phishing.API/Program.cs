using Kobalt.Phishing.Shared.Models;
using Kobalt.Shared.Extensions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddSerilogLogging();

var app = builder.Build();

app.MapPost("/phishing/check/{guildID}/user", (ulong guildID, [FromQuery] string username, [FromQuery] ulong? id, [FromQuery] string? hash) => new { });
app.MapPut("/phishing/{guildID}/username", (ulong guildID, [FromBody] SubmitUsernameRequest request) => new { });

app.Run();
