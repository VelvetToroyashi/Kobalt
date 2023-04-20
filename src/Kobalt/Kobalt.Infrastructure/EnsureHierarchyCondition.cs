using Kobalt.Shared.Conditions;
using Remora.Commands.Conditions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

namespace Kobalt.Infrastructure;

public class EnsureHierarchyCondition : ICondition<EnsureHierarchyAttribute>, ICondition<EnsureHierarchyAttribute, IUser>
{
    private readonly IUser _self;
    private readonly IInteractionContext _context;
    private readonly IDiscordRestUserAPI _users;
    private readonly IDiscordRestGuildAPI _guilds;

    public EnsureHierarchyCondition(IUser self, IInteractionContext context, IDiscordRestUserAPI users, IDiscordRestGuildAPI guilds)
    {
        _self = self;
        _context = context;
        _users = users;
        _guilds = guilds;
    }

    public async ValueTask<Result> CheckAsync(EnsureHierarchyAttribute attribute, CancellationToken ct = default)
    {
        var expectedLevel = attribute.Level;
        var target = attribute.Target;

        if (target is not HierarchyTarget.Invoker)
        {
            return new InvalidOperationError($"{nameof(HierarchyTarget.Self)} is not supported for a command-level attribute.");
        }
        
        var targetEntity = _context.Interaction.Member.Value.User.Value;
        var hierarchyResult = await GetHierarchyAsync(targetEntity, target, ct);

        if (!hierarchyResult.IsSuccess)
        {
            return (Result)hierarchyResult;
        }
        
        var actualLevel = hierarchyResult.Entity;
        
        if (actualLevel == HierarchyLevel.Higher)
        {
            return Result.FromSuccess();
        }
        
        var errorMessage = (expectedLevel, hierarchy: actualLevel) switch 
        {
            (HierarchyLevel.Higher, HierarchyLevel.Lower) => "Your roles are above mine. I can't help here.",
            (HierarchyLevel.Lower, HierarchyLevel.Higher) => "Your roles are below mine. I can't help here.",
            (_, HierarchyLevel.Equal) => "Your roles are equal to mine. I can't help here.",
            _ => throw new ArgumentOutOfRangeException(nameof(expectedLevel), expectedLevel, null)
        };
        
        return new InvalidOperationError(errorMessage);
    }
    
    
    public async ValueTask<Result> CheckAsync(EnsureHierarchyAttribute attribute, IUser data, CancellationToken ct = default)
    {
        var expectedLevel = attribute.Level;
        var target = attribute.Target;

        if (target is not HierarchyTarget.Invoker)
        {
            return new InvalidOperationError($"{nameof(HierarchyTarget.Self)} is not supported for a command-level attribute.");
        }
        
        var targetEntity = _context.Interaction.Member.Value.User.Value;
        var hierarchyResult = await GetHierarchyAsync(targetEntity, target, ct);

        if (!hierarchyResult.IsSuccess)
        {
            return (Result)hierarchyResult;
        }
        
        var actualLevel = hierarchyResult.Entity;
        
        if (actualLevel == HierarchyLevel.Higher)
        {
            return Result.FromSuccess();
        }
        
        var errorMessage = (expectedLevel, actualLevel, target) switch
        {
            (HierarchyLevel.Higher, HierarchyLevel.Lower, HierarchyTarget.Invoker) => "Their roles are above yours. I can't help here.",
            (HierarchyLevel.Higher, HierarchyLevel.Lower, HierarchyTarget.Self) => "Their roles are above mine. I can't help here.",
            (_, HierarchyLevel.Equal, _) => "Your roles are equal. I can't help here.",
            _ => "There's an issue with hierachy, and that's all I know (Expected level: {expectedLevel}, Actual level: {actualLevel}, Target: {target})."
        };
        
        return new InvalidOperationError(errorMessage);
    }

    private async Task<Result<HierarchyLevel>> GetHierarchyAsync(IUser targetEntity, HierarchyTarget target, CancellationToken ct)
    {
        var secondaryTargetID = target switch
        {
            HierarchyTarget.Self => _self.ID,
            HierarchyTarget.Invoker => _context.Interaction.Member.Value.User.Value.ID,
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
        };

        var secondaryTargetResult = await _guilds.GetGuildMemberAsync(_context.Interaction.GuildID.Value, secondaryTargetID, ct);
        
        if (!secondaryTargetResult.IsSuccess)
        {
            return Result<HierarchyLevel>.FromError(secondaryTargetResult);
        }
        
        var secondaryTargetMember = secondaryTargetResult.Entity;
        
        var targetMemberResult = await _guilds.GetGuildMemberAsync(_context.Interaction.GuildID.Value, targetEntity.ID, ct);
        
        if (!targetMemberResult.IsSuccess)
        {
            return Result<HierarchyLevel>.FromError(targetMemberResult);
        }
        
        var targetMember = targetMemberResult.Entity;
        
        var guildRolesResult = await _guilds.GetGuildRolesAsync(_context.Interaction.GuildID.Value, ct);
        
        if (!guildRolesResult.IsSuccess)
        {
            return Result<HierarchyLevel>.FromError(guildRolesResult);
        }
        
        var secondaryTargetRoles = secondaryTargetMember.Roles;
        var targetRoles = targetMember.Roles;
        var guildRoles = guildRolesResult.Entity.ToDictionary(x => x.ID, x => x);
        
        var guildId = _context.Interaction.GuildID.Value;
        
        var secondaryTargetHighestRole = secondaryTargetRoles
                              .Select(x => guildRoles[x])
                              .OrderByDescending(x => x.Position)
                              .ThenBy(r => r.ID)
                              .FirstOrDefault() ?? guildRoles[guildId];

        var targetHighestRole = targetRoles
                                .Select(x => guildRoles[x])
                                .OrderByDescending(x => x.Position)
                                .ThenBy(r => r.ID)
                                .FirstOrDefault() ?? guildRoles[guildId];
        
        
#pragma warning disable CS8846 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        return (secondaryTargetHighestRole.Position - targetHighestRole.Position, secondaryTargetHighestRole.ID,  targetHighestRole.ID) switch
#pragma warning restore CS8846
        {
            (_, var f, var s) when f == s => HierarchyLevel.Equal,
            (0, var f, var s) when f > s => HierarchyLevel.Higher,
            (0, var f, var s) when f < s => HierarchyLevel.Lower,
            (> 0, _, _) => HierarchyLevel.Higher,
            (< 0, _, _) => HierarchyLevel.Lower
        };
        
    }
}
