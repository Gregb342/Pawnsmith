namespace Pawnsmith.Api.Hosting;

/// <summary>
/// Puts the same security headers on every response: the page, the API, an
/// image, an error (§I.15.1, MEN-012, MEN-013, DEC-116).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why the front needs them when the API alone did not.</b> Two surfaces
/// open with an interface. A third-party page can load the application in an
/// invisible frame on <c>localhost</c> and lay its own buttons over ours — the
/// user believes they click there and deletes a blueprint here (MEN-012,
/// clickjacking). And the front shows other people's text — a project name, a
/// clause — possibly from an imported archive; rendered as HTML it would run
/// script in the application's origin, which can do anything through the API
/// (MEN-013).
/// </para>
/// <para>
/// <b>Set when the response starts, not when the request arrives.</b> The
/// error middleware clears the response before writing its code, headers
/// included; a header set on the way in would vanish from every error. A
/// callback registered here runs at the very moment the headers are sent,
/// after any such clearing.
/// </para>
/// </remarks>
public sealed class SecurityHeaders(RequestDelegate next)
{
    /// <summary>
    /// The content security policy, one directive per concern.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>default-src 'self'</c>: anything not named below comes from the origin or not at all.</item>
    /// <item><c>script-src 'self'</c>: the bundle Vite builds, and no inline script — the one an injection would write.</item>
    /// <item><c>style-src 'self'</c>: the stylesheet of the bundle. Styles React sets through the DOM are not inline markup and stay allowed.</item>
    /// <item><c>img-src 'self' data:</c>: the images the API serves, and the small assets Vite may inline as data URIs.</item>
    /// <item><c>frame-ancestors 'self'</c>: no other site may frame the application (MEN-012). Not <c>'none'</c>: the sheet preview is a frame of the same origin (DEC-113).</item>
    /// <item><c>base-uri 'none'</c>: an injected <c>&lt;base&gt;</c> cannot redirect the relative addresses of the page.</item>
    /// <item><c>form-action 'self'</c>: no form of the page can post anywhere else.</item>
    /// </list>
    /// </remarks>
    public const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; " +
        "frame-ancestors 'self'; base-uri 'none'; form-action 'self'";

    public Task InvokeAsync(HttpContext context)
    {
        HttpResponse response = context.Response;

        response.OnStarting(() =>
        {
            IHeaderDictionary headers = response.Headers;

            headers.ContentSecurityPolicy = ContentSecurityPolicy;

            // For browsers that ignore frame-ancestors (MEN-012).
            headers.XFrameOptions = "SAMEORIGIN";

            // An image served by the API is never read as a script, whatever
            // bytes an imported archive put in it.
            headers.XContentTypeOptions = "nosniff";

            // The address of a project carries its name; it does not leave for
            // a third party with a link the user follows.
            headers["Referrer-Policy"] = "no-referrer";

            return Task.CompletedTask;
        });

        return next(context);
    }
}
