using Kobalt.Infractions.API.Services;
using Kobalt.Infractions.Data.Mediator;
using Kobalt.Infractions.Infrastructure.Interfaces;
using Kobalt.Infractions.Infrastructure.Mediator;
using Kobalt.Infractions.Infrastructure.Mediator.DTOs;
using Kobalt.Infractions.Infrastructure.Mediator.Mediator;
using Kobalt.Infractions.Shared.Payloads;
using Kobalt.Shared.Extensions;
using Kobalt.Shared.Services;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Remora.Results;

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


app.MapPatch("/infractions/guilds/{guildID}/{id}", async (ulong guildID, int id, [FromBody] InfractionUpdatePayload payload, IMediator mediator) =>
{
    var result = await mediator.Send(new UpdateInfractionRequest(id, guildID, payload.IsHidden, payload.Reason, payload.ExpiresAt));
    
    if (!result.IsDefined(out var infraction))
    {
        return Results.NotFound();
    }
    
    return Results.Ok(infraction);
});

app.MapGet("/infractions/guilds/{guildID}/users/{id}", async (ulong guildID, ulong id, IMediator mediator) =>
{
    var result = await mediator.Send(new GetInfractionsForUserRequest(guildID, id));
    
    var infractions = result.ToArray();
    
    if (!infractions.Any())
    {
        return Results.NoContent();
    }
    
    return Results.Ok(infractions);
});

app.MapGet("/infractions/{guildID}/rules", async (ulong guildID, IMediator mediator) =>
{
    var result = await mediator.Send(new GetGuildInfractionRulesRequest(guildID));
    
    return Results.Ok(result);
});

app.MapPost("/infractions/{guildID}/rules", async (ulong guildID, InfractionRuleDTO rule, IMediator mediator) =>
{
    var result = await mediator.Send(new CreateInfractionRuleRequest(guildID, rule.ActionType, rule.EffectiveTimespan, rule.MatchValue, rule.MatchType, rule.ActionDuration));
    
    if (!result.IsDefined(out var createdRule))
    {
        return Results.BadRequest(result.Error.Message);
    }
    
    return Results.CreatedAtRoute("/infractions/{guildID}/rules/{id}", new { guildID, id = createdRule.Id }, createdRule);
});


app.MapPatch("/infractions/{guildID}/rules/{id}", async (ulong guildID, int id, [FromBody] InfractionRuleUpdatePayload rule, IMediator mediator) =>
{
    var result = await mediator.Send(new UpdateGuildInfractionRuleRequest(id, guildID, rule));
    
    if (!result.IsDefined(out var updatedRule))
    {
        return result.Error is InvalidOperationError ? Results.BadRequest(result.Error.Message) : Results.NotFound();
    }
    
    return Results.Ok(updatedRule);    
});


app.MapDelete("/infractions/{guildID}/rules/{id}", async (ulong guildID, int id, IMediator mediator) =>
{
    var result = await mediator.Send(new RemoveGuildInfractionRuleRequest(id, guildID));
    
    if (!result.IsSuccess)
    {
        return Results.NotFound();
    }
    
    return Results.NoContent();
});

app.Run();

void AddInfractionServices(IServiceCollection services)
{
    services.AddSingleton<InfractionService>();
    services.AddHostedService<InfractionService>();
    services.AddSingleton<IInfractionService>(x => x.GetRequiredService<InfractionService>());

    services.AddSingleton<WebsocketManagerService>();
}