using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace SubredditMonitorExercise.Types.Reddit;

/// <summary>
/// Reddit API type representing a Listing.
/// <remarks>
/// <para>We cannot currently treat this as an IEnumerable due to the way System.Text.Json handles IEnumerable types.</para>
/// <para>See https://github.com/dotnet/runtime/issues/63791 and https://stackoverflow.com/questions/74615202/system-text-json-how-to-serialise-an-ienumerable-like-a-regular-class for more info.</para>
/// </remarks>
/// </summary>
/// <typeparam name="T">The Kind type that will be contained by the listing data. E.g. Post</typeparam>
[PublicAPI]
public class ListingData<T>
{
    [JsonPropertyName("after")] public string? After { get; set; }

    [JsonPropertyName("before")] public string? Before { get; set; }
    [JsonPropertyName("children")] public required List<Kind<T>> Children { get; set; } = new();
}