using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Pawnsmith.Infrastructure.Tests.Fixtures;

/// <summary>A request as the fake server received it.</summary>
/// <param name="Method">GET, POST…</param>
/// <param name="Target">Path and query, as sent: <c>/history/abc</c>, <c>/view?filename=x.png</c>.</param>
/// <param name="Body">The body, decoded as UTF-8. Empty for a GET.</param>
internal sealed record FakeRequest(string Method, string Target, string Body)
{
    /// <summary>The path without its query.</summary>
    public string Path => Target.Split('?')[0];
}

/// <summary>What the fake server answers.</summary>
/// <param name="Chunked">
/// Sends the body with <c>Transfer-Encoding: chunked</c> and no
/// <c>Content-Length</c>, so that the client cannot know the size in advance and
/// has to enforce its bound while reading.
/// </param>
internal sealed record FakeResponse(int Status, byte[] Body, string ContentType, string? Location = null, bool Chunked = false)
{
    public static FakeResponse Json(string json, int status = 200) =>
        new(status, Encoding.UTF8.GetBytes(json), "application/json");

    public static FakeResponse Png(byte[] png) => new(200, png, "image/png");

    public static FakeResponse Redirect(string location) => new(302, [], "text/plain", location);
}

/// <summary>
/// A real HTTP server on the loopback interface, written by hand on a bare
/// socket, for testing the ComfyUI client.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why by hand, and why a real socket.</b> No mocking library is used (§3 of
/// CLAUDE.md forbids Moq, and NSubstitute is not needed here). A fake
/// <c>HttpMessageHandler</c> would have been shorter, but it never opens a
/// connection — so it could not show what a refused connection, a server that
/// never answers, or a disabled redirect do to the real client stack. This
/// server speaks just enough HTTP/1.1 for the client: one request per
/// connection, a <c>Content-Length</c> body, and <c>Connection: close</c>.
/// </para>
/// <para>
/// <c>HttpListener</c> was the other candidate. On Windows it sits on
/// <c>http.sys</c>, which wants an access-control entry for some prefixes; a
/// socket behaves the same on the owner's machine and in CI.
/// </para>
/// </remarks>
internal sealed class FakeHttpServer : IAsyncDisposable
{
    private readonly TcpListener listener;
    private readonly Func<FakeRequest, Task<FakeResponse>> handler;
    private readonly CancellationTokenSource stopping = new();
    private readonly List<FakeRequest> requests = [];
    private readonly Task loop;

    public FakeHttpServer(Func<FakeRequest, Task<FakeResponse>> handler)
    {
        this.handler = handler;
        listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        BaseAddress = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/";
        loop = AcceptAsync();
    }

    public FakeHttpServer(Func<FakeRequest, FakeResponse> handler)
        : this(request => Task.FromResult(handler(request)))
    {
    }

    /// <summary>The address to give the client, ending with a slash.</summary>
    public string BaseAddress { get; }

    /// <summary>Every request received so far, in arrival order.</summary>
    public IReadOnlyList<FakeRequest> Requests
    {
        get
        {
            lock (requests)
            {
                return [.. requests];
            }
        }
    }

    /// <summary>An address on which nothing listens: a port opened, then closed again.</summary>
    public static string ClosedAddress()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        int port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();

        return $"http://127.0.0.1:{port}/";
    }

    public async ValueTask DisposeAsync()
    {
        await stopping.CancelAsync();
        listener.Stop();

        try
        {
            await loop;
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        catch (SocketException)
        {
        }

        stopping.Dispose();
    }

    private async Task AcceptAsync()
    {
        while (!stopping.IsCancellationRequested)
        {
            TcpClient client = await listener.AcceptTcpClientAsync(stopping.Token);

            // Each connection on its own task: a handler that waits — to
            // simulate a slow generator — must not block the next request.
            _ = Task.Run(() => ServeAsync(client));
        }
    }

    private async Task ServeAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                FakeRequest request = await ReadRequestAsync(stream);

                lock (requests)
                {
                    requests.Add(request);
                }

                FakeResponse response = await handler(request);
                await WriteResponseAsync(stream, response);
            }
            catch (IOException)
            {
                // The client gave up — a timeout test, or a cancellation. Not
                // the server's business.
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private async Task<FakeRequest> ReadRequestAsync(NetworkStream stream)
    {
        // Headers: read byte by byte up to the blank line. Slow and simple,
        // which is right for a test server that handles a dozen requests.
        var header = new List<byte>();
        byte[] one = new byte[1];

        while (!EndsWithBlankLine(header))
        {
            int read = await stream.ReadAsync(one, stopping.Token);

            if (read == 0)
            {
                throw new IOException("The connection closed before the headers ended.");
            }

            header.Add(one[0]);
        }

        string[] lines = Encoding.ASCII.GetString([.. header]).Split("\r\n");
        string[] requestLine = lines[0].Split(' ');

        int contentLength = 0;

        foreach (string line in lines.Skip(1))
        {
            if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
            {
                contentLength = int.Parse(line["Content-Length:".Length..].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        byte[] body = new byte[contentLength];
        await stream.ReadExactlyAsync(body, stopping.Token);

        return new FakeRequest(requestLine[0], requestLine[1], Encoding.UTF8.GetString(body));
    }

    private async Task WriteResponseAsync(NetworkStream stream, FakeResponse response)
    {
        var head = new StringBuilder();
        head.Append(System.Globalization.CultureInfo.InvariantCulture, $"HTTP/1.1 {response.Status} Fake\r\n");
        head.Append(System.Globalization.CultureInfo.InvariantCulture, $"Content-Type: {response.ContentType}\r\n");
        if (!response.Chunked)
        {
            head.Append(System.Globalization.CultureInfo.InvariantCulture, $"Content-Length: {response.Body.Length}\r\n");
        }
        else
        {
            head.Append("Transfer-Encoding: chunked\r\n");
        }

        if (response.Location is not null)
        {
            head.Append(System.Globalization.CultureInfo.InvariantCulture, $"Location: {response.Location}\r\n");
        }

        head.Append("Connection: close\r\n\r\n");

        await stream.WriteAsync(Encoding.ASCII.GetBytes(head.ToString()), stopping.Token);

        if (!response.Chunked)
        {
            await stream.WriteAsync(response.Body, stopping.Token);
        }
        else
        {
            // One chunk with the whole body, then the empty chunk that ends it.
            string size = response.Body.Length.ToString("X", System.Globalization.CultureInfo.InvariantCulture);
            await stream.WriteAsync(Encoding.ASCII.GetBytes($"{size}\r\n"), stopping.Token);
            await stream.WriteAsync(response.Body, stopping.Token);
            await stream.WriteAsync("\r\n0\r\n\r\n"u8.ToArray(), stopping.Token);
        }
        await stream.FlushAsync(stopping.Token);
    }

    private static bool EndsWithBlankLine(List<byte> bytes) =>
        bytes.Count >= 4
        && bytes[^4] == '\r' && bytes[^3] == '\n'
        && bytes[^2] == '\r' && bytes[^1] == '\n';
}
