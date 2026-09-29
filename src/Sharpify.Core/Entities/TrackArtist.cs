namespace Sharpify.Core.Entities;

public class TrackArtist
{
    public string TrackId { get; set; } = string.Empty;
    public SpotifyTrack Track { get; set; } = null!;

    public string ArtistId { get; set; } = string.Empty;
    public SpotifyArtist Artist { get; set; } = null!;
}
