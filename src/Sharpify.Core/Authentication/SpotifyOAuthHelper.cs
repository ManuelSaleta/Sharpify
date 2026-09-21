namespace Sharpify.Core.Authentication;

using System.Diagnostics;
using System.Net;
using System.Text;

public static class SpotifyOAuthHelper
{
    public static async Task<SpotifyToken> LoginAsync(
        ISpotifyAuthService authService,
        ISpotifyTokenStore tokenStore,
        string? redirectUri = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(authService);
        ArgumentNullException.ThrowIfNull(tokenStore);

        // 1. Check existing stored token
        var existingToken = await tokenStore.GetTokenAsync(ct);
        if (existingToken is not null)
        {
            if (!existingToken.IsExpired)
            {
                return existingToken;
            }

            if (!string.IsNullOrWhiteSpace(existingToken.RefreshToken))
            {
                try
                {
                    var refreshed = await authService.RefreshTokenAsync(existingToken.RefreshToken, ct);
                    await tokenStore.SaveTokenAsync(refreshed, ct);
                    return refreshed;
                }
                catch
                {
                    // If refresh fails, proceed to re-authorization
                }
            }
        }

        // 2. Start interactive authorization
        var state = Guid.NewGuid().ToString("N");
        var authUri = authService.BuildAuthorizationUri(state);
        var effectiveRedirectUri = redirectUri ?? "http://127.0.0.1:5000/callback";

        var prefix = effectiveRedirectUri.TrimEnd('/') + "/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(prefix);

        if (Uri.TryCreate(effectiveRedirectUri, UriKind.Absolute, out var parsedUri))
        {
            if (parsedUri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                var alt = $"http://localhost:{parsedUri.Port}{parsedUri.AbsolutePath.TrimEnd('/')}/";
                try { listener.Prefixes.Add(alt); } catch { }
            }
            else if (parsedUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
            {
                var alt = $"http://127.0.0.1:{parsedUri.Port}{parsedUri.AbsolutePath.TrimEnd('/')}/";
                try { listener.Prefixes.Add(alt); } catch { }
            }
        }

        listener.Start();


        Console.WriteLine();
        Console.WriteLine("==================================================================");
        Console.WriteLine("                SPOTIFY USER AUTHORIZATION REQUIRED               ");
        Console.WriteLine("==================================================================");
        Console.WriteLine("Opening browser to authorize with Spotify...");
        Console.WriteLine($"URL: {authUri}");
        Console.WriteLine("==================================================================");
        Console.WriteLine();

        // Launch system browser
        TryOpenBrowser(authUri.ToString());

        Console.WriteLine("Waiting for authorization callback in browser...");

        // Listen for the redirect callback
        var context = await listener.GetContextAsync();
        var query = context.Request.QueryString;

        var returnedState = query["state"];
        var error = query["error"];
        var code = query["code"];

        if (!string.IsNullOrWhiteSpace(error))
        {
            SendResponse(context.Response, "Authorization failed: " + error, HttpStatusCode.BadRequest);
            throw new InvalidOperationException($"Spotify authorization denied: {error}");
        }

        if (returnedState != state)
        {
            SendResponse(context.Response, "Invalid state parameter. Authorization failed.", HttpStatusCode.BadRequest);
            throw new InvalidOperationException("State mismatch during Spotify authorization.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            SendResponse(context.Response, "No authorization code received.", HttpStatusCode.BadRequest);
            throw new InvalidOperationException("No authorization code received from Spotify.");
        }

        SendResponse(context.Response,
            "<html><body style='font-family:sans-serif;text-align:center;padding-top:50px;'>" +
            "<h1 style='color:#1DB954;'>Authorization Successful!</h1>" +
            "<p>You can close this window and return to your terminal.</p>" +
            "</body></html>",
            HttpStatusCode.OK, "text/html");

        listener.Stop();

        // 3. Exchange code for tokens
        var token = await authService.ExchangeCodeForTokenAsync(code, effectiveRedirectUri, ct);
        await tokenStore.SaveTokenAsync(token, ct);

        Console.WriteLine("Authorization successful! Token saved.\n");
        return token;
    }

    private static void SendResponse(HttpListenerResponse response, string content, HttpStatusCode statusCode, string contentType = "text/plain")
    {
        response.StatusCode = (int)statusCode;
        response.ContentType = contentType;
        var buffer = Encoding.UTF8.GetBytes(content);
        response.ContentLength64 = buffer.Length;
        using var output = response.OutputStream;
        output.Write(buffer, 0, buffer.Length);
    }

    private static void TryOpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Browser launch failed or running headless; user can copy the link manually
        }
    }
}
