namespace Sharpify.Core.Authentication;

using System.Text.Json;

public class FileSpotifyTokenStore : ISpotifyTokenStore
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public FileSpotifyTokenStore(string? filePath = null)
    {
        if (!string.IsNullOrWhiteSpace(filePath))
        {
            _filePath = filePath;
        }
        else
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appDir = Path.Combine(homeDir, ".sharpify");
            _filePath = Path.Combine(appDir, "token.json");
        }
    }

    public async Task<SpotifyToken?> GetTokenAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, ct);
            return JsonSerializer.Deserialize<SpotifyToken>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveTokenAsync(SpotifyToken token, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(token, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json, ct);
    }

    public Task ClearTokenAsync(CancellationToken ct = default)
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
        return Task.CompletedTask;
    }
}
