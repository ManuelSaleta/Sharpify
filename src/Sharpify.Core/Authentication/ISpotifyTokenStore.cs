namespace Sharpify.Core.Authentication;

public interface ISpotifyTokenStore
{
    Task<SpotifyToken?> GetTokenAsync(CancellationToken ct = default);
    Task SaveTokenAsync(SpotifyToken token, CancellationToken ct = default);
    Task ClearTokenAsync(CancellationToken ct = default);
}
