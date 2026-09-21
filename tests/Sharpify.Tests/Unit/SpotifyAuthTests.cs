namespace Sharpify.Tests.Unit;

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Sharpify.Core.Authentication;
using Sharpify.Core.Clients;

public class SpotifyAuthTests
{
    private static IOptions<SpotifyClientOptions> CreateOptions()
    {
        return Options.Create(new SpotifyClientOptions
        {
            ClientBaseUrl = "https://api.spotify.com/v1",
            UserAccountUrl = "https://accounts.spotify.com/api/token",
            AuthorizeUrl = "https://accounts.spotify.com/authorize",
            RedirectUri = "http://127.0.0.1:5000/callback",
            ClientId = "test_client_id",
            ClientSecret = "test_client_secret",
            Scopes = ["playlist-read-private", "playlist-read-collaborative"]
        });
    }

    [Fact]
    public void SpotifyToken_IsExpired_ShouldReturnTrue_WhenPastExpiration()
    {
        var token = new SpotifyToken
        {
            AccessToken = "expired_token",
            ExpiresIn = 3600,
            CreatedAt = DateTime.UtcNow.AddSeconds(-3601)
        };

        token.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void SpotifyToken_IsExpired_ShouldReturnFalse_WhenWithinValidityPeriod()
    {
        var token = new SpotifyToken
        {
            AccessToken = "valid_token",
            ExpiresIn = 3600,
            CreatedAt = DateTime.UtcNow
        };

        token.IsExpired.Should().BeFalse();
    }

    [Fact]
    public async Task InMemoryTokenStore_ShouldStoreAndRetrieveToken()
    {
        var store = new InMemorySpotifyTokenStore();
        (await store.GetTokenAsync()).Should().BeNull();

        var token = new SpotifyToken { AccessToken = "abc", ExpiresIn = 3600 };
        await store.SaveTokenAsync(token);

        var retrieved = await store.GetTokenAsync();
        retrieved.Should().NotBeNull();
        retrieved!.AccessToken.Should().Be("abc");

        await store.ClearTokenAsync();
        (await store.GetTokenAsync()).Should().BeNull();
    }

    [Fact]
    public async Task FileSpotifyTokenStore_ShouldPersistAndLoadToken()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"sharpify_test_{Guid.NewGuid():N}.json");
        try
        {
            var store = new FileSpotifyTokenStore(tempFile);
            var token = new SpotifyToken { AccessToken = "persisted_token", RefreshToken = "ref_123", ExpiresIn = 3600 };
            await store.SaveTokenAsync(token);

            var retrieved = await store.GetTokenAsync();
            retrieved.Should().NotBeNull();
            retrieved!.AccessToken.Should().Be("persisted_token");
            retrieved.RefreshToken.Should().Be("ref_123");

            await store.ClearTokenAsync();
            (await store.GetTokenAsync()).Should().BeNull();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void SpotifyAuthService_BuildAuthorizationUri_ShouldIncludeExpectedParameters()
    {
        var options = CreateOptions();
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var authService = new SpotifyAuthService(httpClient, options);

        var uri = authService.BuildAuthorizationUri(state: "test_state");

        uri.ToString().Should().StartWith("https://accounts.spotify.com/authorize?");
        uri.Query.Should().Contain("client_id=test_client_id");
        uri.Query.Should().Contain("response_type=code");
        uri.Query.Should().Contain("state=test_state");
        uri.Query.Should().Contain("redirect_uri=" + Uri.EscapeDataString("http://127.0.0.1:5000/callback"));
        uri.Query.Should().Contain("scope=" + Uri.EscapeDataString("playlist-read-private playlist-read-collaborative"));
    }

    [Fact]
    public async Task SpotifyAuthService_ExchangeCodeForTokenAsync_ShouldPostCorrectParameters()
    {
        var options = CreateOptions();
        HttpRequestMessage? capturedRequest = null;

        var jsonResponse = """
        {
            "access_token": "new_access_token",
            "token_type": "Bearer",
            "expires_in": 3600,
            "refresh_token": "new_refresh_token",
            "scope": "playlist-read-private"
        }
        """;

        string? capturedBody = null;
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        }, async req =>
        {
            capturedRequest = req;
            if (req.Content != null) capturedBody = await req.Content.ReadAsStringAsync();
        });

        var httpClient = new HttpClient(handler);
        var authService = new SpotifyAuthService(httpClient, options);

        var token = await authService.ExchangeCodeForTokenAsync("auth_code_xyz");

        token.Should().NotBeNull();
        token.AccessToken.Should().Be("new_access_token");
        token.RefreshToken.Should().Be("new_refresh_token");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Method.Should().Be(HttpMethod.Post);
        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("grant_type=authorization_code");
        capturedBody.Should().Contain("code=auth_code_xyz");
    }

    [Fact]
    public async Task SpotifyAuthService_RefreshTokenAsync_ShouldPostRefreshToken()
    {
        var options = CreateOptions();
        HttpRequestMessage? capturedRequest = null;
        string? capturedBody = null;

        var jsonResponse = """
        {
            "access_token": "refreshed_access_token",
            "token_type": "Bearer",
            "expires_in": 3600
        }
        """;

        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(jsonResponse, System.Text.Encoding.UTF8, "application/json")
        }, async req =>
        {
            capturedRequest = req;
            if (req.Content != null) capturedBody = await req.Content.ReadAsStringAsync();
        });

        var httpClient = new HttpClient(handler);
        var authService = new SpotifyAuthService(httpClient, options);

        var token = await authService.RefreshTokenAsync("existing_refresh_token");

        token.AccessToken.Should().Be("refreshed_access_token");
        token.RefreshToken.Should().Be("existing_refresh_token"); // Retained from input

        capturedBody.Should().NotBeNull();
        capturedBody.Should().Contain("grant_type=refresh_token");
        capturedBody.Should().Contain("refresh_token=existing_refresh_token");
    }


    private class MockHttpMessageHandler(HttpResponseMessage response, Func<HttpRequestMessage, Task>? callback = null) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (callback != null)
            {
                await callback(request);
            }
            return response;
        }
    }
}

