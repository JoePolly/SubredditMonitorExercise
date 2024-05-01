using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubredditMonitorExercise.Types.Interfaces;

namespace SubredditMonitorExercise.Storage;

/// <summary>
/// A feed of posts that can be enqueued and dequeued.
/// </summary>
public class PostFeed : IPostFeed
{
    private readonly ILogger _logger;
    private readonly Queue<string> _postQueue = new();
    private readonly Dictionary<string, ISocialMediaPost> _posts = new();

    private readonly object _lock = new();

    public PostFeed(ILogger<PostFeed> logger, IConfiguration configuration)
    {
        _logger = logger;
    }

    public int EnqueuePost(ISocialMediaPost post)
    {
        var count = 0;
        lock (_lock)
        {
            count = _postQueue.Count;
            if (_posts.ContainsKey(post.Id))
            {
                // Check if the incoming post is newer than the one we have
                if (_posts[post.Id].FetchTime < post.FetchTime)
                {
                    _posts[post.Id] = post;
                    _logger.LogInformation("Updated post {PostId} ({PostUrl}) with score {PostScore}", post.Id,
                        post.Url, post.Score);
                }

                return count;
            }

            _posts.Add(post.Id, post);
            _postQueue.Enqueue(post.Id);
            count++;
        }

        _logger.LogTrace(
            "Enqueued post {PostId} from {PostAuthor} with title {PostTitle} ({PostUrl}) with score {PostScore} at {PostCreated}",
            post.Id, post.Author, post.Title, post.Url, post.Score, post.Created);

        return count;
    }

    public ISocialMediaPost? DequeuePost()
    {
        lock (_lock)
        {
            if (_postQueue.Count <= 0) return null;

            var postId = _postQueue.Dequeue();
            var post = _posts[postId];
            _posts.Remove(postId);
            return post;
        }
    }

    public bool IsEmpty()
    {
        lock (_lock)
        {
            return _postQueue.Count <= 0;
        }
    }

    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _postQueue.Count;
            }
        }
    }
}