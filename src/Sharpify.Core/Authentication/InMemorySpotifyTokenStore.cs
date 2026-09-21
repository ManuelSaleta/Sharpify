namespace Sharpify.Core.Authentication;

public class InMemorySpotifyTokenStore : ISpotifyTokenStore
{
    private SpotifyToken? _token;

    public InMemorySpotifyTokenStore(SpotifyToken? initialToken = null)
    {
        _token = initialToken;
    }

    public Task<SpotifyToken?> GetTokenAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_token);
    }

    public Task SaveTokenAsync(SpotifyToken token, CancellationToken ct = default)
    {
        _token = token;
        return Task.CompletedTask;
    }

    public Task ClearTokenAsync(CancellationToken ct = default)
    {
        _token = null;
        return Task.CompletedTask;
    }
}
