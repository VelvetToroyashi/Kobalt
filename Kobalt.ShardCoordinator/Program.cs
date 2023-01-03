using System.Net;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kobalt.ShardCoordinator.Services;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
var app = builder.Build();
app.UseHttpsRedirection();
app.MapControllers();

app.UseWebSockets();

app.MapPost
(
    "/shards", async (HttpContext Context, SessionManager Manager) =>
    {
        var shardResult = await Manager.GetNextAvailableSessionAsync();

        if (!shardResult.IsDefined(out var shard))
        {
            // 409 because we're out of shards; try again in ~20 seconds
            Context.Response.StatusCode = 409;
            Context.Response.Headers["X-Retry-After"] = "20000";
        }
        else
        {
            // 201 is "Created", though it's not gaurenteed to be a new session.
            Context.Response.StatusCode = 201;
            Context.Response.WriteAsJsonAsync
            (
                shard, 
                new JsonSerializerOptions()
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                }
            );
        }
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

app.Run();
