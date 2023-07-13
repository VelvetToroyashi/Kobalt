using Humanizer;
using Kobalt.Plugins.Core.Services;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Caching;
using Remora.Discord.Caching.Services;
using Remora.Discord.Extensions.Attributes;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using StackExchange.Redis;

namespace Kobalt.Core.Responders;

[Responder(ResponderGroup.Early)]
public class MessageHashCacherResponder(IConnectionMultiplexer redis, CacheService cache) : IResponder<IMessageCreate>, IResponder<IMessageDelete>
{
    public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
    {
        if (!gatewayEvent.GuildID.IsDefined(out var guildID))
        {
            return Result.FromSuccess();
        }
        
        var db = redis.GetDatabase();
        var key = MessagePurgeService.HashKeyFormat.FormatWith(guildID, gatewayEvent.Author.ID);
        await db.HashSetAsync
        (
            new RedisKey(key),
            new[]
            {
                new HashEntry(gatewayEvent.ID.ToString(), gatewayEvent.ChannelID.ToString())
            }
        );

        return Result.FromSuccess();
    }

    public async Task<Result> RespondAsync(IMessageDelete gatewayEvent, CancellationToken ct = default)
    {
        // This is somewhat fragile code; the downfall of storing the user's ID in the key
        // is that because Discord does not return the message that was deleted, we need to 
        // infer based on what's in cache, and as we all know: https://shouldiblamecaching.com
        if (!gatewayEvent.GuildID.IsDefined(out var guildID))
        {
            return Result.FromSuccess();
        }
        
        var originalMessage = await cache.TryGetValueAsync<IMessageCreate>(new KeyHelpers.MessageCacheKey(gatewayEvent.ChannelID, gatewayEvent.ID), ct);

        if (originalMessage.IsSuccess)
        {
            // If it doesn't exist in cache, we didn't have this message to begin with.
            return Result.FromSuccess();
        }
        
        var db  = redis.GetDatabase();
        var key = MessagePurgeService.HashKeyFormat.FormatWith(guildID, originalMessage.Entity.Author.ID);

        var deleteResult = await db.HashDeleteAsync(new RedisKey(key), new RedisValue(gatewayEvent.ID.ToString()));

        if (deleteResult)
        {
            return Result.FromSuccess();
        }

        return new InvalidOperationError($"Expected key ({key}) to contain hash value, but it did not.");
    }
}