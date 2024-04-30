using RestSharp;
using RestSharp.Authenticators;
using RestSharp.Authenticators.OAuth2;
using SubredditMonitorExercise.Types.Reddit;

namespace SubredditMonitorExercise;

public class RedditClient : IDisposable
{
    private string ClientId { get; init; }
    private string ClientSecret { get; init; }
    
    private RestClient RedditRestClient { get; set; }

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

    public async Task<ListingData<Post>> GetSubredditPosts(string subreddit, string? after = null, string? before = null, int? count = null)
    {
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
            Console.WriteLine(response.Content);
            Console.WriteLine(response.ResponseStatus);
            Console.WriteLine(response.StatusCode);
            Console.WriteLine(response.ErrorMessage);
            Console.WriteLine(response.ErrorException);
            
            throw new Exception("Failed to get subreddit posts");
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
        var request = new RestRequest("api/v1/access_token");
        request.AddParameter("grant_type", "client_credentials");

        var tokenResponse = await RedditRestClient.ExecutePostAsync<AccessToken>(request);

        if (!tokenResponse.IsSuccessful || tokenResponse.Data == null)
        {
            throw new Exception("Failed to refresh access token");
        }
        
        AccessTokenExpiration = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.Data.ExpiresIn);
        AccessToken = tokenResponse.Data.Token;
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
            throw new Exception("Could not get access token");
        }
    }
}