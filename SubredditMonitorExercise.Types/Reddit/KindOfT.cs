using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace SubredditMonitorExercise.Types.Reddit;

/// <summary>
/// Reddit API structure for a kind of data.
/// <remarks>The simplified exercise use of this project does not consume the "kind" JSON property.</remarks>
/// </summary>
/// <typeparam name="T">The type contained in "data".</typeparam>
[PublicAPI]
public class Kind<T>
{
    [JsonPropertyName("data")]
    [JsonRequired]
    public required T Data { get; set; }
    
    public static implicit operator T (Kind<T> kind) => kind.Data;
}