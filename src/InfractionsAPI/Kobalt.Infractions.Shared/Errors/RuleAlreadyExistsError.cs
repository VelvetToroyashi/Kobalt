using Remora.Results;

namespace Kobalt.Infractions.Shared.Errors;

public record RuleAlreadyExistsError(int MatchCount, InfractionType Type)
: ResultError($"A rule already matches {Type} with {MatchCount} matches.");
