using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Kobalt.Infrastructure.DTOs.Reminders;
using Kobalt.ReminderService.API.Services;
using Kobalt.ReminderService.Data;
using Kobalt.ReminderService.Data.Extensions;
using Kobalt.ReminderService.Data.Mediator;
using Kobalt.Shared.Extensions;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API;
using Remora.Rest.Json;
using Remora.Rest.Json.Policies;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>();

builder.Services.AddMediator();
builder.Services.AddReminderDbContext(builder.Configuration);

builder.Services.AddSingleton<ReminderService>();
builder.Services.AddHostedService(s => s.GetRequiredService<ReminderService>());

//TODO: Extension method?
var configure = (JsonSerializerOptions options) =>
{
    options.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    options.PropertyNamingPolicy = new SnakeCaseNamingPolicy();
    options.Converters.Insert(0, new SnowflakeConverter(Constants.DiscordEpoch));
};

builder.Services
       .ConfigureHttpJsonOptions(opt => configure(opt.SerializerOptions))
       .Configure<JsonSerializerOptions>(configure);

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
                
                await Task.Delay(100, cts.Token);
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

    return userReminders;
});

// Create a reminder
app.MapPost("/api/reminders/{userID}", async (ulong userID, [FromBody] ReminderCreatePayload reminder, ReminderService reminders) =>
            await reminders.CreateReminderAsync
            (
                userID,
                reminder.ChannelID,
                reminder.GuildID,
                reminder.ReminderContent,
                reminder.Expiration,
                reminder.ReplyMessageID
            ));

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
    
    return !result.CancelledReminders.Any() ? Results.NotFound() : Results.Ok(result);
});

await app.Services.GetRequiredService<IDbContextFactory<ReminderContext>>().CreateDbContext().Database.MigrateAsync();

await app.RunAsync();