using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using SubredditMonitorExercise.Types.Interfaces;

namespace SubredditMonitorExercise.BackgroundServices;

public sealed class StatisticsWriter : BackgroundService
{
    private readonly IPostStore _postStore;
    private readonly string _filename;
    private readonly TimeSpan _writeInterval;

    public StatisticsWriter(IConfiguration configuration, IPostStore postStore)
    {
        _postStore = postStore;
        _filename = configuration.GetValue("StatisticsWriter:Filename", "statistics.json")!;
        _writeInterval = TimeSpan.FromMilliseconds(configuration.GetValue("StatisticsWriter:IntervalMs", 5000));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Get file write handle
            var file = new FileInfo(_filename);
            await using var stream = file.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            
            //Truncate file
            stream.SetLength(0);
            
            await using var writer = new StreamWriter(stream);

            var postCountsByUser = _postStore.GetPostCountsByUser();
            
            if (postCountsByUser.Count == 0)
            {
                // No posts have been saved yet.
                await JsonSerializer.SerializeAsync(writer.BaseStream, new
                    {
                        TotalPosts = 0,
                    },
                    new JsonSerializerOptions { WriteIndented = true }, stoppingToken);
            }
            else
            {
                var userWithMostPosts = postCountsByUser.MaxBy(pair => pair.Value).Key;
                var totalPosts = postCountsByUser.Sum(pair => pair.Value);
                var averagePostsPerUser = totalPosts / postCountsByUser.Count;

                var mostUpvotedPost = _postStore.GetTopPosts(1).SingleOrDefault();

                await JsonSerializer.SerializeAsync(writer.BaseStream, new
                    {
                        UserWithMostPosts = new
                        {
                            UserName = userWithMostPosts,
                            PostCount = postCountsByUser[userWithMostPosts]
                        },
                        AveragePostsPerUser = averagePostsPerUser,
                        TotalPosts = totalPosts,
                        MostUpvotedPost = mostUpvotedPost
                    },
                    new JsonSerializerOptions { WriteIndented = true }, stoppingToken);
            }

            await writer.FlushAsync();
            await Task.Delay(_writeInterval, stoppingToken);
        }
    }
}