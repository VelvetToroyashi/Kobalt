﻿using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Remora.Discord.API;
using Remora.Rest.Core;

namespace Kobalt.Shared.DatabaseConverters;

public sealed class SnowflakeConverter : ValueConverter<Snowflake, ulong>
{
    private static readonly ConverterMappingHints _defaultHints = new(precision: 20, scale: 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="SnowflakeConverter"/> class.
    /// </summary>
    public SnowflakeConverter()
    : base(sf => sf.Value, value => DiscordSnowflake.New(value), _defaultHints)
    { }
}

public sealed class NullableSnowflakeConverter : ValueConverter<Snowflake?, ulong?>
{
    private static readonly ConverterMappingHints _defaultHints = new(precision: 20, scale: 0);

    /// <summary>
    /// Initializes a new instance of the <see cref="SnowflakeConverter"/> class.
    /// </summary>
    public NullableSnowflakeConverter()
    : base(
        sf => sf.HasValue 
        ? sf.Value.Value 
        : default, 
        value => value.HasValue 
        ? DiscordSnowflake.New(value.Value) 
        : default, _defaultHints)
    { }
}
