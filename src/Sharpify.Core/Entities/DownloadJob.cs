namespace Sharpify.Core.Entities;

public class DownloadJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TrackId { get; set; } = string.Empty;
    public SpotifyTrack Track { get; set; } = null!;

    public Guid? YouTubeMatchId { get; set; }
    public YouTubeMatch? YouTubeMatch { get; set; }

    public string Status { get; set; } = "Pending";
    public string? OutputFilePath { get; set; }
    public string? Format { get; set; }
    public long? FileSizeBytes { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}
