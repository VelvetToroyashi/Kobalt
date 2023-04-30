using Kobalt.Data.DTOs;
using Kobalt.Data.Mediator;
using Kobalt.Shared.Services;
using Kobalt.Shared.Types;
using MediatR;
using Remora.Discord.API;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Core.Services.Discord;

public class ChannelLoggerService : IChannelLoggerService
{
    private const string WebhookName = "Kobalt";
    
    private sealed record LogChannelContent(LogChannelDTO ChannelContext, Optional<string> Content, Optional<IReadOnlyList<Embed>> Embeds);

    private readonly IUser _self;
    private readonly IMediator _mediator;
    private readonly HttpClient _httpClient;
    private readonly IDiscordRestChannelAPI _channels;
    private readonly IDiscordRestWebhookAPI _webhooks;

    public ChannelLoggerService
    (
        IUser self,
        IMediator mediator,
        IHttpClientFactory factory,
        IDiscordRestChannelAPI channels, 
        IDiscordRestWebhookAPI webhooks
    )
    {
        _self = self;
        _mediator = mediator;
        _httpClient = factory.CreateClient("Kobalt");
        _channels = channels;
        _webhooks = webhooks;
    }

    public async ValueTask<Result> LogAsync
    (
        Snowflake guildID,
        LogChannelType type,
        Optional<string> content = default,
        Optional<IReadOnlyList<Embed>> embeds = default
    )
    {
        if (!content.HasValue && !embeds.HasValue)
        {
            return new InvalidOperationError("Content or embeds must be provided.");
        }
        
        if (content.OrDefault()?.Length > 2000)
        {
            return new InvalidOperationError("Content must be less than 2000 characters.");
        }
        
        var channels = await _mediator.Send(new GetLoggingChannels.Request(guildID, type));
        
        foreach (var channel in channels)
        {
            var channelContent = new LogChannelContent(channel, content, embeds);
            
            await SendLogAsync(channelContent, CancellationToken.None);
        }

        return Result.FromSuccess();
    }

    private async Task SendLogAsync(LogChannelContent content, CancellationToken ct)
    {
        var getWebhookResult = await GetWebhookAsync(content.ChannelContext, ct);

        if (getWebhookResult.IsSuccess)
        {
            var executeWebhookResult = await _webhooks.ExecuteWebhookAsync
            (
                getWebhookResult.Entity.ID,
                getWebhookResult.Entity.Token.Value,
                content: content.Content,
                embeds: content.Embeds.Map(embeds => (IReadOnlyList<IEmbed>)embeds),
                allowedMentions: new AllowedMentions(Parse: Array.Empty<MentionType>()),
                ct: ct
            );

            if (executeWebhookResult.IsSuccess)
            {
                return;
            }
        }
        
        var sendMessageResult = await _channels.CreateMessageAsync
        (
            content.ChannelContext.ChannelID,
            content.Content,
            embeds: content.Embeds.Map(embeds => (IReadOnlyList<IEmbed>)embeds),
            allowedMentions: new AllowedMentions(Parse: Array.Empty<MentionType>()),
            ct: ct
        );

        if (!sendMessageResult.IsSuccess)
        {
            // TODO: Log
        }
    }
    
    private async Task<Result<IWebhook>> GetWebhookAsync(LogChannelDTO channel, CancellationToken ct)
    {
        if (channel.WebhookID.HasValue)
        {
            var existingWebhook = await _webhooks.GetWebhookAsync(channel.WebhookID.Value, ct);
            if (!existingWebhook.IsSuccess)
            {
                return existingWebhook;
            }

            return existingWebhook;
        }
        
        var avatarStream = await _httpClient.GetStreamAsync(CDN.GetUserAvatarUrl(_self).Entity, CancellationToken.None);

        var webhook = await _webhooks.CreateWebhookAsync
        (
            channel.ChannelID,
            WebhookName,
            avatarStream,
            ct: ct
        );

        if (!webhook.IsSuccess)
        {
            return webhook;
        }

        var webhookID = webhook.Entity.ID;
        var webhookToken = webhook.Entity.Token;

        var update = await _mediator.Send(new UpdateLoggingChannel.Request(channel.ChannelID, webhookID, webhookToken.Value), ct);
        
        if (!update.IsSuccess)
        {
            return Result<IWebhook>.FromError(update);
        }

        return webhook;
    }
}
