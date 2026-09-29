using System.Data.Common;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Sharpify.Core.Data;
using Sharpify.Core.Data.Interceptors;
using Sharpify.Core.Entities;
using Xunit;

namespace Sharpify.Tests.Unit;

public class SharpifyDbContextTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _connectionString;

    public SharpifyDbContextTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"sharpify_test_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_testDbPath};Cache=Shared";
    }

    private SharpifyDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<SharpifyDbContext>();
        optionsBuilder.UseSqlite(_connectionString)
                      .AddInterceptors(new SqlitePragmaConnectionInterceptor());

        return new SharpifyDbContext(optionsBuilder.Options);
    }

    public void Dispose()
    {
        // Clear all connection pools before deleting test DB
        SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
            var shm = $"{_testDbPath}-shm";
            if (File.Exists(shm)) File.Delete(shm);
            var wal = $"{_testDbPath}-wal";
            if (File.Exists(wal)) File.Delete(wal);
        }
        catch
        {
            // Best effort cleanup in tests
        }
    }

    [Fact]
    public async Task MigrateAsync_AppliesAllPendingMigrationsSuccessfully()
    {
        // Arrange
        await using var context = CreateContext();

        // Act
        await context.Database.MigrateAsync();

        // Assert
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Should().Contain(m => m.Contains("InitialCreate"));
    }

    [Fact]
    public async Task SqlitePragmas_AreConfiguredUponConnectionCreation()
    {
        // Arrange
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();

        // Act - Verify foreign_keys
        await using var fkCommand = connection.CreateCommand();
        fkCommand.CommandText = "PRAGMA foreign_keys;";
        var fkResult = await fkCommand.ExecuteScalarAsync();

        // Act - Verify journal_mode
        await using var walCommand = connection.CreateCommand();
        walCommand.CommandText = "PRAGMA journal_mode;";
        var walResult = await walCommand.ExecuteScalarAsync();

        // Assert
        Convert.ToInt32(fkResult).Should().Be(1);
        walResult?.ToString()?.ToLowerInvariant().Should().Be("wal");
    }

    [Fact]
    public async Task ForeignKeyEnforcement_ThrowsDbUpdateException_WhenParentMissing()
    {
        // Arrange
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        // Act: try adding a PlaylistTrack pointing to nonexistent Playlist and Track
        var orphanedPlaylistTrack = new PlaylistTrack
        {
            PlaylistId = "non-existent-playlist",
            TrackId = "non-existent-track",
            Position = 0
        };

        context.PlaylistTracks.Add(orphanedPlaylistTrack);

        // Assert: foreign key violation must throw DbUpdateException
        var act = async () => await context.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task RelationalGraph_PersistsAndLoadsCorrectly()
    {
        // Arrange
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var artist = new SpotifyArtist
        {
            Id = "artist-1",
            Name = "Daft Punk",
            Uri = "spotify:artist:artist-1"
        };

        var album = new SpotifyAlbum
        {
            Id = "album-1",
            Name = "Discovery",
            AlbumType = "album",
            TotalTracks = 14,
            ReleaseDate = "2001-03-12"
        };

        var track = new SpotifyTrack
        {
            Id = "track-1",
            Name = "One More Time",
            AlbumId = album.Id,
            Album = album,
            DiscNumber = 1,
            TrackNumber = 1,
            DurationMs = 320000,
            Explicit = false,
            Isrc = "FRA123456789"
        };

        var playlist = new SpotifyPlaylist
        {
            Id = "playlist-1",
            Name = "My Favorites",
            TotalTracks = 1
        };

        var playlistTrack = new PlaylistTrack
        {
            PlaylistId = playlist.Id,
            TrackId = track.Id,
            Position = 0,
            AddedAt = DateTimeOffset.UtcNow
        };

        var trackArtist = new TrackArtist
        {
            TrackId = track.Id,
            ArtistId = artist.Id
        };

        var albumArtist = new AlbumArtist
        {
            AlbumId = album.Id,
            ArtistId = artist.Id
        };

        var match = new YouTubeMatch
        {
            TrackId = track.Id,
            YouTubeUrl = "https://www.youtube.com/watch?v=FGBhQbmMxbw",
            VideoId = "FGBhQbmMxbw",
            Title = "Daft Punk - One More Time",
            ConfidenceScore = 0.98,
            MatchStatus = "Matched"
        };

        var job = new DownloadJob
        {
            TrackId = track.Id,
            YouTubeMatchId = match.Id,
            Status = "Pending",
            Format = "mp4"
        };

        context.Artists.Add(artist);
        context.Albums.Add(album);
        context.Tracks.Add(track);
        context.Playlists.Add(playlist);
        context.PlaylistTracks.Add(playlistTrack);
        context.TrackArtists.Add(trackArtist);
        context.AlbumArtists.Add(albumArtist);
        context.YouTubeMatches.Add(match);
        context.DownloadJobs.Add(job);

        await context.SaveChangesAsync();

        // Act
        await using var readContext = CreateContext();
        var retrievedTrack = await readContext.Tracks
            .Include(t => t.Album)
            .Include(t => t.Artists)
            .Include(t => t.Playlists)
            .Include(t => t.YouTubeMatch)
            .Include(t => t.DownloadJobs)
            .FirstOrDefaultAsync(t => t.Id == "track-1");

        // Assert
        retrievedTrack.Should().NotBeNull();
        retrievedTrack!.Name.Should().Be("One More Time");
        retrievedTrack.Album.Should().NotBeNull();
        retrievedTrack.Album!.Name.Should().Be("Discovery");
        retrievedTrack.Artists.Should().ContainSingle(a => a.Name == "Daft Punk");
        retrievedTrack.Playlists.Should().ContainSingle(p => p.Name == "My Favorites");
        retrievedTrack.YouTubeMatch.Should().NotBeNull();
        retrievedTrack.YouTubeMatch!.VideoId.Should().Be("FGBhQbmMxbw");
        retrievedTrack.DownloadJobs.Should().ContainSingle(d => d.Status == "Pending");
    }

    [Fact]
    public async Task CascadeDelete_RemovesChildEntities_WhenTrackDeleted()
    {
        // Arrange
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var track = new SpotifyTrack
        {
            Id = "track-cascade",
            Name = "Track To Delete"
        };

        var match = new YouTubeMatch
        {
            TrackId = track.Id,
            YouTubeUrl = "https://youtube.com/watch?v=xyz",
            MatchStatus = "Matched"
        };

        var job = new DownloadJob
        {
            TrackId = track.Id,
            Status = "Pending"
        };

        context.Tracks.Add(track);
        context.YouTubeMatches.Add(match);
        context.DownloadJobs.Add(job);
        await context.SaveChangesAsync();

        // Act
        await using var deleteContext = CreateContext();
        var toDelete = await deleteContext.Tracks.FindAsync("track-cascade");
        deleteContext.Tracks.Remove(toDelete!);
        await deleteContext.SaveChangesAsync();

        // Assert
        await using var verifyContext = CreateContext();
        (await verifyContext.Tracks.FindAsync("track-cascade")).Should().BeNull();
        (await verifyContext.YouTubeMatches.FirstOrDefaultAsync(y => y.TrackId == "track-cascade")).Should().BeNull();
        (await verifyContext.DownloadJobs.FirstOrDefaultAsync(d => d.TrackId == "track-cascade")).Should().BeNull();
    }

    [Fact]
    public async Task DependencyInjection_RegistersAndResolvesSharpifyDbContext()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _connectionString
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<SqlitePragmaConnectionInterceptor>();
        services.AddDbContext<SharpifyDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<SqlitePragmaConnectionInterceptor>();
            var cs = configuration.GetConnectionString("DefaultConnection");
            options.UseSqlite(cs)
                   .AddInterceptors(interceptor);
        });

        var provider = services.BuildServiceProvider();

        // Act
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetService<SharpifyDbContext>();

        // Assert
        db.Should().NotBeNull();
        await db!.Database.MigrateAsync();
        var canConnect = await db.Database.CanConnectAsync();
        canConnect.Should().BeTrue();
    }
}
