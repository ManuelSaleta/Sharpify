using Sharpify.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Sharpify.Lib;

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
            .ValidateOnStart(); // Crashes early on launch if configuration is missing/invalid

        // Register application services
        builder.Services.AddHttpClient<SpotifyClient>((serviceProvider, httpClient) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SpotifyClientOptions>>().Value;
            httpClient.BaseAddress = new Uri(options.ClientBaseUrl.TrimEnd('/') + "/");
        });

        using IHost host = builder.Build();

        Console.WriteLine("Fetching data from Spotify API...");

        var spotifyClient = host.Services.GetRequiredService<SpotifyClient>();

        var playListId = "0vvXsWCC9xrXsKd4FyS8kM";

        var result = await spotifyClient.GetPlaylistAsync(playListId);

        Console.WriteLine($"Playlist Name: {result.Name}");
    }
}

