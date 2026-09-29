using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Sharpify.Core.Authentication;
using Sharpify.Core.Clients;
using Sharpify.Core.Responses;

internal class Program
{
    private static async Task Main(string[] args)
    {
        HostApplicationBuilderSettings settings = new()
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        };
        HostApplicationBuilder builder = Host.CreateApplicationBuilder(settings);

        // Bind settings with compile-time generation & fail-fast validation on startup
        builder.Services
            .AddOptions<SpotifyClientOptions>()
            .Bind(builder.Configuration.GetSection(SpotifyClientOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register token store (persisted to ~/.sharpify/token.json)
        builder.Services.AddSingleton<ISpotifyTokenStore, FileSpotifyTokenStore>();

        // Register authentication service
        builder.Services.AddHttpClient<ISpotifyAuthService, SpotifyAuthService>();

        // Register application services
        builder.Services.AddHttpClient<SpotifyClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SpotifyClientOptions>>().Value;
            httpClient.BaseAddress = new Uri(options.ClientBaseUrl.TrimEnd('/') + "/");
        });

        using IHost host = builder.Build();

        var options = host.Services.GetRequiredService<IOptions<SpotifyClientOptions>>().Value;
        var tokenStore = host.Services.GetRequiredService<ISpotifyTokenStore>();
        var authService = host.Services.GetRequiredService<ISpotifyAuthService>();
        var spotifyClient = host.Services.GetRequiredService<SpotifyClient>();

        // Ensure user is authenticated
        await SpotifyOAuthHelper.LoginAsync(authService, tokenStore, options.RedirectUri);

        Console.WriteLine("Fetching your Spotify playlists...\n");
        var userPlaylists = await spotifyClient.GetUserPlaylistsAsync();

        if (userPlaylists.Items.Count == 0)
        {
            Console.WriteLine("No playlists found in your account.");
            return;
        }

        Console.WriteLine($"Found {userPlaylists.Total} playlist(s) in your library:");
        for (int i = 0; i < Math.Min(5, userPlaylists.Items.Count); i++)
        {
            var p = userPlaylists.Items[i];
            Console.WriteLine($"  [{i + 1}] {p.Name} (ID: {p.Id})");
        }

        // Use playlist passed as argument or default to the first playlist in the user's library
        var targetPlaylist = userPlaylists.Items[0];
        if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            var matched = userPlaylists.Items.FirstOrDefault(p => p.Id == args[0]);
            targetPlaylist = matched ?? new Sharpify.Core.Responses.PlayListResponse(args[0], "", "", args[0], "");
        }

        Console.WriteLine($"\nFetching tracks for: \"{targetPlaylist.Name}\" (ID: {targetPlaylist.Id})...\n");

        var result = await spotifyClient.GetPlaylistItemsAsync(targetPlaylist.Id);
        var totalSongCount = result.Total;
        var songs = new List<SavedItem>();
        var offSet = result.Items.Count;
        songs.AddRange(result.Items);
        Console.WriteLine($"Successfully retrieved {result.Total} tracks (showing {result.Items.Count}):\n");
        while (offSet < totalSongCount)
        {
            var res = await spotifyClient
            .GetPlaylistItemsAsync(targetPlaylist.Id, new Dictionary<string, string> { ["offset"] = $"{offSet}" });
            songs.AddRange(res.Items);
            offSet += res.Items.Count;
            // if (offSet == totalSongCount) yield break;
        }
        int index = 1;
        foreach (var item in songs)
        {
            var track = item.Item ?? item.Track;
            if (track is not null)
            {
                var artists = track.Artists?.Count > 0
                    ? string.Join(", ", track.Artists.Select(a => a.Name))
                    : "Unknown Artist";
                Console.WriteLine($"{index++}. 🎵 {track.Name} — {artists}");
            }
        }
    }
}
