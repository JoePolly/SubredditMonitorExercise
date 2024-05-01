using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using NLog.Time;
using SubredditMonitorExercise.BackgroundServices;
using SubredditMonitorExercise.Reddit;
using SubredditMonitorExercise.Storage;
using SubredditMonitorExercise.Types.Interfaces;

using LogLevel = NLog.LogLevel;

var appBuilder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ApplicationName = "SubredditMonitor",
    Args = args
});

/*
 * Add config.json file to the configuration builder. This is in addition to the environment and args configurations
 * that are automatically added by Host.CreateApplicationBuilder.
 */
appBuilder.Configuration.AddJsonFile("config.json", true);

/*
 * Set up API clients. Any number of clients can be registered here as long as they implement ISocialMediaApi.
 */
if (appBuilder.Configuration.GetValue<string?>("Reddit:ClientId") != null)
{
    appBuilder.Services.AddSingleton<RedditAccessTokenProvider>();
    appBuilder.Services.AddSingleton<ISocialMediaApi, RedditClient>();
}

/*
 * Set up the post feed singleton service. The included implementation acts as a queue with rudimentary deduplication.
 * In a more complex system, this could be implemented as a way to interact with a messaging service such as AWS SQS
 * or Azure Service Bus. 
 */
appBuilder.Services.AddSingleton<IPostFeed, PostFeed>();

/*
 * Set up the post store singleton service. The included implementation stores posts in-memory.
 * In a more complex system, this could be implemented as a way to interact with a database or other storage system.
 * The data stored by this service is non-relational and would be a great candidate for NoSQL storage such as
 * MongoDB or AWS DynamoDB.
 */
appBuilder.Services.AddSingleton<IPostStore, PostMemoryStore>();

/*
 * Set up Hosted Services.
 * These services run in the background and are handled as part of the Host Application system provided by
 * Microsoft.Extensions.Hosting.
 *
 * In a more complex system, these services could be implemented as separate microservices or expanded with new hosted
 * services such as an HTTPListener for an API or a WebSocket server for real-time updates.
 */
appBuilder.Services.AddHostedService<ApiScheduler>();
appBuilder.Services.AddHostedService<PostConsumer>();

if (appBuilder.Configuration.GetValue("StatisticsWriter:Enabled", true))
{
    appBuilder.Services.AddHostedService<StatisticsWriter>();
}

/*
 * Configure NLog.
 */

LogManager.Setup().LoadConfiguration(builder =>
{
    if (appBuilder.Configuration.GetValue("Logging:Console:Enabled", true))
    {
        var consoleLogLevel = appBuilder.Configuration.GetValue("Logging:Console:LogLevel", LogLevel.Info);
        builder.ForLogger().FilterMinLevel(consoleLogLevel).WriteToConsole();
    }

    builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToFile("${basedir}/logs/${date:format=yyyy-MM-dd-HH}.log");
});
TimeSource.Current = new AccurateUtcTimeSource();

appBuilder.Logging.ClearProviders();
appBuilder.Logging.AddNLog();

/*
 * Build the host and run it.
 */
using var host = appBuilder.Build();

using CancellationTokenSource cancellationTokenSource = new();
await host.RunAsync(cancellationTokenSource.Token);