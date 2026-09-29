namespace Sharpify.Core.Entities;

public class AlbumArtist
{
    public string AlbumId { get; set; } = string.Empty;
    public SpotifyAlbum Album { get; set; } = null!;

    public string ArtistId { get; set; } = string.Empty;
    public SpotifyArtist Artist { get; set; } = null!;
}
