using FuzzySharp;
using Microsoft.Extensions.Caching.Memory;
using NodaTime;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using TimeZoneNames;

namespace Kobalt.Infrastructure.Types;

public class TimezoneAutoCompleteProvider : IAutocompleteProvider
{
    private readonly IMemoryCache _cache;
 
    public string Identity => "TimezoneAutocomplete";
    
    public TimezoneAutoCompleteProvider(IMemoryCache cache, IDateTimeZoneProvider provider)
    {
        _cache = cache;

        if (_cache.TryGetValue("tz_cache", out IDictionary<string, string>? tzCache))
        {
            return;
        }

        tzCache = provider.Ids
                          .Select(id => (id, TZNames.GetDisplayNameForTimeZone(id, "en")!))
                          .Where(kvp => (string?)kvp.Item2 is not null)
                          .DistinctBy(tuple => tuple.Item2)
                          .ToDictionary(kvp => kvp.id, kvp => kvp.Item2);
        
        _cache.Set("tz_cache", tzCache);
    }

    public ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync
    (
        IReadOnlyList<IApplicationCommandInteractionDataOption> options,
        string userInput,
        CancellationToken ct = default
    )
    {
        var tzCache = _cache.Get<IDictionary<string, string>>("tz_cache")!;
        var suggestions = tzCache.Where(tz => Fuzz.PartialTokenSetRatio(tz.Value, userInput) > 80)
            .Select(tz => new ApplicationCommandOptionChoice(tz.Value, tz.Key))
            .Take(25)
            .ToArray();

        return ValueTask.FromResult<IReadOnlyList<IApplicationCommandOptionChoice>>(suggestions);
    }
}
