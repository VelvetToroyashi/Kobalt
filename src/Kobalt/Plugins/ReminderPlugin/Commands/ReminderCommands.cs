using System.ComponentModel;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace ReminderPlugin.Commands;

[Group("reminder")]
public class ReminderCommands : CommandGroup
{
    private readonly IInteractionContext _context;
    private readonly IDiscordRestInteractionAPI _interactions;

    public ReminderCommands(IInteractionContext context, IDiscordRestInteractionAPI interactions)
    {
        _context = context;
        _interactions = interactions;
    }

    [Command("set")]
    [Description("I'll remind you of something. Just tell me when.")]
    public async Task<Result> RemindAsync(string when, string of)
    {
        return default;
    }
    
    [Command("list")]
    [Description("I'll list all your reminders.")]
    public async Task<Result> ListAsync()
    {
        return default;
    }
    
    [Command("delete")]
    [Description("I'll delete a reminder.")]
    public async Task<Result> DeleteAsync([DiscordTypeHint(TypeHint.String)] string id)
    {
        return default;
    }
}
