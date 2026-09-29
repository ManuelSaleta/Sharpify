using Bogus;
using Sharpify.Core.Requests;
using Sharpify.Core.Responses;

namespace Sharpify.Tests;

public static class SpotifyFakerFactory
{
    private static readonly Faker<SpotifyAlbum> fakeAlbum = new Faker<SpotifyAlbum>()
        .CustomInstantiator(f => new SpotifyAlbum(
            default!, default, default!, default!, default!, default!,
            default!, default!, default!, default!, default, default!,
            default!, default!))
        .RuleFor(x => x.AlbumType, f => f.PickRandom("album", "single", "compilation"))
        .RuleFor(x => x.TotalTracks, f => f.Random.Number(1, 30))
        .RuleFor(x => x.AvailableMarkets, f => f.Make(f.Random.Number(1, 5), () => f.Address.CountryCode()).ToList())
        .RuleFor(x => x.ExternalUrls, f => new ExternalUrls(f.Internet.Url()))
        .RuleFor(x => x.Href, f => f.Internet.Url())
        .RuleFor(x => x.Id, f => f.Random.AlphaNumeric(22))
        .RuleFor(x => x.Images, f => new List<SpotifyImage>
        {
            new(f.Internet.Url(), 640, 640),
            new(f.Internet.Url(), 300, 300),
            new(f.Internet.Url(), 64, 64)
        })
        .RuleFor(x => x.Name, f => f.Commerce.ProductName())
        .RuleFor(x => x.ReleaseDate, f => f.Date.Past(5).ToString("yyyy-MM-dd"))
        .RuleFor(x => x.ReleaseDatePrecision, f => f.PickRandom("day", "month", "year"))
        .RuleFor(x => x.Restrictions, f => null)
        .RuleFor(x => x.Type, f => "album")
        .RuleFor(x => x.Uri, (f, u) => $"spotify:album:{u.Id}")
        .RuleFor(x => x.Artists, f =>
        {
            var artistId = f.Random.AlphaNumeric(22);
            return new List<SpotifyArtist>
            {
                new(
                    new ExternalUrls(f.Internet.Url()),
                    $"https://api.spotify.com/v1/artists/{artistId}",
                    artistId,
                    f.Name.FullName(),
                    "artist",
                    $"spotify:artist:{artistId}"
                )
            };
        });

    private static readonly Faker<PaginatedResponse<SpotifyAlbum>> paginatedFakeAlbum = new Faker<PaginatedResponse<SpotifyAlbum>>()
        .CustomInstantiator(f => new PaginatedResponse<SpotifyAlbum>(default!, default, default, default, default, default, default!))
        .RuleFor(x => x.Href, f => f.Internet.Url())
        .RuleFor(x => x.Limit, f => f.Random.Number(1, 50))
        .RuleFor(x => x.Next, f => f.Internet.Url())
        .RuleFor(x => x.Offset, f => f.Random.Number(0, 100))
        .RuleFor(x => x.Previous, f => null)
        .RuleFor(x => x.Total, f => f.Random.Number(1, 100))
        .RuleFor(x => x.Items, f => fakeAlbum.Generate(f.Random.Number(1, 5)));

    private static readonly Faker<SpotifyRequest> fakeRequest = new Faker<SpotifyRequest>()
        .CustomInstantiator(f =>
        {
            var method = f.PickRandom(HttpMethod.Get, HttpMethod.Post, HttpMethod.Put, HttpMethod.Delete);
            object? body = method == HttpMethod.Post || method == HttpMethod.Put
                ? new Dictionary<string, object> { ["name"] = f.Commerce.ProductName() }
                : null;

            return new SpotifyRequest(
                $"v1/{f.PickRandom("albums", "artists", "playlists", "tracks")}/{f.Random.AlphaNumeric(22)}",
                method,
                req: body,
                new Dictionary<string, string>
                {
                    ["limit"] = f.Random.Number(1, 50).ToString(),
                    ["offset"] = f.Random.Number(0, 100).ToString()
                }
            );
        });

    /// <summary>
    /// Register your faker inside this call. That will make it available
    /// When calling this.Generate<SomeType>()
    /// </summary>
    /// <returns>
    /// Dictionary<k,v> where k is the Type you made a faker for, v a function that calls the underlying Faker.Generate(count)
    /// </returns>
    private static Dictionary<Type, Func<int, object>> GetRegisteredFakers() => new()
    {
        [typeof(SpotifyAlbum)] = count => fakeAlbum.Generate(count),
        [typeof(PaginatedResponse<SpotifyAlbum>)] = count => paginatedFakeAlbum.Generate(count),
        [typeof(SpotifyRequest)] = count => fakeRequest.Generate(count)
    };


    public static List<T> Generate<T>(int count = 1)
    {
        if (count == 0) count = 1;

        count = count == int.MinValue ? 1 : Math.Abs(count);

        var registered = GetRegisteredFakers();
        registered.TryGetValue(typeof(T), out var generated);
        if (generated is null)
        {
            throw new NotSupportedException($"Type '{typeof(T).Name}' is not registered.");
        }

        return (List<T>)generated(count);
    }
}

