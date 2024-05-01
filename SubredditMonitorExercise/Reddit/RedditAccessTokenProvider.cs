using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RestSharp;
using RestSharp.Authenticators;
using SubredditMonitorExercise.Types.Reddit;

namespace SubredditMonitorExercise.Reddit;

public sealed class RedditAccessTokenProvider : IDisposable
{
    private ILogger Logger { get; }
    private RestClient RedditRestClient { get; }
    private string? AccessToken { get; set; }
    private DateTimeOffset AccessTokenExpiration { get; set; }
    private object AccessTokenLock { get; } = new();

    public RedditAccessTokenProvider(IConfiguration config, ILogger<RedditAccessTokenProvider> logger)
    {
        Logger = logger;

        var clientId = config.GetValue<string>("Reddit:ClientId") ??
                       throw new ApplicationException("Client ID not found in configuration.");
        var clientSecret = config.GetValue<string>("Reddit:ClientSecret") ??
                           throw new ApplicationException("Client Secret not found in configuration.");

        RedditRestClient = new RestClient(new RestClientOptions("https://www.reddit.com")
        {
            Authenticator = new HttpBasicAuthenticator(clientId, clientSecret)
        });
    }

    public string GetAccessToken()
    {
        lock (AccessTokenLock)
        {
            if (string.IsNullOrEmpty(AccessToken) || DateTimeOffset.UtcNow >= AccessTokenExpiration)
            {
                Logger.LogInformation("Requesting access token");
                var request = new RestRequest("api/v1/access_token");
                request.AddParameter("grant_type", "client_credentials");

                var tokenResponse = RedditRestClient.ExecutePost<AccessToken>(request);

                if (!tokenResponse.IsSuccessful || tokenResponse.Data == null)
                {
                    Logger.LogCritical("Failed to get access token. Received error: {ErrorMessage}",
                        tokenResponse.ErrorMessage);
                    throw new ApplicationException($"Failed to get access token.\n{tokenResponse.ErrorMessage}");
                }

                AccessTokenExpiration = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.Data.ExpiresIn);
                AccessToken = tokenResponse.Data.Token;

                Logger.LogInformation("Access token received, valid until {AccessTokenExpiration}",
                    AccessTokenExpiration);
            }
        }

        return AccessToken;
    }

    public void Dispose()
    {
        RedditRestClient.Dispose();
    }
}