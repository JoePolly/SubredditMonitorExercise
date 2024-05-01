using System.Text.Json.Serialization;
using JetBrains.Annotations;
using SubredditMonitorExercise.Types.Interfaces;

namespace SubredditMonitorExercise.Types.Reddit;

/// <summary>
/// Reddit API type which represents an individual post.
/// </summary>
[PublicAPI]
public class Post : ISocialMediaPost
{
    [JsonPropertyName("permalink")] public required string Permalink { get; set; }

    [JsonPropertyName("ups")] public required int Upvotes { get; set; }

    [JsonPropertyName("title")] public required string Title { get; set; }

    [JsonPropertyName("created_utc")] public required double CreatedUtc { get; set; }

    [JsonPropertyName("author")] public required string Author { get; set; }

    [JsonPropertyName("name")] public required string FullName { get; set; }

    DateTimeOffset ISocialMediaPost.Created => DateTimeOffset.FromUnixTimeSeconds((long)CreatedUtc);

    string ISocialMediaPost.Url => $"https://www.reddit.com{Permalink}";

    int ISocialMediaPost.Score
    {
        get => Upvotes;
        set => Upvotes = value;
    }

    string ISocialMediaPost.Id => FullName;

    public DateTimeOffset FetchTime { get; set; } = DateTimeOffset.UtcNow;
}