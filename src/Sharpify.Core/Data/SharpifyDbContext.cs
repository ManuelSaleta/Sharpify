using Microsoft.EntityFrameworkCore;
using Sharpify.Core.Entities;

namespace Sharpify.Core.Data;

public class SharpifyDbContext : DbContext
{
    public SharpifyDbContext(DbContextOptions<SharpifyDbContext> options)
        : base(options)
    {
    }

    public DbSet<SpotifyPlaylist> Playlists => Set<SpotifyPlaylist>();
    public DbSet<SpotifyAlbum> Albums => Set<SpotifyAlbum>();
    public DbSet<SpotifyArtist> Artists => Set<SpotifyArtist>();
    public DbSet<SpotifyTrack> Tracks => Set<SpotifyTrack>();
    public DbSet<PlaylistTrack> PlaylistTracks => Set<PlaylistTrack>();
    public DbSet<TrackArtist> TrackArtists => Set<TrackArtist>();
    public DbSet<AlbumArtist> AlbumArtists => Set<AlbumArtist>();
    public DbSet<YouTubeMatch> YouTubeMatches => Set<YouTubeMatch>();
    public DbSet<DownloadJob> DownloadJobs => Set<DownloadJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SpotifyPlaylist
        modelBuilder.Entity<SpotifyPlaylist>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(500);
            entity.Property(p => p.SnapshotId).HasMaxLength(100);
        });

        // SpotifyAlbum
        modelBuilder.Entity<SpotifyAlbum>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).IsRequired().HasMaxLength(500);
            entity.Property(a => a.AlbumType).HasMaxLength(50);
            entity.Property(a => a.ReleaseDate).HasMaxLength(50);
        });

        // SpotifyArtist
        modelBuilder.Entity<SpotifyArtist>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Name).IsRequired().HasMaxLength(500);
        });

        // SpotifyTrack
        modelBuilder.Entity<SpotifyTrack>(entity =>
        {
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Name).IsRequired().HasMaxLength(500);
            entity.Property(t => t.Isrc).HasMaxLength(50);

            entity.HasOne(t => t.Album)
                .WithMany(a => a.Tracks)
                .HasForeignKey(t => t.AlbumId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(t => t.YouTubeMatch)
                .WithOne(y => y.Track)
                .HasForeignKey<YouTubeMatch>(y => y.TrackId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(t => t.DownloadJobs)
                .WithOne(d => d.Track)
                .HasForeignKey(d => d.TrackId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PlaylistTrack (Junction Table)
        modelBuilder.Entity<PlaylistTrack>(entity =>
        {
            entity.HasKey(pt => new { pt.PlaylistId, pt.TrackId });

            entity.HasOne(pt => pt.Playlist)
                .WithMany(p => p.PlaylistTracks)
                .HasForeignKey(pt => pt.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pt => pt.Track)
                .WithMany(t => t.PlaylistTracks)
                .HasForeignKey(pt => pt.TrackId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(pt => pt.Position);
        });

        // TrackArtist (Junction Table)
        modelBuilder.Entity<TrackArtist>(entity =>
        {
            entity.HasKey(ta => new { ta.TrackId, ta.ArtistId });

            entity.HasOne(ta => ta.Track)
                .WithMany(t => t.TrackArtists)
                .HasForeignKey(ta => ta.TrackId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ta => ta.Artist)
                .WithMany(a => a.TrackArtists)
                .HasForeignKey(ta => ta.ArtistId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AlbumArtist (Junction Table)
        modelBuilder.Entity<AlbumArtist>(entity =>
        {
            entity.HasKey(aa => new { aa.AlbumId, aa.ArtistId });

            entity.HasOne(aa => aa.Album)
                .WithMany(a => a.AlbumArtists)
                .HasForeignKey(aa => aa.AlbumId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(aa => aa.Artist)
                .WithMany(a => a.AlbumArtists)
                .HasForeignKey(aa => aa.ArtistId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Many-to-many relationship mappings with junction entities
        modelBuilder.Entity<SpotifyPlaylist>()
            .HasMany(p => p.Tracks)
            .WithMany(t => t.Playlists)
            .UsingEntity<PlaylistTrack>(
                j => j.HasOne(pt => pt.Track).WithMany(t => t.PlaylistTracks).HasForeignKey(pt => pt.TrackId),
                j => j.HasOne(pt => pt.Playlist).WithMany(p => p.PlaylistTracks).HasForeignKey(pt => pt.PlaylistId)
            );

        modelBuilder.Entity<SpotifyTrack>()
            .HasMany(t => t.Artists)
            .WithMany(a => a.Tracks)
            .UsingEntity<TrackArtist>(
                j => j.HasOne(ta => ta.Artist).WithMany(a => a.TrackArtists).HasForeignKey(ta => ta.ArtistId),
                j => j.HasOne(ta => ta.Track).WithMany(t => t.TrackArtists).HasForeignKey(ta => ta.TrackId)
            );

        modelBuilder.Entity<SpotifyAlbum>()
            .HasMany(a => a.Artists)
            .WithMany(a => a.Albums)
            .UsingEntity<AlbumArtist>(
                j => j.HasOne(aa => aa.Artist).WithMany(a => a.AlbumArtists).HasForeignKey(aa => aa.ArtistId),
                j => j.HasOne(aa => aa.Album).WithMany(a => a.AlbumArtists).HasForeignKey(aa => aa.AlbumId)
            );

        // YouTubeMatch
        modelBuilder.Entity<YouTubeMatch>(entity =>
        {
            entity.HasKey(y => y.Id);
            entity.HasIndex(y => y.TrackId).IsUnique();
            entity.Property(y => y.YouTubeUrl).IsRequired().HasMaxLength(1000);
            entity.Property(y => y.VideoId).HasMaxLength(50);
            entity.Property(y => y.Title).HasMaxLength(500);
            entity.Property(y => y.ChannelTitle).HasMaxLength(255);
            entity.Property(y => y.MatchStatus).IsRequired().HasMaxLength(50);
        });

        // DownloadJob
        modelBuilder.Entity<DownloadJob>(entity =>
        {
            entity.HasKey(d => d.Id);
            entity.Property(d => d.Status).IsRequired().HasMaxLength(50);
            entity.Property(d => d.Format).HasMaxLength(20);
            entity.Property(d => d.OutputFilePath).HasMaxLength(1000);
            entity.HasIndex(d => d.Status);

            entity.HasOne(d => d.YouTubeMatch)
                .WithMany(y => y.DownloadJobs)
                .HasForeignKey(d => d.YouTubeMatchId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
