namespace Sharpify.Core.Authentication;

public interface ISpotifyAuthService
{
    Uri BuildAuthorizationUri(string? state = null, IEnumerable<string>? scopes = null);
    Task<SpotifyToken> GetClientCredentialsTokenAsync(CancellationToken ct = default);
    Task<SpotifyToken> ExchangeCodeForTokenAsync(string code, string? redirectUri = null, CancellationToken ct = default);
    Task<SpotifyToken> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
}
