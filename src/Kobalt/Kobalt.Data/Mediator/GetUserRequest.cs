using Kobalt.Shared.DTOs.Users;
using Kobalt.Shared.Mediator.Users;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace Kobalt.Data.Mediator;

internal class GetUserHandler : IRequestHandler<GetUserRequest, UserDTO>
{
    private readonly IDbContextFactory<KobaltContext> _contextFactory;
    
    public GetUserHandler(IDbContextFactory<KobaltContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async ValueTask<UserDTO> Handle(GetUserRequest request, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        
        var user = await context.Users.FindAsync(new object?[] { request.ID }, cancellationToken: cancellationToken);
        
        return new UserDTO(user.ID, user.Timezone, user.DisplayTimezone, user.Guilds.Select(g => g.GuildId).ToArray());
    }
}
