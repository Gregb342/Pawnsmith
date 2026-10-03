namespace Pawnsmith.Api.Errors;

/// <summary>
/// Refuses a state-changing request sent by a page of another origin (§G.11.1,
/// MEN-010, DEC-089).
/// </summary>
/// <remarks>
/// <para>
/// <b>The browser of the user is a client.</b> MEN-004 publishes the
/// application on the loopback interface, which keeps the network out — not the
/// user's own browser, which sits on the loopback interface too and runs the
/// code of every open tab. An HTML form on any site can send a <c>POST</c> to
/// <c>http://localhost:8080/api/jobs/…/cancel</c>, and a form triggers no
/// preflight.
/// </para>
/// <para>
/// Browsers send an <c>Origin</c> header on such requests. When it is present
/// and names another place than the request's own host, the request is refused
/// before any endpoint runs. A client that sends no <c>Origin</c> — curl, the
/// command line — is not a browser, and is not the vector. <c>GET</c> and
/// <c>HEAD</c> are let through: they change nothing, and the browser does not
/// let a third-party page read their answer anyway, since the API sends no CORS
/// header.
/// </para>
/// <para>
/// The other half of MEN-010, DNS rebinding, makes the page and the API the
/// same origin, so this check passes; the <c>Host</c> header then names the
/// attacker's domain, and that is refused by the host filtering configured in
/// <c>AllowedHosts</c>.
/// </para>
/// </remarks>
public sealed class OriginGuard(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        HttpRequest request = context.Request;

        if (!HttpMethods.IsGet(request.Method)
            && !HttpMethods.IsHead(request.Method)
            && request.Headers.Origin is { Count: > 0 } origins
            && !IsSameHost(origins.ToString(), request.Host))
        {
            throw new ApiException(ApiCodes.CrossOriginRefused);
        }

        await next(context);
    }

    /// <summary>Whether the origin names the host and port the request was sent to.</summary>
    /// <remarks>
    /// The literal <c>null</c> origin — a sandboxed frame, a file opened from
    /// disk — is not the host, and is refused like any other.
    /// </remarks>
    private static bool IsSameHost(string origin, HostString host) =>
        Uri.TryCreate(origin, UriKind.Absolute, out Uri? parsed)
        && string.Equals(parsed.Authority, host.Value, StringComparison.OrdinalIgnoreCase);
}
