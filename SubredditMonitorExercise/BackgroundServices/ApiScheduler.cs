using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SubredditMonitorExercise.Types.Interfaces;

namespace SubredditMonitorExercise.BackgroundServices;

public sealed class ApiScheduler : BackgroundService
{
    private readonly TimeSpan _minimumInterval;
    private readonly ILogger _logger;
    private readonly Dictionary<string, ISocialMediaApi> _apis = new();
    private readonly PriorityQueue<string, DateTimeOffset> _apiQueue = new();
    private readonly int _warningThreshold;

    public ApiScheduler(IPostFeed postFeed, IConfiguration configuration, IEnumerable<ISocialMediaApi> apis,
        ILogger<ApiScheduler> logger)
    {
        _logger = logger;
        PostFeed = postFeed;

        foreach (var api in apis) RegisterApi(api);

        _minimumInterval =
            TimeSpan.FromMilliseconds(Math.Max(configuration.GetValue("ApiScheduler:MinimumIntervalMs", 500), 100));
        _warningThreshold = configuration.GetValue("ApiScheduler:FeedWarningThreshold", 100);
    }

    private IPostFeed PostFeed { get; }

    private void RegisterApi(ISocialMediaApi api)
    {
        _apis.Add(api.Id, api);
        _apiQueue.Enqueue(api.Id, DateTimeOffset.UtcNow);
    }

    private TimeSpan CalculateIntervalForApi(ISocialMediaApi api)
    {
        var interval = TimeSpan.Zero;

        if (api.RateLimitRemaining == 0)
        {
            interval = TimeSpan.FromSeconds(api.RateLimitReset);
        }
        else if (api.CallsPerFetch > 0)
        {
            // Ceiling here to err on the side of caution and not hit the rate limit
            var intervalSeconds =
                (int)Math.Ceiling(api.RateLimitReset / (api.RateLimitRemaining / (float)api.CallsPerFetch));
            interval = TimeSpan.FromSeconds(intervalSeconds);
        }

        if (interval < _minimumInterval) interval = _minimumInterval;

        return interval;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_apiQueue.Count <= 0)
            {
                await Task.Delay(500, stoppingToken);
                continue;
            }

            if (!_apiQueue.TryPeek(out _, out var priority) || priority > DateTimeOffset.UtcNow)
            {
                await Task.Delay(100, stoppingToken);
                continue;
            }

            var id = _apiQueue.Dequeue();

            _logger.LogInformation("Fetching posts from API {Id}", id);
            var socialMediaApi = _apis[id];

            var tasks = new[]
            {
                socialMediaApi.GetSpecificPostsAsync(socialMediaApi.GetTrackedPostIds(), stoppingToken),
                socialMediaApi.GetNextPostsAsync(stoppingToken)
            };

            _ = Task.WhenAll(tasks)
                .ContinueWith(posts =>
                {
                    foreach (var post in posts.Result.SelectMany(p => p))
                    {
                        PostFeed.EnqueuePost(post);
                        socialMediaApi.TrackPostId(post.Id);
                    }

                    var postFeedCount = PostFeed.Count;
                    if (postFeedCount >= _warningThreshold) _logger.LogWarning("Post queue has reached {PostFeedCount} posts", postFeedCount);

                    var interval = CalculateIntervalForApi(socialMediaApi);
                    _logger.LogDebug("Calculated interval for API {Id} as {Interval}", id, interval);
                    _apiQueue.Enqueue(id, DateTimeOffset.UtcNow + interval);
                }, stoppingToken);
        }
    }
}