namespace Sharpify.Core.Entities;

public class SpotifyPlaylist
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Href { get; set; }
    public string? Uri { get; set; }
    public int TotalTracks { get; set; }
    public string? SnapshotId { get; set; }
    public DateTimeOffset? LastSyncedAt { get; set; }

    public List<PlaylistTrack> PlaylistTracks { get; set; } = [];
    public List<SpotifyTrack> Tracks { get; set; } = [];
}
