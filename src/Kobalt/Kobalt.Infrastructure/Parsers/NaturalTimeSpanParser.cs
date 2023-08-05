using Kobalt.Shared.MediatR.Users;
using MediatR;
using Recognizers.Text.DateTime.Wrapper;
using Recognizers.Text.DateTime.Wrapper.Models.BclDateTime;
using Remora.Commands.Parsers;
using Remora.Commands.Results;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Results;

namespace Kobalt.Infrastructure.Parsers;

public class NaturalTimeSpanParser : AbstractTypeParser<TimeSpan>
{
    private readonly IMediator _mediator;
    private readonly IOperationContext _context;

    public NaturalTimeSpanParser(IMediator mediator, IOperationContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    public override async ValueTask<Result<TimeSpan>> TryParseAsync(string token, CancellationToken ct = default)
    {
        if (!_context.TryGetUserID(out var userID))
        {
            return new InvalidOperationError("Could not get user ID.");
        }

        var refTime = DateTime.UtcNow;
        var userResult = await _mediator.Send(new GetUserRequest(userID));

        if (userResult.IsSuccess)
        {
            refTime += userResult.Entity.Timezone.GetValueOrDefault().ToTimeSpan();
        }

        var extractionResult = DateTimeV2Recognizer.RecognizeDateTimes(token, refTime: refTime);

        if (extractionResult.FirstOrDefault() is not {} result || result.Resolution.Values.FirstOrDefault() is not {} value)
        {
            return new ParsingError<TimeSpan>("Invalid time format.");
        }

        var ts = value switch
        {
            DateTimeV2Date date         => date.Value     - refTime,
            DateTimeV2DateTime dateTime => dateTime.Value - refTime,
            DateTimeV2Duration duration => duration.Value,
            _                           => throw new ArgumentOutOfRangeException()
        };

        return ts;
    }
}
