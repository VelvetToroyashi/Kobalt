using Kobalt.Infractions.Shared;
using Remora.Results;

namespace Kobalt.Infractions.Infrastructure.Mediator.Errors;

public record RuleAlreadyExistsError(int MatchCount, InfractionType Type)
: ResultError($"A rule already matches {Type} with {MatchCount} matches.");
