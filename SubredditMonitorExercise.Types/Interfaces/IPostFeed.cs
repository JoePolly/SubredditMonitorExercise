namespace SubredditMonitorExercise.Types.Interfaces;

public interface IPostFeed
{
    int EnqueuePost(ISocialMediaPost post);
    ISocialMediaPost? DequeuePost();
    bool IsEmpty();
    int Count { get; }
}