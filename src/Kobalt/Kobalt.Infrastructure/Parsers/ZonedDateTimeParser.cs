using Kobalt.Shared.Mediator.Users;
using MediatR;
using Microsoft.Recognizers.Text;
using NodaTime;
using Recognizers.Text.DateTime.Wrapper;
using Recognizers.Text.DateTime.Wrapper.Models.BclDateTime;
using Recognizers.Text.DateTime.Wrapper.Models.Enums;
using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Results;

namespace Kobalt.Infrastructure.Parsers;

public class ZonedDateTimeParser : AbstractTypeParser<ZonedDateTime>
{
    private static readonly ISet<DateTimeV2Type> _supportedTypes = new HashSet<DateTimeV2Type>
    {
        DateTimeV2Type.Date,
        DateTimeV2Type.DateTime,
    };

    private readonly IMediator _mediator;
    private readonly IOperationContext _context;

    public ZonedDateTimeParser(IMediator mediator, IOperationContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    public override async ValueTask<Result<ZonedDateTime>> TryParseAsync(string token, CancellationToken ct = default)
    {
        if (!_context.TryGetUserID(out var userID))
        {
            return new InvalidOperationError("Could not get user ID.");
        }

        var userResult = await _mediator.Send(new GetUserRequest(userID.Value), ct);

        var offset = userResult.Entity?.Timezone ?? Offset.Zero;
        var extractionResult = ExtractDateTimeOffset(token, offset.ToTimeSpan());

        if (extractionResult.IsSuccess)
        {
            return ZonedDateTime.FromDateTimeOffset(extractionResult.Entity.ToUniversalTime());
        }

        return new ParsingError<ZonedDateTime>("Invalid date.");
    }

    private static Result<DateTimeOffset> ExtractDateTimeOffset(string input, TimeSpan offset)
    {
        var refTime = DateTime.UtcNow + offset;
        var extractionResult = DateTimeV2Recognizer.RecognizeDateTimes(input, Culture.English, refTime, _supportedTypes);

        if (extractionResult.FirstOrDefault() is not {} result || !result.Resolution.Values.Any())
        {
            return Result<DateTimeOffset>.FromError(new NotFoundError());
        }

        var res = result.Resolution
              .Values
              .Select(v => v is DateTimeV2Date dtd ? dtd.Value : v is DateTimeV2DateTime dtt ? dtt.Value : (DateTime?)null)
              .FirstOrDefault
              (
                  v => v > refTime
              );

        if (res is null)
        {
            return Result<DateTimeOffset>.FromError(new NotFoundError());
        }

        return DateTimeOffset.UtcNow + (res.Value - refTime);
    }

}
