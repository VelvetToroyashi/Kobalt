using System.Buffers;
using Kobalt.Infrastructure.DTOs.Reminders;
using Kobalt.Infrastructure.Extensions.Remora;
using Kobalt.ReminderService.API.Services;
using Kobalt.ReminderService.Data.Mediator;
using Mediator;
using Microsoft.AspNetCore.Mvc;

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
app.MapGet("/api/reminders/{userID}", async (ulong userID, IMediator mediator) =>
{
    var userReminders = await mediator.Send(new GetRemindersForUser.Request(userID));

    return Results.Json(userReminders);
});

// Create a reminder
app.MapPost("/api/reminders/{userID}", async (ulong userID, [FromBody] ReminderDTO reminder, IMediator mediator) =>
{
    var createdReminder = await mediator.Send
    (
        new CreateReminder.Request
        (
            reminder.AuthorID.Value,
            reminder.ChannelID.Value,
            reminder.GuildID?.Value,
            reminder.ReplyContent,
            reminder.Expiration,
            reminder.ReplyMessageID?.Value
        )
    );

    var ret = new ReminderCreationPayload(createdReminder.Id, createdReminder.Expiration);
    
    return Results.Json(ret);
});

// Delete one or more reminders
app.MapDelete("/api/reminders/{userID}/", async ([FromBody] int[] reminderIDs, ulong userID, ReminderService reminders) =>
{
    var result = new ReminderDeletionPayload(new(), new());
    
    foreach (var reminderID in reminderIDs)
    {
        var deletionResult = await reminders.RemoveReminderAsync(reminderID, userID);

        if (deletionResult.IsSuccess)
            result.CancelledReminders.Add(reminderID);
        else
            result.InvalidReminders.Add(reminderID);
    }
    
    return !result.CancelledReminders.Any() ? Results.NotFound() : Results.Json(result);
});
app.Run();