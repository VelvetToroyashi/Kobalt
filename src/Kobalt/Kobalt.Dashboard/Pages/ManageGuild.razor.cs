using System.Text.Json;
using Kobalt.Dashboard.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;
using Remora.Results;
using Guild = Kobalt.Bot.Data.Entities.Guild;

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
    private Guild? _kobaltGuild;
    private IReadOnlyList<IChannel>? _channels;

    private GuildState _guildState = GuildState.Loading;
    
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
                _channels = (await Discord.GetGuildChannelsAsync(new Snowflake((ulong)GuildID))).Entity;

                var kobaltGuildResult = await GetGuildAsync();
                
                if (!kobaltGuildResult.IsSuccess)
                {
                    _guildState = GuildState.Unavailable;
                    StateHasChanged();
                    return;
                }
                
                _kobaltGuild = kobaltGuildResult.Entity;
                _guildState = GuildState.Ready;
            }
            else
            {
                _guildState = GuildState.Unavailable;
            }
        }

        StateHasChanged();
    }

    private async Task<Result<Guild>> GetGuildAsync()
    {
        using var client = Http.CreateClient("Kobalt");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/guilds/{GuildID}");
        
        using var response = await client.SendAsync(request);
        
        if (response.IsSuccessStatusCode)
        {
            var jsonSerializer = JsonOptions.Get("Discord");
            return Result<Guild>.FromSuccess(await response.Content.ReadFromJsonAsync<Guild>(jsonSerializer));
        }
        else
        {
            return Result<Guild>.FromError(new NotFoundError("Failed to retrieve guild."));
        }
    }
}