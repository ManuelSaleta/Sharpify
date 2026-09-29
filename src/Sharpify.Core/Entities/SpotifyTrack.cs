namespace Sharpify.Core.Entities;

public class SpotifyTrack
{
    public string Id { get; set; } = string.Empty;
    public string? AlbumId { get; set; }
    public SpotifyAlbum? Album { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DiscNumber { get; set; }
    public int TrackNumber { get; set; }
    public int DurationMs { get; set; }
    public bool Explicit { get; set; }
    public string? Isrc { get; set; }
    public string? Uri { get; set; }

    public List<PlaylistTrack> PlaylistTracks { get; set; } = [];
    public List<SpotifyPlaylist> Playlists { get; set; } = [];
    public List<TrackArtist> TrackArtists { get; set; } = [];
    public List<SpotifyArtist> Artists { get; set; } = [];
    public YouTubeMatch? YouTubeMatch { get; set; }
    public List<DownloadJob> DownloadJobs { get; set; } = [];
}
