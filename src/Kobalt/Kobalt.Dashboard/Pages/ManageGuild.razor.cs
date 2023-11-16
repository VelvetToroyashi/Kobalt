using Kobalt.Dashboard.Services;
using Microsoft.AspNetCore.Components;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Rest.Core;

namespace Kobalt.Dashboard.Pages;

public partial class ManageGuild
{
    [Parameter]
    [SupplyParameterFromQuery(Name = "state")]
    public long GuildID { get; set; }

    [Inject]
    public DashboardRestClient Discord { get; set; } = null!;
    
    private IGuild? _guild;
    private IReadOnlyList<IChannel>? _channels;
    private bool _requestFailed;

    protected async override Task OnAfterRenderAsync(bool firstRender)
    {
        if (_requestFailed)
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
            }
            else
            {
                _requestFailed = true;
            }
        }

        StateHasChanged();
    }
}