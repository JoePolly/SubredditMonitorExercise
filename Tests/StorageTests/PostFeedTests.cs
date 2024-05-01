using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SubredditMonitorExercise.Storage;
using SubredditMonitorExercise.Types.Interfaces;
using Tests.Utilities;

namespace Tests.StorageTests;

[TestFixture]
[TestOf(typeof(PostFeed))]
public class PostFeedTests
{
    private ILogger<PostFeed> _logger;
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _logger = Mock.Of<ILogger<PostFeed>>();
    }
    
    [Test]
    public void PostFeed_CanEnqueue()
    {
        var feed = new PostFeed(_logger);
        ISocialMediaPost post = MockBuilders.Post("123", "author").Object;
            
        feed.EnqueuePost(post);
        
        Assert.That(feed.Count, Is.EqualTo(1));
        Assert.That(feed.IsEmpty(), Is.False);
    }
    
    [Test]
    public void PostFeed_CanDequeue()
    {
        var feed = new PostFeed(_logger);
        ISocialMediaPost post = MockBuilders.Post("123", "author").Object;
            
        feed.EnqueuePost(post);

        ISocialMediaPost? dequeued = feed.DequeuePost();
        
        Assert.That(feed.Count, Is.EqualTo(0));
        Assert.That(feed.IsEmpty(), Is.True);
        Assert.That(dequeued, Is.EqualTo(post));
    }
    
    [Test]
    public void PostFeed_CanDequeueEmpty()
    {
        var feed = new PostFeed(_logger);

        ISocialMediaPost? dequeued = feed.DequeuePost();
        
        Assert.That(feed.Count, Is.EqualTo(0));
        Assert.That(feed.IsEmpty(), Is.True);
        Assert.That(dequeued, Is.Null);
    }

    [Test]
    public void PostFeed_CanEnqueueDuplicateId()
    {
        var feed = new PostFeed(_logger);
        Mock<ISocialMediaPost> post = MockBuilders.Post("123", "author", 999);
        post.SetupGet(p => p.FetchTime).Returns(DateTimeOffset.Now);
        
        Mock<ISocialMediaPost> post2 = MockBuilders.Post("123", "author", 555);
        post2.SetupGet(p => p.FetchTime).Returns(DateTimeOffset.Now.AddSeconds(1));
        
        feed.EnqueuePost(post.Object);
        feed.EnqueuePost(post2.Object);
        
        Assert.That(feed.Count, Is.EqualTo(1));
        Assert.That(feed.IsEmpty(), Is.False);
        
        ISocialMediaPost? dequeued = feed.DequeuePost();
        
        Assert.That(feed.Count, Is.EqualTo(0));
        Assert.That(feed.IsEmpty(), Is.True);
        Assert.That(dequeued, Is.EqualTo(post2.Object));
    }
}