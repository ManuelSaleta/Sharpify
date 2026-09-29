namespace Sharpify.Tests.Unit;

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Sharpify.Core.Authentication;
using Sharpify.Core.Clients;

public class SpotifyClientTests
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
            ClientSecret = "test_client_secret"
        });
    }

    [Fact]
    public async Task GetPlaylistItemsAsync_ShouldSendBearerTokenAndDeserializeSavedItems()
    {
        var options = CreateOptions();
        var token = new SpotifyToken
        {
            AccessToken = "user_access_token_123",
            ExpiresIn = 3600,
            CreatedAt = DateTime.UtcNow
        };
        var tokenStore = new InMemorySpotifyTokenStore(token);

        var playlistResponseJson = """
        {
            "href": "https://api.spotify.com/v1/playlists/test_playlist/tracks",
            "limit": 100,
            "next": null,
            "offset": 0,
            "previous": null,
            "total": 1,
            "items": [
                {
                    "added_at": "2024-01-01T00:00:00Z",
                    "is_local": false,
                    "track": {
                        "id": "track_123",
                        "name": "Sample Song",
                        "artists": [
                            {
                                "id": "artist_123",
                                "name": "Sample Artist",
                                "type": "artist",
                                "uri": "spotify:artist:artist_123",
                                "external_urls": { "spotify": "https://spotify.com" },
                                "href": "https://api.spotify.com"
                            }
                        ],
                        "album": {
                            "id": "album_123",
                            "name": "Sample Album",
                            "album_type": "album",
                            "total_tracks": 10,
                            "available_markets": [],
                            "external_urls": { "spotify": "https://spotify.com" },
                            "href": "https://api.spotify.com",
                            "images": [],
                            "release_date": "2024-01-01",
                            "release_date_precision": "day",
                            "type": "album",
                            "uri": "spotify:album:album_123",
                            "artists": []
                        },
                        "available_markets": [],
                        "disc_number": 1,
                        "duration_ms": 180000,
                        "explicit": false,
                        "external_ids": {},
                        "external_urls": { "spotify": "https://spotify.com" },
                        "href": "https://api.spotify.com",
                        "is_playable": true,
                        "popularity": 50,
                        "track_number": 1,
                        "type": "track",
                        "uri": "spotify:track:track_123"
                    }
                }
            ]
        }
        """;

        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(playlistResponseJson, System.Text.Encoding.UTF8, "application/json")
        }, req => { capturedRequest = req; return Task.CompletedTask; });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.spotify.com/v1/")
        };

        var client = new SpotifyClient(httpClient, options, tokenStore: tokenStore);

        var result = await client.GetPlaylistItemsAsync("test_playlist");

        result.Should().NotBeNull();
        result.Total.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Track.Should().NotBeNull();
        result.Items[0].Track!.Name.Should().Be("Sample Song");
        result.Items[0].Track!.Artists[0].Name.Should().Be("Sample Artist");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be("https://api.spotify.com/v1/playlists/test_playlist/items");
        capturedRequest.Headers.Authorization.Should().NotBeNull();

        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        capturedRequest.Headers.Authorization.Parameter.Should().Be("user_access_token_123");
    }

    [Fact]
    public async Task Request_ShouldAutoRefreshToken_WhenExpired()
    {
        var options = CreateOptions();
        var expiredToken = new SpotifyToken
        {
            AccessToken = "old_token",
            RefreshToken = "test_refresh_token",
            ExpiresIn = 3600,
            CreatedAt = DateTime.UtcNow.AddHours(-2)
        };
        var tokenStore = new InMemorySpotifyTokenStore(expiredToken);

        var refreshedToken = new SpotifyToken
        {
            AccessToken = "refreshed_token_456",
            RefreshToken = "test_refresh_token",
            ExpiresIn = 3600,
            CreatedAt = DateTime.UtcNow
        };

        var authServiceMock = new FakeAuthService(refreshedToken);

        HttpRequestMessage? capturedRequest = null;
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        }, req => { capturedRequest = req; return Task.CompletedTask; });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.spotify.com/v1/")
        };

        var client = new SpotifyClient(httpClient, options, authService: authServiceMock, tokenStore: tokenStore);

        await client.GetPlaylistItemsAsync("test_playlist");

        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization!.Parameter.Should().Be("refreshed_token_456");

        // Verify token store has updated refreshed token
        var stored = await tokenStore.GetTokenAsync();
        stored!.AccessToken.Should().Be("refreshed_token_456");
    }

    private class FakeAuthService(SpotifyToken refreshedToken) : ISpotifyAuthService
    {
        public Uri BuildAuthorizationUri(string? state = null, IEnumerable<string>? scopes = null) => new("https://example.com");
        public Task<SpotifyToken> GetClientCredentialsTokenAsync(CancellationToken ct = default) => Task.FromResult(refreshedToken);
        public Task<SpotifyToken> ExchangeCodeForTokenAsync(string code, string? redirectUri = null, CancellationToken ct = default) => Task.FromResult(refreshedToken);
        public Task<SpotifyToken> RefreshTokenAsync(string refreshToken, CancellationToken ct = default) => Task.FromResult(refreshedToken);
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
