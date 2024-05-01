namespace SubredditMonitorExercise.Types.Interfaces;

public interface ISocialMediaPost
{
    string Author { get; }
    string Title { get; }
    
    /// <summary>
    /// The full URL of the post.
    /// </summary>
    string Url { get; }
    
    /// <summary>
    /// Date and Time when the post was created.
    /// </summary>
    DateTimeOffset Created { get; }
    
    int Score { get; set; }
    
    string Id { get; }

    /// <summary>
    /// Date and Time when the post was fetched.
    /// </summary>
    DateTimeOffset FetchTime { get; }
}