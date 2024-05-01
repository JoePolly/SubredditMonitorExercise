namespace SubredditMonitorExercise.Types.Interfaces;

/// <summary>
/// An interface which defines mechanisms for interacting with a social media API, as well as marking posts as tracked
/// for purposes of tracking ongoing updates, such as new comments or votes.
/// </summary>
public interface ISocialMediaApi
{
    /// <summary>
    /// Retrieve the next set of posts from the social media API.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<ISocialMediaPost>> GetNextPostsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve the specific posts with the given IDs from the social media API.
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<ISocialMediaPost>> GetSpecificPostsAsync(IEnumerable<string> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark a post as tracked for ongoing updates.
    /// </summary>
    /// <param name="id"></param>
    void TrackPostId(string id);

    /// <summary>
    /// Mark a post as no longer tracked for ongoing updates.
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> GetTrackedPostIds();

    /// <summary>
    /// The number of requests remaining in the current rate limit window.
    /// </summary>
    int RateLimitRemaining { get; }

    /// <summary>
    /// The number of seconds remaining in the current rate limit window.
    /// </summary>
    int RateLimitReset { get; }
    
    /// <summary>
    /// The number of calls that are made per fetch. This is used to calculate the how often calls should be made to
    /// avoid exceeding the rate limit. 
    /// </summary>
    int CallsPerFetch { get; }

    /// <summary>
    /// The ID of the social media API.
    /// <remarks>Singleton Service API clients should use the name of the class. If the social media API needs a User scope,
    /// it would be a good idea to add it here as well.</remarks>
    /// </summary>
    string Id { get; }
}