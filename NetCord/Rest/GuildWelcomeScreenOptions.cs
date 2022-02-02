﻿using System.Text.Json.Serialization;

namespace NetCord;

public class GuildWelcomeScreenOptions
{
    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    [JsonPropertyName("welcome_channels")]
    public List<GuildWelcomeScreenChannelProperties>? WelcomeChannels { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}