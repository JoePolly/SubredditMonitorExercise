using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SubredditMonitorExercise.Types.Interfaces;

namespace SubredditMonitorExercise.BackgroundServices;

public sealed class PostConsumer : BackgroundService
{
    private readonly IPostFeed _postFeed;
    private readonly ILogger _logger;
    private readonly IPostStore _postStore;

    public PostConsumer(IPostFeed postFeed, ILogger<PostConsumer> logger, IPostStore postStore)
    {
        _postFeed = postFeed;
        _logger = logger;
        _postStore = postStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_postFeed.IsEmpty())
            {
                await Task.Delay(500, stoppingToken);
                continue;
            }

            var post = _postFeed.DequeuePost()!;
            ProcessPost(post);
        }

        await Task.CompletedTask;
    }

    private void ProcessPost(ISocialMediaPost post)
    {
        _logger.LogInformation("Consumed post {PostId}", post.Id);

        if (_postStore.Exists(post.Id))
            // We just need to update the score.
            _postStore.SetPostScore(post.Id, post.Score);

        // If you want to do any processing on the post before storing it, you can do it here
        // For example, if you wanted to use an external service to analyze the post content.

        _logger.LogInformation(
            "Storing post {PostId} from {PostAuthor} with title {PostTitle} ({PostUrl}) with score {PostScore} at {PostCreated}",
            post.Id, post.Author, post.Title, post.Url, post.Score, post.Created);
        _postStore.Insert(post);
    }
}