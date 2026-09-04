using System;
using System.Collections.Generic;

namespace Scrapefy.Responses;

public sealed record PaginatedResponse<T>(
    string Href,
    int Limit,
    string? Next,
    int Offset,
    string? Previous,
    int Total,
    IReadOnlyList<T> Items
);

public sealed record SavedItem(
    string AddedAt,
    SpotifyUser? AddedBy,
    bool IsLocal,
    SpotifyTrack? Item,
    SpotifyTrack? Track
);

public sealed record SpotifyUser(
    ExternalUrls ExternalUrls,
    string Href,
    string Id,
    string Type,
    string Uri
);

public sealed record SpotifyTrack(
    SpotifyAlbum Album,
    IReadOnlyList<SpotifyArtist> Artists,
    IReadOnlyList<string> AvailableMarkets,
    int DiscNumber,
    int DurationMs,
    bool Explicit,
    ExternalIds ExternalIds,
    ExternalUrls ExternalUrls,
    string Href,
    string Id,
    bool IsPlayable,
    bool IsLocal,
    Restrictions? Restrictions,
    string Name,
    int Popularity,
    string? PreviewUrl,
    int TrackNumber,
    string Type,
    string Uri
);

public sealed record SpotifyAlbum(
    string AlbumType,
    int TotalTracks,
    IReadOnlyList<string> AvailableMarkets,
    ExternalUrls ExternalUrls,
    string Href,
    string Id,
    IReadOnlyList<SpotifyImage> Images,
    string Name,
    string ReleaseDate,
    string ReleaseDatePrecision,
    Restrictions? Restrictions,
    string Type,
    string Uri,
    IReadOnlyList<SpotifyArtist> Artists
);

public sealed record SpotifyArtist(
    ExternalUrls ExternalUrls,
    string Href,
    string Id,
    string Name,
    string Type,
    string Uri
);

public sealed record SpotifyImage(
    string Url,
    int? Height,
    int? Width
);

public sealed record ExternalUrls(
    string Spotify
);

public sealed record ExternalIds(
    string? Isrc,
    string? Ean,
    string? Upc
);

public sealed record Restrictions(
    string Reason
);

public sealed record AccessTokenResponse(string AccessToken, string TokenType, int ExpiresIn);
public sealed record PlayListResponse(string Name, string Description, string Href, string Id, string Uri);