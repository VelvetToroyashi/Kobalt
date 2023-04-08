using Kobalt.Infrastructure.DTOs.Reminders;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.ReminderService.Data.Mediator;

/// <summary>
/// Represents a request to get all reminders for a given user.
/// </summary>
public static class GetRemindersForUser
{
    public record Request(ulong UserID) : IRequest<IEnumerable<ReminderDTO>>;
    
    internal class Handler : IRequestHandler<Request, IEnumerable<ReminderDTO>>
    {
        private readonly IDbContextFactory<ReminderContext> _context;

        public Handler(IDbContextFactory<ReminderContext> context)
        {
            _context = context;
        }

        public async ValueTask<IEnumerable<ReminderDTO>> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await _context.CreateDbContextAsync();
            
            var reminders = await context.Reminders
                .Where(r => r.AuthorID == request.UserID)
                .ToListAsync(cancellationToken);
            
            return reminders.Cast<ReminderDTO>();
        }
    }
}
