using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SubredditMonitorExercise.BackgroundServices;
using SubredditMonitorExercise.Storage;
using SubredditMonitorExercise.Types.Interfaces;

namespace Tests.BackgroundServices;

[TestFixture]
public class ApiSchedulerTests
{
    private PostFeed _postFeed;
    private PostMemoryStore _postStore;
    private IConfiguration _configuration;

    [SetUp]
    public void SetUp()
    {
        _postFeed = new PostFeed(Mock.Of<ILogger<PostFeed>>());
        _postStore = new PostMemoryStore();
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            {"ApiScheduler:MinimumIntervalMs", "500"},
            {"ApiScheduler:FeedWarningThreshold", "100"}
        }!).Build();
    }
    
    [Test]
    public void ApiScheduler_CanStartAndStop()
    {
        var scheduler = new ApiScheduler(_postFeed, _configuration, Array.Empty<ISocialMediaApi>(), Mock.Of<ILogger<ApiScheduler>>());
        
        scheduler.StartAsync(CancellationToken.None).Wait();
        scheduler.StopAsync(CancellationToken.None).Wait();
    }
    
    [Test]
    public void ApiScheduler_RateLimit_WithRemaining()
    {
        Mock<ISocialMediaApi> api = new();
        api.SetupGet(x => x.Id).Returns("test");
        api.SetupGet(a => a.RateLimitRemaining).Returns(60);
        api.SetupGet(a => a.RateLimitReset).Returns(60);
        api.SetupGet(a => a.CallsPerFetch).Returns(1);
        
        var scheduler = new ApiScheduler(_postFeed, _configuration, new[] {api.Object}, Mock.Of<ILogger<ApiScheduler>>());
        scheduler.StartAsync(CancellationToken.None).Wait();
        Task.Delay(2000).Wait();
        scheduler.StopAsync(CancellationToken.None).Wait();

        // Verify that GetNextPostsAsync was called once.
        api.Verify(a => a.GetNextPostsAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2));
        api.Verify(a => a.GetNextPostsAsync(It.IsAny<CancellationToken>()), Times.AtMost(3));
    }
    
    [Test]
    public void ApiScheduler_RateLimit_WithoutRemaining()
    {
        Mock<ISocialMediaApi> api = new();
        api.SetupGet(x => x.Id).Returns("test");
        api.SetupGet(a => a.RateLimitRemaining).Returns(0);
        api.SetupGet(a => a.RateLimitReset).Returns(60);
        api.SetupGet(a => a.CallsPerFetch).Returns(1);
        
        var scheduler = new ApiScheduler(_postFeed, _configuration, new[] {api.Object}, Mock.Of<ILogger<ApiScheduler>>());
        scheduler.StartAsync(CancellationToken.None).Wait();
        Task.Delay(2000).Wait();
        scheduler.StopAsync(CancellationToken.None).Wait();

        // Verify that GetNextPostsAsync was called once.
        api.Verify(a => a.GetNextPostsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}