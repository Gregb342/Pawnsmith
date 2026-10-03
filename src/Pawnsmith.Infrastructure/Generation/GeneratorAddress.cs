namespace Pawnsmith.Infrastructure.Generation;

/// <summary>
/// Checks the generator's address, as DEC-081 asks (MEN-003).
/// </summary>
/// <remarks>
/// <para>
/// <b>MEN-003 is treated at the root, not filtered.</b> An SSRF needs an
/// attacker to <i>choose</i> the address the server calls. This address is a
/// deployment setting the operator writes on their own machine, and the API
/// never changes it, so nobody else chooses it. A whitelist of ports, which
/// chapter 9 used to prescribe, would protect from nothing and break the day
/// ComfyUI runs somewhere other than 8188.
/// </para>
/// <para>
/// What is checked is what keeps the address an address. Each rule has its own
/// reason, written beside it.
/// </para>
/// <para>
/// <b>A refusal never repeats more than <c>scheme://host:port/path</c>.</b>
/// Its message reaches the log (§H.4.1), and the parts it leaves out are where
/// a secret hides: credentials, a token in a query string or a fragment. Text
/// that did not even parse is not repeated at all — <c>user:secret@host</c>
/// without a scheme reads as the scheme <c>user</c>, with the secret in what
/// follows.
/// </para>
/// </remarks>
public static class GeneratorAddress
{
    /// <summary>The address as a base URI, ending with a slash so that relative calls append to it.</summary>
    /// <exception cref="GeneratorConfigException"><c>GENERATOR_URL_INVALID</c>.</exception>
    public static Uri Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text) || !Uri.TryCreate(text.Trim(), UriKind.Absolute, out Uri? uri))
        {
            throw Refuse(shown: null, "it is not an absolute address");
        }

        // file:, ftp: and whatever else the HTTP stack might know how to open
        // are closed off. The generator speaks HTTP (section 6.4).
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            throw Refuse(shown: null, $"its scheme is '{uri.Scheme}'; only http and https are accepted");
        }

        if (uri.Host.Length == 0)
        {
            throw Refuse(shown: null, "it names no host");
        }

        // What a refusal may repeat from here on: the address without its
        // credentials, query or fragment.
        string shown = new UriBuilder(uri) { UserName = string.Empty, Password = string.Empty }.Uri.GetLeftPart(UriPartial.Path);

        // A secret in an address ends up in a log (MEN-006, chapter 8).
        if (uri.UserInfo.Length > 0)
        {
            throw Refuse(shown, "it carries credentials; the generator is reached without any");
        }

        // The calls add their own path and query; an address that already
        // carries some would be concatenated the wrong way.
        if (uri.Query.Length > 0 || uri.Fragment.Length > 0)
        {
            throw Refuse(shown, "it carries a query or a fragment");
        }

        // A path is allowed — ComfyUI behind a reverse proxy at /comfy/ — and
        // made to end with a slash, so that "prompt" resolves to /comfy/prompt
        // and not to /prompt.
        return uri.AbsolutePath.EndsWith('/')
            ? uri
            : new Uri(uri.GetLeftPart(UriPartial.Path) + "/");
    }

    private static GeneratorConfigException Refuse(string? shown, string why) =>
        new(GeneratorConfigErrorCode.GeneratorUrlInvalid,
            shown is null
                ? $"The generator address is refused because {why} (DEC-081)."
                : $"The generator address '{shown}' is refused because {why} (DEC-081).");
}
