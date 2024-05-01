using SubredditMonitorExercise.Types.Interfaces;
using SubredditMonitorExercise.Types.Reddit;

namespace Tests.Reddit;

[TestFixture]
public class PostTests
{
    [Test]
    public void Post_CorrectlyImplementsInterface()
    {
        var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        var post = new Post
        {
            Permalink = "/r/Some/Link",
            Upvotes = 999,
            Title = "Some Title",
            CreatedUtc = time,
            Author = "Some Author",
            FullName = "t3_whatever"
        };
        
        ISocialMediaPost socialMediaPost = post;
        
        Assert.Multiple(() =>
        {
            Assert.That(socialMediaPost.Created, Is.EqualTo(DateTimeOffset.FromUnixTimeSeconds(time)));
            Assert.That(socialMediaPost.Url, Is.EqualTo("https://www.reddit.com/r/Some/Link"));
            Assert.That(socialMediaPost.Score, Is.EqualTo(999));
            Assert.That(socialMediaPost.Id, Is.EqualTo("t3_whatever"));
            Assert.That(socialMediaPost.Author, Is.EqualTo("Some Author"));
            Assert.That(socialMediaPost.Title, Is.EqualTo("Some Title"));
        });
        
        socialMediaPost.Score = 111;
        
        Assert.That(post.Upvotes, Is.EqualTo(111));
    }
}