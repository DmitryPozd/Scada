using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Scada.Client.Models;

/// <summary>
/// Configuration for tags loaded from tags.json
/// </summary>
public class TagsConfiguration
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("totalTags")]
    public int TotalTags { get; set; }

    [JsonPropertyName("tags")]
    public List<TagDefinition> Tags { get; set; } = new();
}
