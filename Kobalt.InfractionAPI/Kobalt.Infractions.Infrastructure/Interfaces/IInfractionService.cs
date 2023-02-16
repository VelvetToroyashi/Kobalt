using Kobalt.Infractions.Infrastructure.Mediator.DTOs;

namespace Kobalt.Infractions.Infrastructure.Interfaces;

public interface IInfractionService
{
    void HandleInfractionUpdate(InfractionDTO infraction);
}
