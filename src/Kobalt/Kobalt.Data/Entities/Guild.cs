﻿using Remora.Rest.Core;

namespace Kobalt.Data.Entities;

public class Guild
{
    public Snowflake ID { get; set; }
    
    public List<GuildUserJoiner> Users { get; set; } = new();
    // TODO: Configurations
}