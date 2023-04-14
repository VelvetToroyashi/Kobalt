﻿using Kobalt.Shared.DTOs.Users;
using Mediator;
using Remora.Rest.Core;
using Remora.Results;

namespace Kobalt.Shared.Mediator.Users;

/// <summary>
/// Requests a user.
/// </summary>
/// <param name="ID">The ID of the user to request.</param>
public record GetUserRequest(Snowflake ID) : IRequest<Result<UserDTO>>;
