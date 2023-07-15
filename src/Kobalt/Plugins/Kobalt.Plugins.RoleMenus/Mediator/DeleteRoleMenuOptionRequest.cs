using Kobalt.Plugins.RoleMenus.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace Kobalt.Plugins.RoleMenus.Mediator;

public static class DeleteRoleMenuOption
{
    /// <summary>
    /// Deletes a role menu option.
    /// </summary>
    /// <param name="OptionID">The ID of the option to be deleted.</param>
    public record Request(int OptionID) : IRequest<Result>;
    
    internal class Handler(IDbContextFactory<RoleMenuContext> dbFactory) : IRequestHandler<Request, Result>
    {
        public async Task<Result> Handle(Request request, CancellationToken cancellationToken)
        {
            await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

            var option = await db.FindAsync<RoleMenuOptionEntity>(new object[] { request.OptionID }, cancellationToken);

            if (option is null)
            {
                return new NotFoundError($"No role menu option with the ID `{request.OptionID}` exists.");
            }

            db.Remove(option);

            await db.SaveChangesAsync(cancellationToken);

            return Result.FromSuccess();
        }
    }
}