using System.ComponentModel.DataAnnotations;

namespace Sharpify.Core.Clients;

public sealed class SpotifyClientOptions
{
    /// <summary>
    /// Options section
    /// </summary>
    public const string SectionName = "SpotifyClient";

    /// <summary>
    /// API BaseUrl -
    /// </summary>
    [Required]
    public required string ClientBaseUrl { get; init; }

    /// <summary>
    /// URL for requesting teh init. authorization request
    /// </summary>
    [Required]
    public required string UserAccountUrl { get; init; }

    /// <summary>
    /// DEV Acct. Client Id.
    /// </summary>
    [Required]
    public required string ClientId { get; init; }


    /// <summary>
    /// The requested Client Secret.
    /// </summary>
    [Required]
    public required string ClientSecret { get; init; }

    /// <summary>
    /// Spotify OAuth authorization endpoint URL.
    /// </summary>
    public string AuthorizeUrl { get; init; } = "https://accounts.spotify.com/authorize";

    /// <summary>
    /// Redirect URI configured in the Spotify Developer Dashboard.
    /// </summary>
    public string RedirectUri { get; init; } = "http://127.0.0.1:5000/callback";

    /// <summary>
    /// OAuth scopes requested during user authorization.
    /// </summary>
    public IReadOnlyList<string> Scopes { get; init; } = ["playlist-read-private", "playlist-read-collaborative"];
}

