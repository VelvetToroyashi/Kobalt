using System.Buffers;
using Kobalt.Infrastructure.Extensions.Remora;
using Kobalt.ReminderService.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMediator();

builder.Services.AddSingleton<ReminderService>();
builder.Services.AddHostedService(s => s.GetRequiredService<ReminderService>());

var app = builder.Build();

app.UseWebSockets();

app.MapGet("/api/reminders", async (HttpContext context, ReminderService reminders) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }
    
    var cts = new CancellationTokenSource();
    var socket = await context.WebSockets.AcceptWebSocketAsync();
    
    reminders.AddClient(socket, cts);
    
    // Hold the connection open for as long as the client is alive.
    // As soon as this handler returns ASP.NET closes the socket.
    var buffer = ArrayPool<byte>.Shared.Rent(1024);
    await ResultExtensions.TryCatchAsync
    (
        async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                // It's fine to pass a CT here because we don't need to clean anything up.
                _ = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
            }
        }
    );
    
    cts.Cancel();
    ArrayPool<byte>.Shared.Return(buffer);
});

// List a user's reminders
app.MapGet("/api/reminders/{userID}", () => "");

// Create a reminder
app.MapPost("/api/reminders/{userID}", () => "");

// Delete one or more reminders
app.MapDelete("/api/reminders/{userID}", () => "");
app.Run();
