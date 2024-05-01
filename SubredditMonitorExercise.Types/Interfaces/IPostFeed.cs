namespace SubredditMonitorExercise.Types.Interfaces;

public interface IPostFeed
{
    void EnqueuePost(ISocialMediaPost post);
    ISocialMediaPost? DequeuePost();
    bool IsEmpty();
}