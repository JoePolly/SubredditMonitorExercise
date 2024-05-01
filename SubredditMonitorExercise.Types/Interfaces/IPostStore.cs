namespace SubredditMonitorExercise.Types.Interfaces;

public interface IPostStore
{
    void Insert(ISocialMediaPost post);
    bool Delete(string id);
    bool Exists(string id);
    void Clear();

    IReadOnlyCollection<ISocialMediaPost> GetAllPosts();

    IReadOnlyCollection<ISocialMediaPost> GetAllPostsByUser(string user);

    IReadOnlyCollection<ISocialMediaPost> GetTopPosts(int count = 10);

    IReadOnlyCollection<ISocialMediaPost> GetTopPostsByUser(string user, int count = 10);

    IReadOnlyDictionary<string, int> GetPostCountsByUser();

    void SetPostScore(string postId, int postScore);
}