using Moq;
using SubredditMonitorExercise.Types.Interfaces;

namespace Tests.Utilities;

public static class MockBuilders
{
    public static Mock<ISocialMediaPost> Post(string id, string author = "author", int score = 0)
    {
        Mock<ISocialMediaPost> post = new();
        post.SetupGet(p => p.Id).Returns(id);
        post.SetupGet(p => p.Author).Returns(author);
        
        post.SetupProperty(p => p.Score);
        post.Object.Score = score;
        
        return post;
    }
    
}