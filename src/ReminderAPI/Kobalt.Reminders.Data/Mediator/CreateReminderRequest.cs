using Kobalt.Reminders.Data.Entities;
using Kobalt.Shared.DTOs.Reminders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Reminders.Data.Mediator;

public static class CreateReminder
{
    /// <summary>
    /// Represents a request to create a reminder.
    /// </summary>
    /// <param name="AuthorID">The ID of the user creating the reminder.</param>
    /// <param name="ChannelID">The ID of the channel the reminder was created in.</param>
    /// <param name="GuildID">The ID of the guild the reminder was created in, if any.</param>
    /// <param name="ReminderContent">The content of the reminder.</param>
    /// <param name="Expiration">When the reminder expires.</param>
    /// <param name="ReplyMessageID">The ID of the message the reminder is replying to, if any.</param>
    public record Request
    (
        ulong AuthorID,
        ulong ChannelID,
        ulong? GuildID,
        string ReminderContent,
        DateTimeOffset Expiration,
        ulong? ReplyMessageID
    ) : IRequest<ReminderDTO>;

    internal class Handler : IRequestHandler<Request, ReminderDTO>
    {
        private readonly IDbContextFactory<ReminderContext> _context;

        public Handler(IDbContextFactory<ReminderContext> context) => _context = context;

        public async Task<ReminderDTO> Handle(Request request, CancellationToken cancellationToken)
        {
            var entity = new ReminderEntity
            {
                AuthorID = request.AuthorID,
                ChannelID = request.ChannelID,
                GuildID = request.GuildID,
                ReminderContent = request.ReminderContent,
                Creation = DateTimeOffset.UtcNow,
                Expiration = request.Expiration,
                ReplyMessageID = request.ReplyMessageID
            };

            await using var context = await _context.CreateDbContextAsync();

            await context.Reminders.AddAsync(entity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            return (ReminderDTO)entity;
        }
    }
}
