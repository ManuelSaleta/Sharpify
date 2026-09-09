using System.ComponentModel.DataAnnotations;

namespace Sharpify.Lib;

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
}
