using System.Collections.Frozen;
using System.Net;
using System.Text;
using System.Text.Json;
using Humanizer;
using Humanizer.Localisation;
using Kobalt.Bot.Data.DTOs;
using Kobalt.Dashboard.Extensions;
using Kobalt.Dashboard.Services;
using Kobalt.Dashboard.Views;
using Kobalt.Infractions.Shared;
using Kobalt.Infractions.Shared.DTOs;
using Kobalt.Shared.Types;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using MudBlazor;
using Recognizers.Text.DateTime.Wrapper;
using Recognizers.Text.DateTime.Wrapper.Models.BclDateTime;
using Recognizers.Text.DateTime.Wrapper.Models.Enums;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using Remora.Results;
using Singulink.Enums;

namespace Kobalt.Dashboard.Pages;

public partial class ManageGuild
{
    private enum GuildState
    {
        Loading,
        Unavailable,
        Ready
    }
    
    [Parameter]
    public long GuildID { get; set; }
    
    [Inject]
    public required IHttpClientFactory Http { get; set; }
    
    [Inject]
    public required IOptionsMonitor<JsonSerializerOptions> JsonOptions { get; set; }
    
    [Inject]
    public required DashboardRestClient Discord { get; set; }
    
    private IGuild? _guild;
    private KobaltGuildView? _kobaltGuild;
    private IReadOnlyDictionary<Snowflake, IChannel>? _channels;
    private Result<IReadOnlyList<InfractionView>>? _infractions;

    private string? _currentSearch;
    private readonly Func<KobaltLoggingConfigView, bool> _searchFilter = (config) => true;
    
    private bool _isBusy;
    private GuildState _guildState = GuildState.Loading;

    private static readonly ISet<DateTimeV2Type> TypeFilter = new[] { DateTimeV2Type.Duration }.ToFrozenSet();

    private readonly Converter<TimeSpan> _timespanConverter = new()
    {
        GetFunc = (s) => DateTimeV2Recognizer.RecognizeDateTimes(s!, "en-us", null, TypeFilter).FirstOrDefault()?.Resolution.Values.FirstOrDefault() is DateTimeV2Duration duration
        ? duration.Value
        : throw new(),
        SetFunc = (s) => s.Humanize(3, minUnit: TimeUnit.Minute)
    };
    
    private void UpdateLogChannel(IEnumerable<LogChannelType> selectedChannelTypes) {}
    
    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (_guildState is GuildState.Unavailable)
        {
            await base.OnAfterRenderAsync(firstRender);
            return;
        }
        
        if (_guild is null)
        {
            var guildsResult = await Discord.GetGuildAsync(new Snowflake((ulong)GuildID));

            if (guildsResult.IsSuccess)
            {
                _guild = guildsResult.Entity;
                var channels = (await Discord.GetGuildChannelsAsync(new Snowflake((ulong)GuildID))).Entity;

                _channels = channels.ToDictionary(c => c.ID, c => c);
                
                
                var kobaltGuildResult = await GetGuildAsync();
                
                if (!kobaltGuildResult.IsSuccess)
                {
                    _guildState = GuildState.Unavailable;
                    StateHasChanged();
                    return;
                }
                
                _kobaltGuild = new KobaltGuildView(kobaltGuildResult.Entity);
                _guildState = GuildState.Ready;
                
                _ = InvokeAsync(GetInfractionsAsync);
            }
            else
            {
                _guildState = GuildState.Unavailable;
            }
        }

        StateHasChanged();
    }
    
    private void UpdateDate(DateTime? date, Action<TimeSpan?> setTime)
    {
        setTime(date - DateTimeOffset.UtcNow);
    }
    
    private void UpdateDate(DateTime? date, Action<TimeSpan> setTime)
    {
        setTime(date - DateTimeOffset.UtcNow ?? TimeSpan.Zero);
    }

    private async Task GetInfractionsAsync()
    {
        using var client = Http.CreateClient("Infractions");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"infractions/guilds/{GuildID}");
        
        using var response = await client.SendAsync(request);
        
        try
        {
            if (!response.IsSuccessStatusCode)
            {
                _infractions = Result<IReadOnlyList<InfractionView>>.FromError(new NotFoundError("Failed to retrieve infractions."));
            }
            else
            {
                if (response.StatusCode is HttpStatusCode.NoContent)
                {
                    _infractions = Result<IReadOnlyList<InfractionView>>.FromSuccess(Array.Empty<InfractionView>());
                    return;
                }
                
                var jsonSerializer = JsonOptions.Get("Discord");
                var content = await response.Content.ReadFromJsonAsync<IReadOnlyList<InfractionDTO>>(jsonSerializer);

                var list = new List<InfractionView>();

                foreach (var infraction in content!.Take(6))
                {
                    var enforcer = await Discord.ResolveUserAsync(infraction.ModeratorID);
                    var target = await Discord.ResolveUserAsync(infraction.UserID);

                    list.Add
                    (
                        new InfractionView
                        (
                            infraction.Type,
                            enforcer.Entity.GetFormattedUsername(),
                            target.Entity.GetFormattedUsername(),
                            infraction.CreatedAt,
                            infraction.Reason.Truncate(40, "[...]"),
                            infraction.ExpiresAt
                        )
                    );
                }

                _infractions = Result<IReadOnlyList<InfractionView>>.FromSuccess(list);
            }
        }
        finally
        {
            StateHasChanged();   
        }
    }
    
    private async Task<Result<KobaltGuildDTO>> GetGuildAsync()
    {
        using var client = Http.CreateClient("Kobalt");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/guilds/{GuildID}");
        
        using var response = await client.SendAsync(request);
        
        if (response.IsSuccessStatusCode)
        {
            var jsonSerializer = JsonOptions.Get("Discord");
            var jsonData = await response.Content.ReadAsByteArrayAsync();

            return Result<KobaltGuildDTO>.FromSuccess(JsonSerializer.Deserialize<KobaltGuildDTO>(jsonData, jsonSerializer)!);
        }
        else
        {
            return Result<KobaltGuildDTO>.FromError(new NotFoundError("Failed to retrieve guild."));
        }
    }

    private record InfractionView
    (
        InfractionType Type,
        string Enforcer,
        string Target,
        DateTimeOffset When,
        string Reason,
        DateTimeOffset? Expiration
    ); 
}

