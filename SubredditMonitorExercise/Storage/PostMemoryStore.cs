using SubredditMonitorExercise.Types.Interfaces;

namespace SubredditMonitorExercise.Storage;

public class PostMemoryStore : IPostStore
{
    private readonly Dictionary<string, ISocialMediaPost> _posts = new();

    public void Insert(ISocialMediaPost post)
    {
        _posts.Add(post.Id, post);
    }

    public bool Delete(string id)
    {
        return _posts.Remove(id);
    }

    public bool Exists(string id)
    {
        return _posts.ContainsKey(id);
    }

    public void Clear()
    {
        _posts.Clear();
    }

    public IReadOnlyCollection<ISocialMediaPost> GetAllPosts()
    {
        return _posts.Values.ToList();
    }

    public IReadOnlyCollection<ISocialMediaPost> GetAllPostsByUser(string user)
    {
        return _posts.Values.Where(p => p.Author == user).ToList();
    }

    public IReadOnlyCollection<ISocialMediaPost> GetTopPosts(int count = 10)
    {
        return _posts.Values.OrderByDescending(p => p.Score).Take(count).ToList();
    }

    public IReadOnlyCollection<ISocialMediaPost> GetTopPostsByUser(string user, int count = 10)
    {
        return _posts.Values.Where(p => p.Author == user).OrderByDescending(p => p.Score).Take(count).ToList();
    }

    public IReadOnlyDictionary<string, int> GetPostCountsByUser()
    {
        return _posts.Values.GroupBy(p => p.Author).ToDictionary(g => g.Key, g => g.Count());
    }

    public void SetPostScore(string postId, int postScore)
    {
        if (_posts.TryGetValue(postId, out var post)) post.Score = postScore;
    }
}