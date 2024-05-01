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

appBuilder.Configuration.AddJsonFile("config.json", true);

if (appBuilder.Configuration.GetValue<string?>("Reddit:ClientId") != null)
{
    appBuilder.Services.AddSingleton<RedditAccessTokenProvider>();
    appBuilder.Services.AddSingleton<ISocialMediaApi, RedditClient>();
}

appBuilder.Services.AddSingleton<IPostFeed, PostFeed>();
appBuilder.Services.AddSingleton<IPostStore, PostMemoryStore>();

appBuilder.Services.AddHostedService<ApiScheduler>();
appBuilder.Services.AddHostedService<PostConsumer>();

if (appBuilder.Configuration.GetValue<bool>("StatisticsWriter:Enabled", true))
{
    appBuilder.Services.AddHostedService<StatisticsWriter>();
}

LogManager.Setup().LoadConfiguration(builder =>
{
    if (appBuilder.Configuration.GetValue("Logging:Console:Enabled", true))
    {
        var consoleLogLevel = appBuilder.Configuration.GetValue("Logging:Console:LogLevel", LogLevel.Info);
        builder.ForLogger().FilterMinLevel(consoleLogLevel).WriteToConsole();
    }

    builder.ForLogger().FilterMinLevel(LogLevel.Debug).WriteToFile("${basedir}/logs/${shortdate}.log",
        maxArchiveFiles: 4, archiveAboveSize: 10_000);
});
TimeSource.Current = new AccurateUtcTimeSource();


appBuilder.Logging.ClearProviders();
appBuilder.Logging.AddNLog();

using var host = appBuilder.Build();

using CancellationTokenSource cancellationTokenSource = new();
cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(10));

await host.RunAsync(cancellationTokenSource.Token);