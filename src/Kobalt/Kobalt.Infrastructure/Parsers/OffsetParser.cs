﻿using NodaTime;
using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Results;

namespace Kobalt.Infrastructure.Parsers;

public class OffsetParser : AbstractTypeParser<Offset>
{
    private readonly IDateTimeZoneProvider _dateTimeZoneProvider;

    public OffsetParser(IDateTimeZoneProvider dateTimeZoneProvider)
    {
        _dateTimeZoneProvider = dateTimeZoneProvider;
    }

    public override ValueTask<Result<Offset>> TryParseAsync(string token, CancellationToken ct = default)
    {
        var offsetName = _dateTimeZoneProvider.GetZoneOrNull(token);
        
        if (offsetName is null)
        {
            return ValueTask.FromResult<Result<Offset>>(new ParsingError<Offset>("Invalid timezone."));
        }

        return ValueTask.FromResult<Result<Offset>>(offsetName.GetUtcOffset(Instant.FromDateTimeOffset(DateTimeOffset.UtcNow)));
    }
}
