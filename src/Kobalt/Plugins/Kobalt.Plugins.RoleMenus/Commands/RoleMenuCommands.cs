using System.ComponentModel;
using MediatR;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Results;

namespace Kobalt.Plugins.RoleMenus.Commands;

[Group("role-menu")]
[Description("Commands for managing role menus.")]
public class RoleMenuCommands(IMediator mediator) : CommandGroup
{
    
}