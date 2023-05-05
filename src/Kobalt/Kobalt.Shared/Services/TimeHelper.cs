using Kobalt.Shared.Extensions;
using NodaTime;
using NodaTime.TimeZones;
using Remora.Commands.Results;
using Remora.Results;

namespace Kobalt.Shared.Services;

/// <summary>
/// A helper class for time-related operations.
/// </summary>
public static class TimeHelper
{
    /// <summary>
    /// Attempts to parse a string into a <see cref="DateTimeZone"/>.
    /// </summary>
    /// <param name="input">The input string.</param>
    /// <param name="provider">A provider of timezones.</param>
    /// <returns>A result of the extracted timezone, otherwise an error.</returns>
    public static Result<DateTimeZone> GetDateTimeZoneFromString(string input, IDateTimeZoneProvider provider)
    {
        var bclResult = ResultExtensions.TryCatch(() => TimeZoneInfo.FindSystemTimeZoneById(input));

        if (bclResult.IsDefined(out var bcl))
        {
            return BclDateTimeZone.FromTimeZoneInfo(bcl);
        }

        var offsetName = provider.GetZoneOrNull(input);

        if (offsetName is null)
        {
            return new ParsingError<DateTimeZone>("Invalid timezone.");
        }

        return offsetName;
    }
}
