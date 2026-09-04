namespace Scrapefy.Clients;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Scrapefy.Responses;

public sealed class SpotifyClient
{
    private readonly HttpClient _httpClient;
    private readonly Uri _BaseAddress = new("https://api.spotify.com/v1");
    private readonly string clientId = "";
    private readonly string clientSecret = "";
    private AccessTokenResponse? _accessTokenResponse;
    private bool _expired = true;
    private DateTime _tokenExpirationTime;


    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public SpotifyClient()
    {

        _httpClient = new HttpClient
        {
            // Configure default headers for Spotify API calls
            BaseAddress = _BaseAddress
        };


        RenewAccessToken();

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _accessTokenResponse!.AccessToken);
    }


    public async Task<PaginatedResponse<SpotifyTrack>> GetPlaylistItemsAsync(string id, Dictionary<string, string>? q = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        return await Request<PaginatedResponse<SpotifyTrack>>($"playlists/{id}/items", HttpMethod.Get, ct);
    }
    public async Task<PlayListResponse> GetPlaylistAsync(string id, CancellationToken ct = default)
    {
        // no query params for now.
        return await Request<PlayListResponse>($"playlists/{id}", HttpMethod.Get, ct);
    }

    private async Task<T> Request<T>(string endpoint, HttpMethod verb, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(endpoint);
        ArgumentNullException.ThrowIfNull(verb);
        RenewAccessToken();
        // TODO: Add support for multiple HTTP verbs and query parameters if needed in the future.
        // For now, we only support GET requests without query parameters.
        // The method's name implies that it can handle different HTTP verbs, but currently, it only supports GET requests.
        var response = await _httpClient.GetAsync($"{_BaseAddress}/{endpoint}", ct);
        var data = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<T>(data, DefaultJsonOptions);

        return result ?? throw new InvalidOperationException("Failed to deserialize response.");
    }

    private async Task<AccessTokenResponse> GetAccessTokenAsync(CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));
        request.Content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        ]);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<AccessTokenResponse>(content, DefaultJsonOptions) ?? throw new InvalidOperationException("Failed to deserialize access token response.");
    }

    private void RenewAccessToken()
    {
        if (DateTime.Now > _tokenExpirationTime)
        {

            _accessTokenResponse =
            GetAccessTokenAsync()
            .GetAwaiter()
            .GetResult();

            _tokenExpirationTime = DateTime.Now.AddSeconds(_accessTokenResponse.ExpiresIn);
            _expired = false;
        }
    }

}
