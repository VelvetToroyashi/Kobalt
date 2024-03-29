﻿using Kobalt.Shared.DTOs.Reminders;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Reminders.Data.Mediator;

/// <summary>
/// Represents a request to get all reminders for a given user.
/// </summary>
public static class GetRemindersForUser
{
    public record Request(ulong UserID) : IRequest<IEnumerable<ReminderDTO>>;

    internal class Handler : IRequestHandler<Request, IEnumerable<ReminderDTO>>
    {
        private readonly IDbContextFactory<ReminderContext> _context;

        public Handler(IDbContextFactory<ReminderContext> context) => _context = context;

        public async Task<IEnumerable<ReminderDTO>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await _context.CreateDbContextAsync();

            var reminders = await context.Reminders
                .Where(r => r.AuthorID == request.UserID)
                .Select(r => (ReminderDTO)r)
                .ToListAsync(cancellationToken);

            return reminders;
        }
    }
}
