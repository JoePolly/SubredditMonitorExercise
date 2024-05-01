namespace SubredditMonitorExercise.Types.Interfaces;

public interface IPostFeed
{
    /// <summary>
    /// Enqueue a post in the feed.
    /// </summary>
    /// <param name="post"></param>
    /// <returns></returns>
    int EnqueuePost(ISocialMediaPost post);
    
    /// <summary>
    /// Dequeue the next post from the feed.
    /// </summary>
    /// <returns></returns>
    ISocialMediaPost? DequeuePost();
    
    /// <summary>
    /// Returns true if the feed is empty.
    /// </summary>
    /// <returns></returns>
    bool IsEmpty();
    
    /// <summary>
    /// The number of posts currently in the feed.
    /// </summary>
    int Count { get; }
}