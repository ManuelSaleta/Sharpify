namespace Sharpify.Core.Authentication;

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Sharpify.Core.Clients;

public class SpotifyAuthService : ISpotifyAuthService
{
    private readonly HttpClient _httpClient;
    private readonly SpotifyClientOptions _options;

    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SpotifyAuthService(HttpClient httpClient, IOptions<SpotifyClientOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Uri BuildAuthorizationUri(string? state = null, IEnumerable<string>? scopes = null)
    {
        var scopeList = scopes ?? _options.Scopes;
        var scopeString = string.Join(" ", scopeList);

        var queryParams = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["response_type"] = "code",
            ["redirect_uri"] = _options.RedirectUri,
            ["scope"] = scopeString
        };

        if (!string.IsNullOrWhiteSpace(state))
        {
            queryParams["state"] = state;
        }

        var queryString = string.Join('&', queryParams.Select(kvp =>
            $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

        return new Uri($"{_options.AuthorizeUrl.TrimEnd('/')}?{queryString}");
    }

    public async Task<SpotifyToken> GetClientCredentialsTokenAsync(CancellationToken ct = default)
    {
        return await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials"
        }, ct);
    }

    public async Task<SpotifyToken> ExchangeCodeForTokenAsync(string code, string? redirectUri = null, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = redirectUri ?? _options.RedirectUri
        }, ct);
    }

    public async Task<SpotifyToken> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken);

        var token = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = _options.ClientId
        }, ct);

        // If Spotify does not return a new refresh token, retain the existing one
        if (string.IsNullOrWhiteSpace(token.RefreshToken))
        {
            return token with { RefreshToken = refreshToken };
        }

        return token;
    }

    private async Task<SpotifyToken> RequestTokenAsync(IEnumerable<KeyValuePair<string, string>> formFields, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.UserAccountUrl);
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.ClientId}:{_options.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        request.Content = new FormUrlEncodedContent(formFields);

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Spotify authentication failed with status {response.StatusCode} ({response.ReasonPhrase}): {errorBody}");
        }

        var content = await response.Content.ReadAsStringAsync(ct);
        var token = JsonSerializer.Deserialize<SpotifyToken>(content, DefaultJsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize Spotify token response.");

        return token with { CreatedAt = DateTime.UtcNow };
    }
}
