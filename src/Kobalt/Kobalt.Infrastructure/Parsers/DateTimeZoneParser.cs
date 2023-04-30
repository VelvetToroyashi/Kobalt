using Kobalt.Shared.Extensions;
using NodaTime;
using NodaTime.TimeZones;
using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Results;

namespace Kobalt.Infrastructure.Parsers;

public class DateTimeZoneParser : AbstractTypeParser<DateTimeZone>
{
    private readonly IDateTimeZoneProvider _dateTimeZoneProvider;

    public DateTimeZoneParser(IDateTimeZoneProvider dateTimeZoneProvider) => _dateTimeZoneProvider = dateTimeZoneProvider;

    public override ValueTask<Result<DateTimeZone>> TryParseAsync(string token, CancellationToken ct = default)
    {
        var tzInfoResult = ResultExtensions.TryCatch(() => TimeZoneInfo.FindSystemTimeZoneById(token));
     
        if (tzInfoResult.IsDefined(out var tzInfo))
        {
            var offset = BclDateTimeZone.FromTimeZoneInfo(tzInfo);
            
            return ValueTask.FromResult<Result<DateTimeZone>>(offset);
        }
        
        var offsetName = _dateTimeZoneProvider.GetZoneOrNull(token);
        
        if (offsetName is null)
        {
            return ValueTask.FromResult<Result<DateTimeZone>>(new ParsingError<DateTimeZone>("Invalid timezone."));
        }

        return ValueTask.FromResult<Result<DateTimeZone>>(offsetName);
    }
}
