namespace Sharpify.Core.Entities;

public class SpotifyArtist
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Uri { get; set; }
    public string? Href { get; set; }

    public List<TrackArtist> TrackArtists { get; set; } = [];
    public List<SpotifyTrack> Tracks { get; set; } = [];
    public List<AlbumArtist> AlbumArtists { get; set; } = [];
    public List<SpotifyAlbum> Albums { get; set; } = [];
}
