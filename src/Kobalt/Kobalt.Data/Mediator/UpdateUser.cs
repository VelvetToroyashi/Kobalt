using Mediator;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Data.Mediator;

public static class UpdateUser
{
    /// <summary>
    /// Requests to update a user.
    /// </summary>
    /// <param name="ID">The ID of the user.</param>
    /// <param name="Timezone">The timezone of the user.</param>
    /// <param name="DisplayTimezone">Whether the user wants to display their timezone.</param>
    public record Request
    (
        Snowflake ID,
        Optional<string> Timezone,
        Optional<bool> DisplayTimezone
    ) : IRequest<Result>;
    
    internal class Handler : IRequestHandler<Request, Result>
    {
        private readonly IDbContextFactory<KobaltContext> _contextFactory;
        
        public Handler(IDbContextFactory<KobaltContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }
        
        public async ValueTask<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            
            var user = await context.Users.FindAsync(new object?[] { request.ID }, cancellationToken: cancellationToken);
            
            if (user is null)
            {
                return new NotFoundError("User not found.");
            }
            
            if (request.Timezone.HasValue)
            {
                user.Timezone = request.Timezone.Value;
            }
            
            if (request.DisplayTimezone.HasValue)
            {
                user.DisplayTimezone = request.DisplayTimezone.Value;
            }
            
            await context.SaveChangesAsync(cancellationToken);
            
            return Result.FromSuccess();
        }
    }
}
