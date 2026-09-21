namespace Sharpify.Core.Authentication;

using System.Text.Json.Serialization;

public sealed record SpotifyToken
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; init; }

    [JsonPropertyName("scope")]
    public string? Scope { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [JsonIgnore]
    public DateTime ExpiresAt => CreatedAt.AddSeconds(ExpiresIn);

    [JsonIgnore]
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddSeconds(-30);
}
