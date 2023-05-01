using System.Text.RegularExpressions;
using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Results;

namespace Kobalt.Infrastructure.Parsers;

public partial class MicroTimeSpanParser : AbstractTypeParser<TimeSpan>
{
    [GeneratedRegex("(?<Quantity>[0-9]{1,4}(?<Value>y|mo|w|d|h|m)){1,3}")]
    private partial Regex TimeMatch();

    public override ValueTask<Result<TimeSpan>> TryParseAsync(string token, CancellationToken ct = default)
    {
        var match = TimeMatch().Match(token);

        if (!match.Success)
        {
            return ValueTask.FromResult(Result<TimeSpan>.FromError(new ParsingError<TimeSpan>("Invalid time format.")));
        }

        var quantity = int.Parse(match.Groups["Quantity"].Value[..^1]);
        var value = match.Groups["Value"].Value[^1] switch
        {
            'y' => TimeSpan.FromDays(365 * quantity),
            'o' => TimeSpan.FromDays(30 * quantity),
            'w' => TimeSpan.FromDays(7 * quantity),
            'd' => TimeSpan.FromDays(quantity),
            'h' => TimeSpan.FromHours(quantity),
            'm' => TimeSpan.FromMinutes(quantity),
            _ => throw new ArgumentOutOfRangeException()
        };

        return ValueTask.FromResult<Result<TimeSpan>>(value);
    }
}
