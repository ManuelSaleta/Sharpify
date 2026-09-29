namespace Sharpify.Core.Entities;

public class YouTubeMatch
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TrackId { get; set; } = string.Empty;
    public SpotifyTrack Track { get; set; } = null!;

    public string YouTubeUrl { get; set; } = string.Empty;
    public string? VideoId { get; set; }
    public string? Title { get; set; }
    public string? ChannelTitle { get; set; }
    public int DurationMs { get; set; }
    public double ConfidenceScore { get; set; }
    public string MatchStatus { get; set; } = string.Empty;
    public DateTimeOffset MatchedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<DownloadJob> DownloadJobs { get; set; } = [];
}
