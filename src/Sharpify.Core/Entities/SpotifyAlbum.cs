namespace Sharpify.Core.Entities;

public class SpotifyAlbum
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AlbumType { get; set; }
    public int TotalTracks { get; set; }
    public string? ReleaseDate { get; set; }
    public string? ImageUrl { get; set; }
    public string? Uri { get; set; }

    public List<SpotifyTrack> Tracks { get; set; } = [];
    public List<AlbumArtist> AlbumArtists { get; set; } = [];
    public List<SpotifyArtist> Artists { get; set; } = [];
}
