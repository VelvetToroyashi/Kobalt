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
    
    // ERRATA: The server should read from the socket in a loop even if the client isn't
    // expected to send anything, as the client will still send control messages which
    // aren't ever seen if we don't read. I was reminded of this by the following SO answer:
    // https://stackoverflow.com/a/49605801 TODO: READ THE SOCKET!
    await ResultExtensions.TryCatchAsync(async () => await Task.Delay(-1, cts.Token));
});

// List a user's reminders
app.MapGet("/api/reminders/{userID}", () => "");

// Create a reminder
app.MapPost("/api/reminders/{userID}", () => "");

// Delete one or more reminders
app.MapDelete("/api/reminders/{userID}", () => "");
app.Run();
