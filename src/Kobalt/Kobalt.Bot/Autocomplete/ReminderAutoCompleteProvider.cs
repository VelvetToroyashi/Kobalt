﻿using FuzzySharp;
using Humanizer;
using Humanizer.Localisation;
using Kobalt.Bot.Services;
using Kobalt.Shared.DTOs.Reminders;
using Microsoft.Extensions.Caching.Memory;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;

namespace Kobalt.Bot.Autocomplete;

/// <summary>
/// Provides autocompletions for the reminder command.
/// </summary>
public class ReminderAutoCompleteProvider : IAutocompleteProvider
{
    private readonly IMemoryCache _cache;
    private readonly ReminderAPIService _reminders;
    private readonly IInteractionContext _context;

    public ReminderAutoCompleteProvider(IMemoryCache cache, ReminderAPIService reminders, IInteractionContext context)
    {
        _cache = cache;
        _reminders = reminders;
        _context = context;
    }

    public string Identity => "Plugins:Reminders";

    public async ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync
    (
        IReadOnlyList<IApplicationCommandInteractionDataOption> options,
        string userInput,
        CancellationToken ct = default
    )
    {
        if (!_context.TryGetUserID(out var userId))
        {
            return Array.Empty<IApplicationCommandOptionChoice>();
        }

        if (!_cache.TryGetValue($"{userId}_reminders", out IReadOnlyList<ReminderDTO>? reminders))
        {
            var reminderResult = await _reminders.GetRemindersAsync(userId);

            if (!reminderResult.IsSuccess)
            {
                return Array.Empty<IApplicationCommandOptionChoice>();
            }

            reminders = reminderResult.Entity;
            _cache.Set($"{userId}_reminders", reminders, TimeSpan.FromMinutes(5));
        }

        if (string.IsNullOrWhiteSpace(userInput))
        {
            return reminders!
                   .OrderBy(r => r.Expiration)
                   .Take(20)
                   .Select(GetReminderContent)
                   .Select(s => new ApplicationCommandOptionChoice(s.Item2, s.Item1.ToString()))
                   .ToArray();
        }
        var suggestions = reminders!
                          .Select(r => (r, Fuzz.PartialRatio(r.ReminderContent, userInput)))
                          .Where(rt => rt.Item2 > 60)
                          .OrderByDescending(rt => rt.Item2)
                          .ThenByDescending(rt => rt.r.Expiration)
                          .Select(rt => rt.r)
                          .Take(25)
                          .Select(GetReminderContent)
                          .Select(s => new ApplicationCommandOptionChoice(s.Item2, s.Item1.ToString()))
                          .ToArray();

        return suggestions;

        static (int, string) GetReminderContent(ReminderDTO reminder)
        {
            return (reminder.Id, $"({reminder.Id}) | in {(reminder.Expiration - DateTimeOffset.UtcNow).Humanize(minUnit: TimeUnit.Second)}: {reminder.ReminderContent}".Truncate(65, "[...]"));
        }
    }
}
