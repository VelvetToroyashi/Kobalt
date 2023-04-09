using Kobalt.Infractions.API.Services;
using Kobalt.Infractions.Data.Mediator;
using Kobalt.Infractions.Infrastructure.Interfaces;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Kobalt.Shared.Extensions;
using Kobalt.Shared.Services;
using Mediator;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSerilogLogging();

AddInfractionServices(builder.Services);

var app = builder.Build();
app.MapControllers();

app.MapPut("/infractions/guilds/{guildID}", async (HttpResponse response, ulong guildID, [FromBody] InfractionDTO infraction, IInfractionService infractions) =>
{
    var now = DateTimeOffset.UtcNow;
    var result = await infractions.CreateInfractionAsync
    (
        guildID,
        infraction.UserID,
        infraction.ModeratorID,
        infraction.Type,
        infraction.Reason,
        infraction.ExpiresAt,
        infraction.ReferencedId
    );

    if (!result.IsDefined(out infraction))
    {
        return Results.BadRequest(result.Error.Message);
    }
    
    // If the infraction is freshly created, return 201, otherwise 200.
    return infraction.CreatedAt < now
        ? Results.Ok(infraction) 
        : Results.CreatedAtRoute("/infractions/guilds/{guildID}/{id}", new { guildID, id = infraction.Id }, infraction);
});
app.MapGet("/infractions/guilds/{guildID}/{id}", async (ulong guildID, int id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetGuildInfractionRequest(id, guildID));

    if (!result.IsDefined(out var infraction))
    {
        return Results.NotFound();
    }

    return Results.Ok(infraction);
});
app.MapPatch("/infractions/guilds/{guildID}/{id}", () => {});
app.MapGet("/infractions/guilds/{guildID}/users/{id}", () => {});

app.MapGet("/infractions/{guildID}/rules", () => {});
app.MapPost("/infractions/{guildID}/rules", () => {});
app.MapPatch("/infractions/{guildID}/rules/{id}", () => {});
app.MapDelete("/infractions/{guildID}/rules/{id}", () => {});

app.Run();

void AddInfractionServices(IServiceCollection services)
{
    services.AddSingleton<InfractionService>();
    services.AddHostedService<InfractionService>();
    services.AddSingleton<IInfractionService>(x => x.GetRequiredService<InfractionService>());

    services.AddSingleton<WebsocketManagerService>();
}