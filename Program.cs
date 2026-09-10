using Sharpify.Clients;

Console.WriteLine("Fetching data from Spotify API...");

var spotifyClient = new SpotifyClient();

// var id = "0laxkTPia4Ko1FyuC1Wmie";
var id = "37vVbInEzfnXJQjVuU7bAZ";

// var result = await spotifyClient.GetPlaylistItemsAsync(playListId);
var result = await spotifyClient
    .GetPlaylistItemsAsync(new ( $"playlists/{id}/items"));

Console.WriteLine(result.ToString());
// https://open.spotify.com/playlist/0laxkTPia4Ko1FyuC1Wmie?si=7473aecfdfec4203

