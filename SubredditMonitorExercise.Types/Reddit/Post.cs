using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace SubredditMonitorExercise.Types.Reddit;

/// <summary>
/// Reddit API type which represents an individual post.
/// </summary>
[PublicAPI]
public class Post
{
    [JsonPropertyName("permalink")]
    public required string Permalink { get; set; }
    
    [JsonPropertyName("ups")]
    public required int Upvotes { get; set; }
    
    [JsonPropertyName("title")]
    public required string Title { get; set; }
    
    [JsonPropertyName("created_utc")]
    public required double CreatedUtc { get; set; }
    
    public DateTimeOffset Created => DateTimeOffset.FromUnixTimeSeconds((long)CreatedUtc);
}