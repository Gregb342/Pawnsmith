using System.Net;

namespace Pawnsmith.Api.Hosting;

/// <summary>
/// Tells which listening addresses are not on the loopback interface (§H.5, MEN-004).
/// </summary>
/// <remarks>
/// <para>
/// Loopback means <c>localhost</c>, any IPv4 address in <c>127.0.0.0/8</c>, and
/// <c>::1</c>: only a program on the same machine can reach them. Everything
/// else — <c>0.0.0.0</c>, <c>[::]</c>, <c>+</c>, <c>*</c>, a network address or
/// a host name — can be reached from elsewhere.
/// </para>
/// <para>
/// The address is parsed by ASP.NET's own <see cref="BindingAddress"/>, the
/// type Kestrel itself parses <c>--urls</c> with: <c>http://+:8080</c> is not a
/// valid URI for <see cref="Uri"/>, and a hand-written parser would disagree
/// with the server on exactly the wildcard forms this check exists for.
/// </para>
/// </remarks>
public static class ListeningAddresses
{
    /// <summary>The addresses that are not loopback, in the order given.</summary>
    public static IReadOnlyList<string> NonLocal(IEnumerable<string> addresses)
    {
        ArgumentNullException.ThrowIfNull(addresses);

        return [.. addresses.Where(address => !IsLoopback(BindingAddress.Parse(address).Host))];
    }

    private static bool IsLoopback(string host)
    {
        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // An IPv6 host comes in brackets, "[::1]"; IPAddress wants it bare.
        return IPAddress.TryParse(host.Trim('[', ']'), out IPAddress? ip) && IPAddress.IsLoopback(ip);
    }
}
