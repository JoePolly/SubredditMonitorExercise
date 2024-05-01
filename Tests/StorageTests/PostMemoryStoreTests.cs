using Moq;
using SubredditMonitorExercise.Storage;
using SubredditMonitorExercise.Types.Interfaces;
using Tests.Utilities;

namespace Tests.StorageTests;

[TestFixture]
public class PostMemoryStoreTests
{
    [Test]
    public void PostMemoryStore_Insert()
    {
        PostMemoryStore store = new PostMemoryStore();
        Mock<ISocialMediaPost> post = MockBuilders.Post("123", "author");
        
        store.Insert(post.Object);
        
        Assert.That(store.Exists("123"), Is.True);
        Assert.That(store.GetAllPosts(), Contains.Item(post.Object));
    }

    [Test]
    public void PostMemoryStore_PostsByUser()
    {
        PostMemoryStore store = new PostMemoryStore();
        
        Mock<ISocialMediaPost> post1 = MockBuilders.Post("123", "author", 99);
        Mock<ISocialMediaPost> post2 = MockBuilders.Post("456", "author", 0);
        Mock<ISocialMediaPost> post3 = MockBuilders.Post("789", "other", 100);
        
        store.Insert(post1.Object);
        store.Insert(post2.Object);
        store.Insert(post3.Object);

        var allPostsByUser = store.GetAllPostsByUser("author");
        var allPostsByOther = store.GetAllPostsByUser("other");
        var postCountsByUser = store.GetPostCountsByUser();

        Assert.That(allPostsByUser, Contains.Item(post1.Object));
        Assert.That(allPostsByUser, Contains.Item(post2.Object));
        
        Assert.That(allPostsByOther, Contains.Item(post3.Object));
        
        Assert.That(postCountsByUser["author"], Is.EqualTo(2));
        Assert.That(postCountsByUser["other"], Is.EqualTo(1));
        
        Assert.That(store.GetTopPostsByUser("author", 1), Contains.Item(post1.Object));
        Assert.That(store.GetTopPostsByUser("author", 1), Does.Not.Contain(post2.Object));
    }
    
    [Test]
    public void PostMemoryStore_Delete()
    {
        PostMemoryStore store = new PostMemoryStore();
        Mock<ISocialMediaPost> post = MockBuilders.Post("123", "author");
        
        store.Insert(post.Object);
        
        Assert.That(store.Exists("123"), Is.True);
        
        store.Delete("123");
        
        Assert.That(store.Exists("123"), Is.False);
    }
    
    [Test]
    public void PostMemoryStore_Clear()
    {
        PostMemoryStore store = new PostMemoryStore();
        Mock<ISocialMediaPost> post = MockBuilders.Post("123", "author");
        
        store.Insert(post.Object);
        
        Assert.That(store.Exists("123"), Is.True);
        
        store.Clear();
        
        Assert.That(store.Exists("123"), Is.False);
    }

    [Test]
    public void PostMemoryStore_SetPostScore()
    {
        PostMemoryStore store = new PostMemoryStore();
        Mock<ISocialMediaPost> post = MockBuilders.Post("123", "author", 111);
        store.Insert(post.Object);

        var getPost = store.GetAllPosts().Single();
        Assert.That(getPost.Score, Is.EqualTo(111));
        Assert.That(getPost, Is.SameAs(post.Object));
        
        store.SetPostScore("123", 999);
        
        var getPostB = store.GetAllPosts().Single();
        Assert.That(getPostB.Score, Is.EqualTo(999));
        Assert.That(getPostB, Is.SameAs(post.Object));
    }

    [Test]
    public void PostMemoryStore_GetTopPosts()
    {
        PostMemoryStore store = new PostMemoryStore();
        ISocialMediaPost[] posts = new ISocialMediaPost[10];
        for (int i = 0; i < 10; i++)
        {
            Mock<ISocialMediaPost> post = MockBuilders.Post(i.ToString(), "author", i * 100);
            posts[i] = post.Object;
            store.Insert(post.Object);
        }
        
        var topPosts = store.GetTopPosts(5);
        
        Assert.That(topPosts.Count, Is.EqualTo(5));
        Assert.That(topPosts, Is.EquivalentTo(posts.Skip(5).Take(5)));
    }
}