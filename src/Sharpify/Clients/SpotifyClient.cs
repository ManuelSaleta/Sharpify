namespace Sharpify.Clients;

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Sharpify.Lib;
using Sharpify.Lib.Responses;

public sealed class SpotifyClient
{
    private readonly HttpClient _httpClient;
    private readonly SpotifyClientOptions _options;
    public sealed record AccessTokenResponse(string AccessToken, string TokenType, int ExpiresIn);
    public sealed record PlayListResponse(string Name, string Description, string Href, string Id, string Uri);
    private AccessTokenResponse? _accessTokenResponse;
    private DateTime _tokenExpirationTime;

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public SpotifyClient(HttpClient httpClient, IOptions<SpotifyClientOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (_httpClient.BaseAddress is null && !string.IsNullOrWhiteSpace(_options.ClientBaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.ClientBaseUrl.TrimEnd('/') + "/");
        }
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
        await EnsureAccessTokenAsync(ct);

        // TODO: Add support for multiple HTTP verbs and query parameters if needed in the future.
        // For now, we only support GET requests without query parameters.
        // The method's name implies that it can handle different HTTP verbs, but currently, it only supports GET requests.
        var response = await _httpClient.GetAsync($"{_BaseAddress}/{endpoint}", ct);
        var data = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<T>(data, DefaultJsonOptions);
        var url = _httpClient.BaseAddress is null
            ? $"{_options.ClientBaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}"
            : endpoint.TrimStart('/');

        var response = await _httpClient.GetStringAsync(url, ct);
        var result = JsonSerializer.Deserialize<T>(response, DefaultJsonOptions);

        return result ?? throw new InvalidOperationException("Failed to deserialize response.");
    }

    private async Task<AccessTokenResponse> GetAccessTokenAsync(CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _options.UserAccountUrl);
        var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        ]);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize<AccessTokenResponse>(content, DefaultJsonOptions) ?? throw new InvalidOperationException("Failed to deserialize access token response.");
    }

    private async Task EnsureAccessTokenAsync(CancellationToken ct = default)
    {
        if (_accessTokenResponse is null || DateTime.UtcNow >= _tokenExpirationTime)
        {
            _accessTokenResponse = await GetAccessTokenAsync(ct);
            _tokenExpirationTime = DateTime.UtcNow.AddSeconds(_accessTokenResponse.ExpiresIn);
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _accessTokenResponse.AccessToken);
        }
    }

    private void RenewAccessToken()
    {
        EnsureAccessTokenAsync().GetAwaiter().GetResult();
    }

}
