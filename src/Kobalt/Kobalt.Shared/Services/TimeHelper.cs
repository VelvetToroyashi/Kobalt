using Kobalt.Shared.Extensions;
using NodaTime;
using NodaTime.TimeZones;
using OneOf;
using Recognizers.Text.DateTime.Wrapper;
using Recognizers.Text.DateTime.Wrapper.Models.BclDateTime;
using Remora.Commands.Results;
using Remora.Results;

namespace Kobalt.Shared.Services;

/// <summary>
/// Represents a time extraction.
/// </summary>
/// <param name="Position">The position in the text the time was extracted from.</param>
/// <param name="Time">The time, relative to the given offset.</param>
public record ExtractedTime(Range Position, OneOf<DateTimeOffset, (DateTimeOffset Start, DateTimeOffset End)> Time);

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



    /// <summary>
    /// Extracts times from a given string, relative to a given timezone.
    /// </summary>
    /// <param name="input">The input text to extract texts from.</param>
    /// <param name="timezoneOffset">The timezone to serve as a reference for any relative mentions of time.</param>
    /// <returns>A list of extracted times, including their indices in the text.</returns>
    public static Result<IReadOnlyList<ExtractedTime>> ExtractTimes(string input, Offset timezoneOffset)
    {
        var offset = timezoneOffset.ToTimeSpan();
        var refTime = DateTime.UtcNow + offset;

        var times = DateTimeV2Recognizer.RecognizeDateTimes(input, refTime: refTime);

        if (!times.Any())
        {
            return new InvalidOperationError("No times found.");
        }

        var result = new List<ExtractedTime>();

        // DateTimeOffset.UtcNow + (res.Value - refTime)
        foreach (var time in times)
        {
            var recognizedTimeString = time.Start..(time.End + 1);
            OneOf<DateTimeOffset?, (DateTimeOffset, DateTimeOffset)> recognizedTime = time.Resolution.Values.First() switch
            {
                DateTimeV2Date date => (DateTimeOffset)(date.Value - offset),
                DateTimeV2DateTime dateTime => (DateTimeOffset)dateTime.Value - offset,
                DateTimeV2DateRange dateRange => (dateRange.Value.Start - offset, dateRange.Value.End - offset),
                DateTimeV2TimeRange dateTimeRange => (dateTimeRange.Value.Start - offset, dateTimeRange.Value.End - offset),
                _ => null
            };

            if (recognizedTime.Value is null)
            {
                continue;
            }

            result.Add(new ExtractedTime(recognizedTimeString, recognizedTime.MapT0(t => t.Value)));
        }

        return result;
    }
}
