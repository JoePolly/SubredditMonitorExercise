using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace SubredditMonitorExercise.Types.Reddit;

[PublicAPI]
public class AccessToken
{
    [JsonPropertyName("access_token")] public required string Token { get; set; }

    [JsonPropertyName("expires_in")] public required int ExpiresIn { get; set; }
}