using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Humanizer;
using Kobalt.Shared.Services;
using Microsoft.Extensions.Logging;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Results;
using Remora.Rest.Core;
using Remora.Rest.Results;
using Remora.Results;
using StackExchange.Redis;

namespace Kobalt.Bot.Services;

public class MessagePurgeService(IConnectionMultiplexer redis, IDiscordRestChannelAPI channels, IChannelLoggerService logs, ILogger<MessagePurgeService> logger)
{
    /// <summary>
    /// Represents the format that hash keys for member messages should be stored in.
    /// </summary>
    public const string HashKeyFormat = "guilds-{0}-members-{1}-messages";

    // Two weeks, minus a minute to account for any tomfoolery on Discord's side, as well as processing delays
    private static readonly TimeSpan TwoWeeksAgo = TimeSpan.FromMinutes(-14 * 24 * 60 - 1);

    /// <summary>
    /// Purges a given user's messages in a given guild, across all channels. 
    /// </summary>
    /// <param name="guildID">The ID of the guild Kobalt should scan.</param>
    /// <param name="userID">The ID of the user who's messages will be deleted.</param>
    /// <param name="amount">How many messages to search.</param>
    /// <param name="reason">The reason the messages are being deleted.</param>
    /// <returns>The actual messages found.
    /// </returns>
    public async Task<Result<int>> PurgeByUserAsync(Snowflake guildID, Snowflake userID, int amount, string reason = "")
    {
        var db = redis.GetDatabase();
        var key = HashKeyFormat.FormatWith(guildID.ToString(), userID.ToString());
        var pairs = await db.HashGetAllAsync(key);

        var deleted = 0;
        var twoWeeksAgo = DateTimeOffset.Now + TwoWeeksAgo;

        var bulkDeletions = pairs
                            .Reverse()
                            .Take(amount)
                            .GroupBy(p => p.Value)
                            .Where
                            (
                              kvp => 
                                  kvp.Count() > 1 && 
                                  kvp.All(hash => DiscordSnowflake.New((ulong)hash.Name).Timestamp > twoWeeksAgo)
                            )
                            .ToArray();
        
        var cyclicDeletions = pairs.Except(bulkDeletions.SelectMany(kvp => kvp)).Reverse().ToArray();

        await db.HashDeleteAsync(key, bulkDeletions.SelectMany(bd => bd).Concat(cyclicDeletions).Select(c => c.Name).Take(amount).ToArray());

        foreach (var bulkDeletion in bulkDeletions)
        {
            var result = await channels.BulkDeleteMessagesAsync
            (
              DiscordSnowflake.New((ulong)bulkDeletion.Key),
              bulkDeletion.Select(hv => DiscordSnowflake.New((ulong)hv.Name)).ToArray(),
              reason
            );

            if (result.IsSuccess)
            {
                deleted += bulkDeletion.Count();
            }

            if (deleted >= amount)
            {
                return deleted;
            }
        }

        foreach (var deletion in cyclicDeletions)
        {
            var result = await channels.DeleteMessageAsync
            (
              DiscordSnowflake.New((ulong)deletion.Value),
              DiscordSnowflake.New((ulong)deletion.Name)
            );

            if (result.IsSuccess)
            {
                deleted++;
            }

            if (deleted >= amount)
            {
                break;
            }
        }

        return deleted;
    }

    /// <summary>
    /// Purges a user's messages in a given channel.
    /// </summary>
    /// <param name="channelID">The ID of the channel to purge messages from.</param>
    /// <param name="amount">The amount of messages to search through.</param>
    /// <param name="regex">The regex to filter by.</param>
    /// <param name="reason">The reason the messages are being purged.</param>
    /// <returns>The count of deleted messages. </returns>
    public async Task<Result<int>> PurgeByRegexAsync(Snowflake channelID, int amount, string regex, string reason = "")
    {
        // Validate the regex, first and foremost.
        Regex filter;

        try
        {
            filter = new Regex(regex);
        }
        catch (RegexParseException rpe)
        {
            return new InvalidOperationError($"Your regex is invalid. \n{rpe.Message.Replace('\'', '`')}");
        }

        var getMessages = await channels.GetChannelMessagesAsync(channelID);

        if (!getMessages.IsDefined(out var messages))
        {
            return new PermissionDeniedError
            (
                "There was an error getting messages from the channel—if no channel was specified, " +
                "this is a bug in Kobalt, and should be reported."
            );
        }

        messages = messages.Where(m => filter.IsMatch(m.Content)).Take(amount).ToArray();

        var twoWeeksAgo = DateTimeOffset.UtcNow + TwoWeeksAgo;
        var safeForBulkDeletion = messages.Where(m => m.Timestamp > twoWeeksAgo).ToArray();
        var unsafeForBulk = messages.Except(safeForBulkDeletion);

        var deleted = 0;

        if (safeForBulkDeletion.Any())
        {
            var res = await channels.BulkDeleteMessagesAsync(channelID, safeForBulkDeletion.Select(m => m.ID).ToArray(), reason!);

            if (!res.IsSuccess)
            {
                var cast = (RestResultError<RestError>)res.Error;
                return new InvalidOperationError
                (
                    "Something went wrong when trying to delete messages. This could be a bug in Kobalt—please file a bug report.\n" +
                    $"{cast.Error.Code}: {cast.Error.Message}"
                );
            }

            deleted += safeForBulkDeletion.Length;

            if (deleted >= amount)
            {
                return deleted;
            }
        }

        foreach (var message in unsafeForBulk)
        {
            var res = await channels.DeleteMessageAsync(channelID, message.ID, reason);

            if (res.IsSuccess)
            {
                deleted++;
            }

            if (deleted >= amount)
            {
                break;
            }
        }

        return deleted;
    }

    /// <summary>
    /// Purges messages from a channel indiscriminately.
    /// </summary>
    /// <param name="channelID">The ID of the channel to purge messages from.</param>
    /// <param name="around">The message to delete around (50 above and below).</param>
    /// <param name="before">Purge messages before this message.</param>
    /// <param name="after">Purge messages after this message.</param>
    /// <param name="amount">How many messages to purge.</param>
    /// <param name="reason">The reason the messages are being purged.</param>
    /// <returns>A result with the count of deleted message.</returns>
    /// <remarks>If <paramref name="around"/> is specified, <paramref name="amount"/> is limited to 100, and the service does not attempt to fetch further messages.</remarks>
    public async Task<Result<int>> PurgeByChannelAsync(Snowflake channelID, Snowflake? around, Snowflake? before, Snowflake? after, int amount, string reason = "")
    {
        var deleted = 0;
        var twoWeeksAgo = DateTimeOffset.UtcNow + TwoWeeksAgo;

        await foreach (var chunkResult in GetMessagesAsync(channelID, around, before, after, amount))
        {
            if (!chunkResult.IsDefined(out var chunk))
            {
                return new PermissionDeniedError
                (
                    "There was an error getting messages from the channel—if no channel was specified, " +
                    "this is a bug in Kobalt, and should be reported."
                );
            }

            var deletable = chunk.Where(m => m.Timestamp > twoWeeksAgo).Select(d => d.ID).ToArray();

            var deletionResult = await channels.BulkDeleteMessagesAsync(channelID, deletable, reason);

            if (deletionResult.IsSuccess)
            {
                deleted += deletable.Length;
                continue;
            }

            var cast = (RestResultError<RestError>)deletionResult.Error;
            return new InvalidOperationError
            (
                "Something went wrong when trying to delete messages. This could be a bug in Kobalt—please file a bug report.\n" +
                $"{cast.Error.Code}: {cast.Error.Message}"
            );
        }

        return deleted;
    }

    private async IAsyncEnumerable<Result<IReadOnlyList<IMessage>>> GetMessagesAsync
    (
        Snowflake channel,
        Snowflake? around,
        Snowflake? before,
        Snowflake? after,
        int amount
    )
    {
        if (around is not null)
        {
            amount = Math.Min(100, amount);
        }

        if (amount <= 100)
        {
            (Optional<Snowflake> around, Optional<Snowflake> before, Optional<Snowflake> after) fetch =
            (around is null, before is null, after is null) switch
            {
                (false, _, _) => (around.AsOptional(), default, default),
                (_, false, _) => (default, before.AsOptional(), default),
                (_, _, false) => (default, default, after.AsOptional()),
                _ => throw new UnreachableException()
            };

            yield return await channels.GetChannelMessagesAsync(channel, fetch.around, fetch.before, fetch.after, amount);
            yield break;
        }
        
        var head = before ?? after ?? default(Snowflake);
        var fetched = 0;

        while (fetched < amount)
        {
            var (fetchBefore, fetchAfter) = (before is null, after is null) switch
            {
                (false, true) => (default(Optional<Snowflake>), head),
                (true, false) => (head, default),
                (true, true)  => (head, default),
                (false, false) => throw new UnreachableException()
            };
            
            var chunk = await channels.GetChannelMessagesAsync(channel, default, fetchBefore, fetchAfter);

            if (!chunk.IsSuccess)
            {
                yield return chunk;
                yield break;
            }

            if (chunk.Entity.Count > amount - fetched)
            {
                yield return chunk.Entity.Take(amount - fetched).ToArray();
                yield break;
            }

            if (chunk.Entity.Count < 100)
            {
                yield return chunk;
                yield break;
            }

            fetched += chunk.Entity.Count;
        }
        
        
    }
}