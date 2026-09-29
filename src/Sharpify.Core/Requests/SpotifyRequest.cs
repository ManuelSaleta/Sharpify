namespace Sharpify.Core.Requests;

/// <summary>
/// Build any valid spotify request
/// </summary>
/// <param name="uri"></param>
/// <param name="q"></param>
public sealed class SpotifyRequest(string uri, HttpMethod? method = null, object? req = null, IReadOnlyDictionary<string, string>? q = null)
{
    public SpotifyRequest(string uri, IReadOnlyDictionary<string, string>? q)
        : this(uri, HttpMethod.Get, req: null, q)
    {
    }

    public string Uri { get; set; } = uri;
    public HttpMethod Method { get; set; } = method ?? HttpMethod.Get;
    public object? Body { get; set; } = req;
    public object? Req
    {
        get => Body;
        set => Body = value;
    }
    public IReadOnlyDictionary<string, string>? QueryParameters { get; } = q;

    /// <summary>
    /// ToString() override so you - builds the query for you.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var normalizedUri = Uri.StartsWith('/') ? Uri : $"/{Uri}";

        // No query params
        if (QueryParameters is null || QueryParameters.Count == 0)
            return normalizedUri;


        // Functional "reducer": escapes keys/values and joins with '&' in one pass
        // Ex: kvp {"limit": 10, "offset": 5 } => 'limit=10&offset=5'
        var queryString = string.Join('&', QueryParameters.Select(static kvp =>
        {
            // You can inline this, but this is more readable.
            var param = System.Uri.EscapeDataString(kvp.Key);
            var value = System.Uri.EscapeDataString(kvp.Value);
            return $"{param}={value}";
        }));

        return $"{normalizedUri}?{queryString}";
    }
}
