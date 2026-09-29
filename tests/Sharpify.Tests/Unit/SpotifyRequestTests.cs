namespace Sharpify.Tests.Unit;

using FluentAssertions;
using Sharpify.Core.Requests;

public class SpotifyRequestTests
{
    [Fact]
    public void SampleTest()
    {
        var res = SpotifyFakerFactory.Generate<SpotifyRequest>();
        res.Should().NotBeNull();
        res.Should().ContainSingle();

        var request = res[0];
        request.Uri.Should().NotBeNullOrWhiteSpace();
        request.ToString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void SpotifyRequest_RequestBody_ShouldBeStoredAndRetrievable()
    {
        var bodyPayload = new { name = "Favorites" };
        var request = new SpotifyRequest("v1/playlists", HttpMethod.Post, req: bodyPayload);

        request.Body.Should().BeSameAs(bodyPayload);
        request.Req.Should().BeSameAs(bodyPayload);
    }
}
