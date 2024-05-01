namespace SubredditMonitorExercise.Types.Interfaces;

public interface ISocialMediaApi
{
    Task<IEnumerable<ISocialMediaPost>> GetNextPostsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<ISocialMediaPost>> GetSpecificPostsAsync(IEnumerable<string> ids,
        CancellationToken cancellationToken = default);

    void TrackPostId(string id);

    IEnumerable<string> GetTrackedPostIds();

    int RateLimitRemaining { get; }

    int RateLimitReset { get; }

    int CallsPerFetch { get; }

    string Id { get; }
}