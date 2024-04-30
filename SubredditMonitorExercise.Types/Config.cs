using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace SubredditMonitorExercise.Types;

[PublicAPI]
public class Config
{
    [JsonPropertyName("subreddit")]
    [JsonRequired]
    public required string Subreddit { get; set; }

    [JsonPropertyName("clientId")]
    [JsonRequired]
    public required string ClientId { get; set; }
    
    [JsonPropertyName("clientSecret")]
    [JsonRequired]
    public required string ClientSecret { get; set; }
}