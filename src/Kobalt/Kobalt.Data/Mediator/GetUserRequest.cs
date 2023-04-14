using Kobalt.Shared.DTOs.Users;
using Kobalt.Shared.Mediator.Users;
using Mediator;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Remora.Results;

namespace Kobalt.Data.Mediator;

internal class GetUserHandler : IRequestHandler<GetUserRequest, Result<UserDTO>>
{
    private readonly IDateTimeZoneProvider _timeZoneProvider;
    private readonly IDbContextFactory<KobaltContext> _contextFactory;
    
    
    public GetUserHandler(IDateTimeZoneProvider timeZoneProvider, IDbContextFactory<KobaltContext> contextFactory)
    {
        _contextFactory = contextFactory;
        _timeZoneProvider = timeZoneProvider;
    }

    public async ValueTask<Result<UserDTO>> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        var user = await context.Users.FindAsync(new object?[] { request.ID }, cancellationToken: cancellationToken);

        if (user is null)
        {
            return new NotFoundError("User not found.");
        }
        
        Offset? timezone = user.Timezone is null ? null : _timeZoneProvider.GetZoneOrNull(user.Timezone)?.GetUtcOffset(Instant.FromDateTimeOffset(DateTimeOffset.UtcNow));
        
        return new UserDTO(user.ID, timezone, user.DisplayTimezone, user.Guilds.Select(g => g.GuildId).ToArray());
    }
}
