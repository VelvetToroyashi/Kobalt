using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.ReminderService.Data.Mediator;

public class DeleteReminder
{
    /// <summary>
    /// Represents a request to delete a reminder.
    /// </summary>
    /// <param name="Id">The ID of the reminder to delete.</param>
    /// <param name="AuthorID">The user requesting to delete the reminder.</param>
    public record Request(int Id, ulong AuthorID) : IRequest<Result>;
    
    internal class Handler : IRequestHandler<Request, Result>
    {
        private readonly IDbContextFactory<ReminderContext> _context;
        
        public Handler(IDbContextFactory<ReminderContext> context)
        {
            _context = context;
        }
        
        public async ValueTask<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await _context.CreateDbContextAsync();
            
            var entity = await context.Reminders.FindAsync(request.Id);
            
            if (entity is null)
            {
                return new NotFoundError("Reminder not found.");
            }
            
            if (entity.AuthorID != request.AuthorID)
            {
                return new InvalidOperationError("You are not the author of this reminder.");
            }
            
            context.Reminders.Remove(entity);
            await context.SaveChangesAsync(cancellationToken);
            
            return Result.FromSuccess();
        }
    }
    
}
