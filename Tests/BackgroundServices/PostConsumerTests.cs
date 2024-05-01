using Microsoft.Extensions.Logging;
using Moq;
using SubredditMonitorExercise.BackgroundServices;
using SubredditMonitorExercise.Storage;
using Tests.Utilities;

namespace Tests.BackgroundServices;

[TestFixture]
public class PostConsumerTests
{
    [Test]
    public void PostConsumer_StopAsync()
    {
        var postFeed = new PostFeed(Mock.Of<ILogger<PostFeed>>());
        var postStore = new PostMemoryStore();
        var consumer = new PostConsumer(postFeed, Mock.Of<ILogger<PostConsumer>>(), postStore);

        consumer.StartAsync(CancellationToken.None).Wait();
        consumer.StopAsync(CancellationToken.None).Wait();
        
        Assert.That(postStore.GetAllPosts(), Is.Empty);
    }
    
    [Test]
    public void PostConsumer_Consume_WithPosts()
    {
        var postFeed = new PostFeed(Mock.Of<ILogger<PostFeed>>());
        var postStore = new PostMemoryStore();
        var consumer = new PostConsumer(postFeed, Mock.Of<ILogger<PostConsumer>>(), postStore);
        
        postFeed.EnqueuePost(MockBuilders.Post("123", "author").Object);
        postFeed.EnqueuePost(MockBuilders.Post("456", "author").Object);

        CancellationTokenSource cts = new();
        consumer.StartAsync(cts.Token).Wait(cts.Token);

        cts.CancelAfter(1000);
        
        Task.Delay(2000).Wait();
        
        postFeed.EnqueuePost(MockBuilders.Post("789", "author").Object);
        
        
        Assert.That(postStore.Exists("123"), Is.True);
        Assert.That(postStore.Exists("456"), Is.True);
        Assert.That(postStore.Exists("789"), Is.False);
    }
    
    [Test]
    public void PostConsumer_Consume_WithExistingPost()
    {
        var postFeed = new PostFeed(Mock.Of<ILogger<PostFeed>>());
        var postStore = new PostMemoryStore();
        var consumer = new PostConsumer(postFeed, Mock.Of<ILogger<PostConsumer>>(), postStore);
        
        postStore.Insert(MockBuilders.Post("123", "author", 111).Object);
        Assert.That(postStore.GetAllPosts().Single().Score, Is.EqualTo(111));
        
        postFeed.EnqueuePost(MockBuilders.Post("123", "author", 999).Object);

        consumer.StartAsync(CancellationToken.None).Wait();
        Task.Delay(1000).Wait();
        consumer.StopAsync(CancellationToken.None).Wait();
        
        Assert.That(postStore.GetAllPosts().Single().Score, Is.EqualTo(999));
    }
}