namespace SubredditMonitorExercise.Types.Interfaces;

public interface IPostStore
{
    /// <summary>
    /// Insert the specified post object.
    /// </summary>
    /// <param name="post"></param>
    void Insert(ISocialMediaPost post);
    
    /// <summary>
    /// Delete the specified post. 
    /// </summary>
    /// <param name="id"></param>
    /// <returns>true if deleted, false otherwise</returns>
    bool Delete(string id);
    
    /// <summary>
    /// Returns true if the specified post exists in the data store.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    bool Exists(string id);

    /// <summary>
    /// Truncate the data store.
    /// </summary>
    void Clear();

    /// <summary>
    /// Get all stored posts.
    /// </summary>
    /// <returns></returns>
    IReadOnlyCollection<ISocialMediaPost> GetAllPosts();

    /// <summary>
    /// Get all posts by the specified user.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    IReadOnlyCollection<ISocialMediaPost> GetAllPostsByUser(string user);

    /// <summary>
    /// Get the top posts.
    /// </summary>
    /// <param name="count">Number of posts</param>
    /// <returns></returns>
    IReadOnlyCollection<ISocialMediaPost> GetTopPosts(int count = 10);

    /// <summary>
    /// Retrieve the top posts by the specified user.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="count">Number of Posts</param>
    /// <returns></returns>
    IReadOnlyCollection<ISocialMediaPost> GetTopPostsByUser(string user, int count = 10);

    /// <summary>
    /// Retrieve the number of posts by each user.
    /// </summary>
    /// <returns></returns>
    IReadOnlyDictionary<string, int> GetPostCountsByUser();

    /// <summary>
    /// Update the score of the specified post.
    /// </summary>
    /// <param name="postId"></param>
    /// <param name="postScore"></param>
    void SetPostScore(string postId, int postScore);
}