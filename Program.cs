using Scrapefy.Clients;

Console.WriteLine("Fetching data from Spotify API...");

var spotifyClient = new SpotifyClient();

var playListId = "0laxkTPia4Ko1FyuC1Wmie";

// var result = await spotifyClient.GetPlaylistItemsAsync(playListId);
var result = await spotifyClient.GetPlaylistAsync(playListId);
var test = "test";
// https://open.spotify.com/playlist/0laxkTPia4Ko1FyuC1Wmie?si=7473aecfdfec4203

