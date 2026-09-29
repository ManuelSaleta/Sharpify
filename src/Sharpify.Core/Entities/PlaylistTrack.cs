namespace Sharpify.Core.Entities;

public class PlaylistTrack
{
    public string PlaylistId { get; set; } = string.Empty;
    public SpotifyPlaylist Playlist { get; set; } = null!;

    public string TrackId { get; set; } = string.Empty;
    public SpotifyTrack Track { get; set; } = null!;

    public int Position { get; set; }
    public DateTimeOffset? AddedAt { get; set; }
}
