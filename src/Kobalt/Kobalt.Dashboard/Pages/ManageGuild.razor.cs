using Kobalt.Bot.Data.Entities;
using Kobalt.Bot.Data.MediatR.Guilds;
using Kobalt.Dashboard.Services;
using MediatR;
using Microsoft.AspNetCore.Components;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

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
    public required DashboardRestClient Discord { get; set; }

    [Inject]
    public required IMediator Mediator { get; set; }

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
                
                //var kobaltGuildResult = await Mediator.Send(new GetGuild. ((ulong)GuildID)));
                
                _guildState = GuildState.Ready;
            }
            else
            {
                _guildState = GuildState.Unavailable;
            }
        }

        StateHasChanged();
    }
}