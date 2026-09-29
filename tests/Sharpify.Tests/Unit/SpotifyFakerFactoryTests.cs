namespace Sharpify.Tests.Unit;

using Sharpify.Core.Requests;
using Sharpify.Core.Responses;

public class SpotifyFakerFactoryTests
{
    [Fact]
    public void Generate_PaginatedResponseSpotifyAlbum_DefaultCount_ReturnsSingleItemList()
    {
        var result = SpotifyFakerFactory.Generate<PaginatedResponse<SpotifyAlbum>>();

        Assert.NotNull(result);
        var paginated = Assert.Single(result);
        Assert.False(string.IsNullOrWhiteSpace(paginated.Href));
        Assert.True(paginated.Limit > 0);
        Assert.True(paginated.Offset >= 0);
        Assert.True(paginated.Total >= 0);
        Assert.NotEmpty(paginated.Items);

        var firstAlbum = paginated.Items[0];
        Assert.False(string.IsNullOrWhiteSpace(firstAlbum.Id));
        Assert.False(string.IsNullOrWhiteSpace(firstAlbum.Name));
        Assert.False(string.IsNullOrWhiteSpace(firstAlbum.Uri));
        Assert.Equal("album", firstAlbum.Type);
        Assert.NotEmpty(firstAlbum.Images);
        Assert.NotEmpty(firstAlbum.Artists);
        Assert.NotEmpty(firstAlbum.AvailableMarkets);
        Assert.NotNull(firstAlbum.ExternalUrls);
        Assert.False(string.IsNullOrWhiteSpace(firstAlbum.ExternalUrls.Spotify));
    }

    [Fact]
    public void Generate_PaginatedResponseSpotifyAlbum_CountGreaterThanOne_ReturnsListWithCount()
    {
        var result = SpotifyFakerFactory.Generate<PaginatedResponse<SpotifyAlbum>>(3);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        foreach (var item in result)
        {
            Assert.NotEmpty(item.Items);
        }
    }

    [Fact]
    public void Generate_PaginatedResponseSpotifyAlbum_CountZero_ReturnsSingleItemList()
    {
        var result = SpotifyFakerFactory.Generate<PaginatedResponse<SpotifyAlbum>>(0);

        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public void Generate_PaginatedResponseSpotifyAlbum_NegativeCount_UsesAbsoluteValue()
    {
        var result = SpotifyFakerFactory.Generate<PaginatedResponse<SpotifyAlbum>>(-2);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Generate_SpotifyAlbum_DefaultCount_ReturnsSingleItemList()
    {
        var result = SpotifyFakerFactory.Generate<SpotifyAlbum>();

        Assert.NotNull(result);
        var album = Assert.Single(result);
        Assert.False(string.IsNullOrWhiteSpace(album.Id));
        Assert.False(string.IsNullOrWhiteSpace(album.Name));
        Assert.NotEmpty(album.Artists);
    }

    [Fact]
    public void Generate_SpotifyAlbum_CountGreaterThanOne_ReturnsListWithCount()
    {
        var result = SpotifyFakerFactory.Generate<SpotifyAlbum>(2);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Generate_SpotifyRequest_DefaultCount_ReturnsSingleItemList()
    {
        var result = SpotifyFakerFactory.Generate<SpotifyRequest>();

        Assert.NotNull(result);
        var request = Assert.Single(result);
        Assert.False(string.IsNullOrWhiteSpace(request.Uri));
        Assert.NotNull(request.Method);
        Assert.NotNull(request.QueryParameters);
        Assert.NotEmpty(request.QueryParameters);
    }

    [Fact]
    public void Generate_SpotifyRequest_CountGreaterThanOne_ReturnsListWithCount()
    {
        var result = SpotifyFakerFactory.Generate<SpotifyRequest>(3);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        foreach (var req in result)
        {
            Assert.False(string.IsNullOrWhiteSpace(req.Uri));
            Assert.NotNull(req.Method);
        }
    }

    [Fact]
    public void Generate_UnsupportedType_ThrowsNotSupportedException()
    {
        Assert.Throws<NotSupportedException>(() => SpotifyFakerFactory.Generate<string>());
    }
}

