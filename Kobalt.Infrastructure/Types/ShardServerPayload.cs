using System.Text.Json.Serialization;
using Kobalt.Infrastructure.Enums;

namespace Kobalt.Infrastructure.Types;

/// <summary>
/// Represents a payload sent to and from the server.
/// </summary>
/// <param name="Opcode">The operation code of this payload.</param>
/// <param name="Data">The data of the payload, if any.</param>
public record ShardServerPayload
(
    [property: JsonPropertyName("op")]
    ShardServerOpcode Opcode,
    
    [property: JsonPropertyName("d")]
    object? Data
);
