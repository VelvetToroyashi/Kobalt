using System.Net.WebSockets;
using System.Text.Json;
using Kobalt.Infrastructure.Types;
using Kobalt.Infrastructure.Types.ShardCoordinator;
using Kobalt.ShardCoordinator.Services;
using Microsoft.AspNetCore.Authentication;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Rest;
using Remora.Discord.Rest.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddDiscordRest(s => (s.GetRequiredService<IConfiguration>()["Discord:Token"], DiscordTokenType.Bot));
builder.Services.AddDiscordCaching();

builder.Services.Configure<KobaltConfig>(builder.Configuration);

builder.Services
       .AddSingleton<SessionManager>()
       .AddSingleton<WebsocketManagerService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.UseWebSockets();

// Either gets a new session if all pre-existing ones are taken
// or returns an existing, unused session
app.MapPost
(
    "/shards", async (HttpContext context, SessionManager manager) =>
    {
        var sessionResult = await manager.GetNextAvailableSessionAsync();

        if (!sessionResult.IsDefined(out var session))
        {
            // 409 because we're out of shards; try again in ~20 seconds
            context.Response.Headers["X-Retry-After"] = "20000";
            return Results.Conflict();
        }
        else
        {
            // 201 is "Created", though it's not gaurenteed to be a new session.
            return Results.Created($"/shards/{session.ShardID}", session);
        }
    }
);

// DELETE /shards/{id} to release a session, saving its updated information
// these sessions are reassigned to any connecting client during their POST request
app.MapDelete
(
    "/shards/{shardID}", async (HttpContext context, int shardID, WebsocketManagerService websocketManager, SessionManager sessions) =>
    {
        var sessionId = context.Request.Headers["X-Session-ID"].ToString();
        
        if (!sessions.IsValidSession(sessionId, shardID, out var parsedSessionId))
        {
            return Results.Unauthorized();
        }
        
        var body = await JsonSerializer.DeserializeAsync<ClientGatewaySessionData>(context.Request.Body);
        
        if (string.IsNullOrEmpty(body.GatewaySessionID) || body.Sequence is 0)
        {
            return Results.BadRequest();
        }

        var sessionResult = await websocketManager.TerminateClientSessionAsync(parsedSessionId, body.GatewaySessionID, body.Sequence);
        
        if (!sessionResult.IsSuccess)
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }
);

app.Use(async (context, next) =>
{
    if (context.Request.Path == "/ws")
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var socketHelper = context.RequestServices.GetRequiredService<WebsocketManagerService>();
            
            await socketHelper.HandleConnectionAsync(webSocket, context.Request.GetTypedHeaders(), context.RequestAborted);
        }
        else
        {
            await context.ForbidAsync();
        }
    }
    else
    {
        await next();
    }
});

await app.RunAsync();