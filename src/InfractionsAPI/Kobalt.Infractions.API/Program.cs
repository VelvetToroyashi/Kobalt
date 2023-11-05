using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kobalt.Infractions.API.Services;
using Kobalt.Infractions.Data;
using Kobalt.Infractions.Data.MediatR;
using Kobalt.Infractions.Shared.DTOs;
using Kobalt.Infractions.Shared.Interfaces;
using Kobalt.Infractions.Shared.Payloads;
using Kobalt.Shared.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Json;
using Remora.Rest.Json.Policies;
using Remora.Results;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRabbitMQ();
builder.Services.AddControllers();
builder.Services.AddSerilogLogging();
builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true);

AddInfractionServices(builder.Services);

var configure = (JsonSerializerOptions options) =>
{
    options.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    options.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
    options.Converters.Insert(0, new SnowflakeConverter(1420070400000));
    options.Converters.Insert(1, new ISO8601DateTimeOffsetConverter());
};

builder.Services
       .Configure(configure)
       .ConfigureHttpJsonOptions(opt => configure(opt.SerializerOptions));

var app = builder.Build();
app.MapControllers();

app.MapPut("/infractions/guilds/{guildID}", async (ulong guildID, [FromBody] InfractionCreatePayload infraction, IInfractionService infractions) =>
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
        infraction.ReferencedID
    );

    if (!result.IsDefined(out var created))
    {
        return Results.BadRequest(result.Error!.Message);
    }

    // If the infraction is freshly created, return 201, otherwise 200.
    return created.CreatedAt < now
        ? Results.Ok(created)
        : Results.Created($"/infractions/guilds/{guildID}/{created.Id}", created);
});

app.MapPost("/infractions/guilds/{guildID}/rules/evaluate/{userID}", async (IInfractionService infractions, ulong guildID, ulong userID) =>
{
    var result = await infractions.EvaluateInfractionsAsync(guildID, userID);

    if (result.IsDefined(out var matched))
    {
        return Results.Json(matched);
    }
    else
    {
        return Results.NoContent();
    }
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

app.MapGet("/infractions/guilds/{guildID}/users/{id}", async (ulong guildID, ulong id, [FromQuery(Name = "with_pardons")] bool includePardons, IMediator mediator) =>
{
    var result = await mediator.Send(new GetInfractionsForUserRequest(guildID, id, includePardons));

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
        return Results.BadRequest(result.Error!.Message);
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

await app.Services.GetRequiredService<IDbContextFactory<InfractionContext>>().CreateDbContext().Database.MigrateAsync();

app.Run();

void AddInfractionServices(IServiceCollection services)
{
    services.AddSingleton<InfractionService>();
    services.AddHostedService<InfractionService>();
    services.AddSingleton<IInfractionService>(x => x.GetRequiredService<InfractionService>());

    services.AddMediatR(s => s.RegisterServicesFromAssemblyContaining<InfractionContext>());
    services.AddDbContextFactory<InfractionContext>("Infractions");
}
