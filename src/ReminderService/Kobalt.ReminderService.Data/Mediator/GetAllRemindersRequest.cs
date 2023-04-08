using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.ReminderService.Data.Mediator;

public static class GetAllReminders
{
    public record Request : IRequest<IEnumerable<ReminderDTO>>;
    
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
            
            var entities = await context.Reminders.ToListAsync(cancellationToken);
            return entities.Cast<ReminderDTO>();
        }
    }
    
}
