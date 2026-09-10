namespace Sharpify.Requests;

/// <summary>
/// Build any valid spotify request
/// </summary>
/// <param name="uri"></param>
/// <param name="q"></param>
public sealed class SpotifyRequest(string uri, HttpVerbPOST, IReadOnlyDictionary<string, string>? q = null)
{
    public string Uri { get; set; } = uri;
    public IReadOnlyDictionary<string, string>? QueryParameters { get; } = q;

    /// <summary>
    /// ToString() override so you - builds the query for you.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var normalizedUri = uri.StartsWith('/') ? uri : $"/{uri}";

        // No query params
        if (q is null || q.Count == 0)
            return normalizedUri;


        // Functional "reducer": escapes keys/values and joins with '&' in one pass
        // Ex: kvp {"limit": 10, "offset": 5 } => 'limit=10&offset=5'
        var queryString = string.Join('&', q.Select(static kvp =>
        {
            // You can inline this, but this is more readable.
            var param = System.Uri.EscapeDataString(kvp.Key);
            var value = System.Uri.EscapeDataString(kvp.Value);
            return $"{param}={value}";
        }));

        return $"{normalizedUri}?{queryString}";
    }
}
