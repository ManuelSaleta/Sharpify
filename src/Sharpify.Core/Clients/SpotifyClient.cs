namespace Sharpify.Core.Clients;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Sharpify.Core.Authentication;
using Sharpify.Core.Requests;
using Sharpify.Core.Responses;

public interface ISpotifyClient
{
    /// <summary>
    /// Builds generic SpotifyRequest, should work for any request type for spotify
    /// </summary>
    /// <typeparam name="T"> the typeof Response</typeparam>
    /// <param name="request">typeof SpotifyRequest</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns></returns>
    Task<T> Request<T>(SpotifyRequest request, CancellationToken ct = default);

    /// <summary>
    /// Retrieves items (tracks/episodes) from a playlist by playlist ID.
    /// </summary>
    Task<PaginatedResponse<SavedItem>> GetPlaylistItemsAsync(string playlistId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves items from a playlist using a SpotifyRequest.
    /// </summary>
    Task<PaginatedResponse<SavedItem>> GetPlaylistItemsAsync(SpotifyRequest r, CancellationToken ct = default);

    /// <summary>
    /// Retrieves playlist details by playlist ID.
    /// </summary>
    Task<PlayListResponse> GetPlaylistAsync(string playlistId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the current user's playlists.
    /// </summary>
    Task<PaginatedResponse<PlayListResponse>> GetUserPlaylistsAsync(CancellationToken ct = default);
}

public sealed class SpotifyClient : ISpotifyClient
{
    private readonly HttpClient _httpClient;
    private readonly SpotifyClientOptions _options;
    private readonly ISpotifyAuthService _authService;
    private readonly ISpotifyTokenStore _tokenStore;

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public SpotifyClient(
        HttpClient httpClient,
        IOptions<SpotifyClientOptions> options,
        ISpotifyAuthService? authService = null,
        ISpotifyTokenStore? tokenStore = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _authService = authService ?? new SpotifyAuthService(httpClient, options);
        _tokenStore = tokenStore ?? new InMemorySpotifyTokenStore();

        if (_httpClient.BaseAddress is null && !string.IsNullOrWhiteSpace(_options.ClientBaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.ClientBaseUrl.TrimEnd('/') + "/");
        }
    }

    public async Task<PaginatedResponse<SavedItem>> GetPlaylistItemsAsync(string playlistId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playlistId);
        return await GetPlaylistItemsAsync(new SpotifyRequest($"playlists/{playlistId}/items"), ct);
    }

    public async Task<PaginatedResponse<SavedItem>> GetPlaylistItemsAsync(SpotifyRequest r, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(r);
        return await Request<PaginatedResponse<SavedItem>>(r, ct);
    }

    public async Task<PlayListResponse> GetPlaylistAsync(string playlistId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(playlistId);
        return await Request<PlayListResponse>(new SpotifyRequest($"playlists/{playlistId}"), ct);
    }

    public async Task<PaginatedResponse<PlayListResponse>> GetUserPlaylistsAsync(CancellationToken ct = default)
    {
        return await Request<PaginatedResponse<PlayListResponse>>(new SpotifyRequest("me/playlists"), ct);
    }


    public async Task<T> Request<T>(SpotifyRequest r, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(r);
        ArgumentNullException.ThrowIfNull(r.Uri);
        ArgumentNullException.ThrowIfNull(r.Method);

        var token = await EnsureAccessTokenAsync(ct);

        var url = r.ToString().TrimStart('/');
        using var httpRequest = new HttpRequestMessage(r.Method, url);
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

        if (r.Body is not null)
        {
            httpRequest.Content = JsonContent.Create(r.Body, options: DefaultJsonOptions);
        }

        var response = await _httpClient.SendAsync(httpRequest, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Spotify API request failed. Status: {response.StatusCode} ({response.ReasonPhrase}) Request: {url}, Error: {errorBody}");
        }

        var data = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<T>(data, DefaultJsonOptions);

        return result ?? throw new InvalidOperationException("Failed to deserialize response.");
    }

    private async Task<SpotifyToken> EnsureAccessTokenAsync(CancellationToken ct = default)
    {
        var token = await _tokenStore.GetTokenAsync(ct);
        if (token is not null && !token.IsExpired)
        {
            return token;
        }

        if (!string.IsNullOrWhiteSpace(token?.RefreshToken))
        {
            try
            {
                var refreshedToken = await _authService.RefreshTokenAsync(token.RefreshToken, ct);
                await _tokenStore.SaveTokenAsync(refreshedToken, ct);
                return refreshedToken;
            }
            catch
            {
                // If refresh fails, fall back to client credentials
            }
        }

        var clientToken = await _authService.GetClientCredentialsTokenAsync(ct);
        await _tokenStore.SaveTokenAsync(clientToken, ct);
        return clientToken;
    }
}
