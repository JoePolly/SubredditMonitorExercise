namespace SubredditMonitorExercise.Types.Interfaces;

public interface ISocialMediaPost
{
    string Author { get; }
    string Title { get; }
    string Url { get; }
    DateTimeOffset Created { get; }
    int Score { get; set; }
    string Id { get; }

    DateTimeOffset FetchTime { get; }
}