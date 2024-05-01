using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Authenticators.OAuth2;
using SubredditMonitorExercise.Types.Interfaces;
using SubredditMonitorExercise.Types.Reddit;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SubredditMonitorExercise.Reddit;

public sealed class RedditClient : IDisposable, ISocialMediaApi
{
    /// <summary>
    /// Approximate number of calls left before being rate limited
    /// </summary>
    public int RateLimitRemaining { get; private set; }

    /// <summary>
    /// Approximate time in seconds until the rate limit resets
    /// </summary>
    public int RateLimitReset { get; private set; }

    int ISocialMediaApi.CallsPerFetch => Subreddits.Count;

    private readonly RedditAccessTokenProvider _accessTokenProvider;

    private readonly ILogger _logger;


    /// <summary>
    /// Start time of the program, used to determine which posts are new.
    /// </summary>
    private readonly DateTimeOffset _startTime = DateTimeOffset.UtcNow;

    private const string UserAgent = "SubredditMonitorExercise by u/JRPolly";


    /// <summary>
    /// The time at which the rate limit was last updated. Used to mitigate race conditions when executing multiple calls.
    /// </summary>
    private DateTimeOffset RateLimitLastUpdated { get; set; }

    string ISocialMediaApi.Id => nameof(RedditClient);

    private Dictionary<string, SubredditTrackingMetadata> Subreddits { get; } = new();

    /// <summary>
    /// Per the Reddit API documentation, requests made using the client credentials flow should be made to the oauth.reddit.com domain.
    /// </summary>
    private RestClient OauthRestClient { get; } = new("https://oauth.reddit.com");

    public RedditClient(IConfiguration config, RedditAccessTokenProvider accessTokenProvider,
        ILogger<RedditClient> logger)
    {
        _logger = logger;
        _accessTokenProvider = accessTokenProvider;

        var lookbackPeriod = config.GetValue<int?>("LookbackPeriod");
        if (lookbackPeriod != null)
        {
            _startTime = _startTime.AddSeconds(-lookbackPeriod.Value);
            _logger.LogInformation(
                "Lookback {LookbackPeriod} seconds. Program will consider any post after {StartTime} as new",
                lookbackPeriod, _startTime);
        }

        var subreddits = config.GetSection("Reddit:Subreddits").Get<string[]>();
        if (subreddits == null || subreddits.Length == 0)
        {
            _logger.LogWarning("No subreddits found in configuration. Add Subreddits in the Reddit:Subreddits section");
            return;
        }

        foreach (var subreddit in subreddits) SubscribeSubreddit(subreddit);
    }

    public async Task<IEnumerable<ISocialMediaPost>> GetSpecificPostsAsync(IEnumerable<string> ids,
        CancellationToken stoppingToken = default)
    {
        await Task.Delay(5000, stoppingToken);
        return Array.Empty<ISocialMediaPost>();
    }

    public async Task<IEnumerable<ISocialMediaPost>> GetNextPostsAsync(CancellationToken stoppingToken = default)
    {
        var tasks = Subreddits.Keys.Select(subreddit => GetNextSubredditPosts(subreddit, stoppingToken)).ToArray();

        var posts = await Task.WhenAll(tasks);

        return posts.SelectMany(p => p);
    }

    public void Dispose()
    {
        OauthRestClient.Dispose();
    }

    private void SubscribeSubreddit(string subreddit)
    {
        subreddit = subreddit.Trim().Trim('/');
        if (subreddit.StartsWith("r/")) subreddit = subreddit[2..];

        if (Subreddits.ContainsKey(subreddit))
        {
            _logger.LogWarning("Attempted to subscribe to r/{Subreddit} more than once", subreddit);
            return;
        }

        Subreddits[subreddit] = new SubredditTrackingMetadata();
        _logger.LogInformation("Subscribed to r/{Subreddit}", subreddit);
    }

    private async Task<IEnumerable<Post>> GetNextSubredditPosts(string subreddit,
        CancellationToken stoppingToken = default)
    {
        if (!Subreddits.TryGetValue(subreddit, out var tracker))
        {
            _logger.LogError("Attempted to get posts for r/{Subreddit} without being subscribed", subreddit);
            return Array.Empty<Post>();
        }

        var posts = await GetSubredditPosts(subreddit, tracker.Before, tracker.Count, stoppingToken);

        return posts;
    }

    private async Task<IEnumerable<Post>> GetSubredditPosts(string subreddit, string? before = null, int? count = null,
        CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Getting subreddit posts for r/{Subreddit}", subreddit);

        var response = await FetchNewPostsForSubreddit(subreddit, before, count, stoppingToken);

        if (!response.IsSuccessful || response.Data == null)
        {
            _logger.LogError("Failed to get subreddit posts for r/{Subreddit}:\n{ErrorMessage}", subreddit,
                response.ErrorMessage);
            return Array.Empty<Post>();
        }

        var startUnixSeconds = _startTime.ToUnixTimeSeconds();

        var posts = response.Data.Data.Children
            .Select(kind => kind.Data)
            .Where(post => post.CreatedUtc > startUnixSeconds)
            .OrderByDescending(post => post.CreatedUtc)
            .ToArray();

        if (posts.Length == 0)
        {
            _logger.LogInformation("No new posts found for r/{Subreddit}", subreddit);
            return Array.Empty<Post>();
        }

        // Update the posts with the correct FetchTime
        if (response.Headers != null)
        {
            var dateHeader = response.Headers.FirstOrDefault(h => h.Name == "Date")?.Value;
            var date = DateTimeOffset.UtcNow;

            if (dateHeader != null) date = DateTimeOffset.Parse(dateHeader.ToString()!);

            _logger.LogDebug("Response received at {Date}", date);
            foreach (Post dataChild in response.Data.Data.Children) dataChild.FetchTime = date;
        }

        var postCount = response.Data.Data.Children.Count;
        lock (Subreddits[subreddit])
        {
            Subreddits[subreddit].Before = posts.First().FullName;
            Subreddits[subreddit].Count += postCount;
        }

        return posts.ToArray();
    }

    private async Task<RestResponse<Kind<ListingData<Post>>>> FetchNewPostsForSubreddit(string subreddit,
        string? before, int? count, CancellationToken stoppingToken = default)
    {
        _logger.LogDebug("Using request parameters before={Before}, count={Count}", before, count);
        var request = CreateRequest($"r/{subreddit}/new");

        if (before != null) request.AddParameter("before", before);

        if (count != null) request.AddParameter("count", count.ToString());

        request.AddParameter("limit", "100");

        var response = await OauthRestClient.ExecuteAsync<Kind<ListingData<Post>>>(request, stoppingToken);
        HandleRateLimit(response);

        return response;
    }

    private void HandleRateLimit(RestResponseBase response)
    {
        if (response.Headers == null) return;

        var remainingHeader = response.Headers.FirstOrDefault(h => h.Name == "x-ratelimit-remaining")?.Value;
        var resetHeader = response.Headers.FirstOrDefault(h => h.Name == "x-ratelimit-reset")?.Value;

        if (remainingHeader == null || resetHeader == null)
            // Rate limit headers not found, skip.
            return;

        var dateHeader = response.Headers.FirstOrDefault(h => h.Name == "Date")?.Value;

        try
        {
            var remaining = float.Parse(remainingHeader.ToString()!);
            var reset = float.Parse(resetHeader.ToString()!);
            var date = dateHeader == null ? DateTimeOffset.UtcNow : DateTimeOffset.Parse(dateHeader.ToString()!);

            // Keep these separate so that we don't overwrite the values if the parsing fails for one but not the other.

            if (date < RateLimitLastUpdated)
                // This is an old rate limit response, ignore it.
                return;

            RateLimitRemaining = (int)remaining;
            RateLimitReset = (int)reset;
            RateLimitLastUpdated = date;
            _logger.LogDebug("Rate limit remaining: {RateLimitRemaining}, reset in {RateLimitReset} seconds",
                RateLimitRemaining, RateLimitReset);
        }
        catch (FormatException formatException)
        {
            _logger.LogWarning("Reddit responded with a non-integer rate limit header.\n{FormatException}",
                formatException);
        }
        catch (OverflowException overflowException)
        {
            _logger.LogWarning("Reddit responded with a rate limit header that is too large.\n{OverflowException}",
                overflowException);
        }
    }

    private RestRequest CreateRequest(string resource)
    {
        var request = new RestRequest(resource)
        {
            Authenticator =
                new OAuth2AuthorizationRequestHeaderAuthenticator(_accessTokenProvider.GetAccessToken(), "Bearer")
        };

        request.AddHeader("User-Agent", UserAgent);

        return request;
    }
}