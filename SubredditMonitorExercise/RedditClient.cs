using NLog;
using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth2;
using SubredditMonitorExercise.Types.Reddit;

namespace SubredditMonitorExercise;

public class RedditClient : IDisposable
{
    private static Logger Logger { get; } = LogManager.GetCurrentClassLogger();
    
    private string ClientId { get; init; }
    private string ClientSecret { get; init; }
    
    /// <summary>
    /// Used for retrieving the access token.
    /// </summary>
    private RestClient RedditRestClient { get; set; }

    /// <summary>
    /// Per the Reddit API documentation, requests made using the client credentials flow should be made to the oauth.reddit.com domain.
    /// </summary>
    private RestClient OauthRestClient { get; set; } = new("https://oauth.reddit.com");
    
    private string? AccessToken { get; set; }
    
    private DateTimeOffset? AccessTokenExpiration { get; set; }

    public RedditClient(string clientId, string clientSecret)
    {
        ClientId = clientId;
        ClientSecret = clientSecret;
        
        RedditRestClient = new RestClient(new RestClientOptions("https://www.reddit.com")
        {
            Authenticator = new HttpBasicAuthenticator(ClientId, ClientSecret)
        });
    }

    public async Task<ListingData<Post>?> GetSubredditPosts(string subreddit, string? after = null, string? before = null, int? count = null)
    {
        Logger.Info($"Getting subreddit posts for {subreddit}");
        Logger.Debug($"Using parameters: after={after}, before={before}, count={count}");
        
        var request = await CreateRequest($"r/{subreddit}/new");
        
        if (after != null)
        {
            request.AddParameter("after", after);
        }
        
        if (before != null)
        {
            request.AddParameter("before", before);
        }
        
        if (count != null)
        {
            request.AddParameter("count", count.ToString());
        }
        
        var response = await OauthRestClient.ExecuteAsync<Kind<ListingData<Post>>>(request);
        
        if (!response.IsSuccessful || response.Data == null)
        {
            Logger.Error($"Failed to get subreddit posts for {subreddit}.\n{response.ErrorMessage}");
            return null;
        }

        return response.Data.Data;
    }

    public void Dispose()
    {
        RedditRestClient.Dispose();
        OauthRestClient.Dispose();
    }

    private async Task RefreshAccessToken()
    {
        Logger.Info("Requesting access token.");
        var request = new RestRequest("api/v1/access_token");
        request.AddParameter("grant_type", "client_credentials");

        var tokenResponse = await RedditRestClient.ExecutePostAsync<AccessToken>(request);

        if (!tokenResponse.IsSuccessful || tokenResponse.Data == null)
        {
            Logger.Fatal($"Failed to get access token.\n{tokenResponse.ErrorMessage}");
            throw new ApplicationException($"Failed to get access token.\n{tokenResponse.ErrorMessage}");
        }
        
        AccessTokenExpiration = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.Data.ExpiresIn);
        AccessToken = tokenResponse.Data.Token;
        
        Logger.Info($"Access token received, valid until {AccessTokenExpiration:g}");
    }

    private async Task<RestRequest> CreateRequest(string resource)
    {
        await EnsureAccessToken();
        
        return new(resource)
        {
            Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(AccessToken!, "Bearer")
        };
    }

    private async Task EnsureAccessToken()
    {
        if (AccessTokenExpiration == null || AccessTokenExpiration < DateTimeOffset.UtcNow)
        {
            await RefreshAccessToken();
        }
        
        if (AccessToken == null)
        {
            Logger.Fatal("Access token is null");
            throw new ApplicationException("Access token is null");
        }
    }
}