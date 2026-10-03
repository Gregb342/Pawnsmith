using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

using Pawnsmith.Application.Ports;

namespace Pawnsmith.Infrastructure.Generation;

/// <summary>
/// The adapter behind <see cref="IImageGenerator"/>: a ComfyUI server, spoken to
/// over its plain HTTP API (§E.7).
/// </summary>
/// <remarks>
/// <para>
/// <b>No WebSocket.</b> Progress is followed by polling the history once a
/// second: four ordinary HTTP calls, testable against a fake server written by
/// hand, and a second of granularity is nothing next to forty seconds of
/// generation.
/// </para>
/// <para>
/// <b>Everything that comes back is untrusted</b> (§E.0): bounded before it is
/// read, checked before it is used, and never joined to a local path. The file
/// name ComfyUI returns only serves to ask ComfyUI for it again.
/// </para>
/// <para>
/// <b>The HTTP client follows no redirect and uses no proxy</b> (DEC-081). A
/// followed redirect would let the validated server name the next target
/// itself, which makes checking the address pointless; a proxy would send the
/// prompts to a third party, when DEC-007 chose a local generator precisely to
/// keep them home.
/// </para>
/// </remarks>
public sealed class ComfyUiImageGenerator : IImageGenerator, IDisposable
{
    private readonly ComfyUiOptions options;
    private readonly WorkflowTemplate workflow;
    private readonly HttpClient http;

    /// <param name="options">Address and bounds.</param>
    /// <param name="workflow">The graph to submit, already validated.</param>
    /// <exception cref="GeneratorConfigException"><c>GENERATOR_URL_INVALID</c>, at construction — never at the first batch.</exception>
    public ComfyUiImageGenerator(ComfyUiOptions options, WorkflowTemplate workflow)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(workflow);

        this.options = options;
        this.workflow = workflow;

        Uri baseAddress = GeneratorAddress.Parse(options.GeneratorUrl);

        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
        };

        // The client-wide timeout is switched off: each call gets its own,
        // through a linked cancellation, so that a timeout can be told apart
        // from a cancellation by the caller — the first is GENERATOR_TIMEOUT,
        // the second is not an error at all.
        http = new HttpClient(handler, disposeHandler: true)
        {
            BaseAddress = baseAddress,
            Timeout = Timeout.InfiniteTimeSpan,
        };
    }

    /// <summary>The framing clause of the workflow this generator submits.</summary>
    /// <remarks>
    /// Exposed so that whoever wires the generator can hand it to the batch as a
    /// plain string (§C.5.2). It is not on the port: no port exists to fetch the
    /// framing clause, and none should.
    /// </remarks>
    public string FramingClause => workflow.FramingClause;

    /// <inheritdoc />
    public async Task<GeneratorAvailability> CheckAsync(CancellationToken cancellationToken)
    {
        try
        {
            using HttpResponseMessage response = await SendAsync(
                () => new HttpRequestMessage(HttpMethod.Get, "system_stats"),
                options.CheckTimeout,
                cancellationToken).ConfigureAwait(false);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                return GeneratorAvailability.Unhealthy;
            }

            JsonNode? body = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);

            return body is JsonObject ? GeneratorAvailability.Available : GeneratorAvailability.Unhealthy;
        }
        catch (GeneratorException error) when (error.Code is GeneratorErrorCode.Unreachable or GeneratorErrorCode.Timeout)
        {
            // The ordinary state of a machine where ComfyUI is not running: a
            // value, never an exception (§E.7.1).
            return GeneratorAvailability.Unreachable;
        }
        catch (GeneratorException)
        {
            return GeneratorAvailability.Unhealthy;
        }
    }

    /// <inheritdoc />
    public Task<GeneratedImage> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken)
    {
        // Written in task 5 of E.17. Declared here so that the type satisfies
        // its port from the first commit.
        throw new NotImplementedException("Generation arrives in task 5 of E.17.");
    }

    public void Dispose() => http.Dispose();

    /// <summary>Sends one request with its own timeout, and turns network failures into codes.</summary>
    /// <param name="build">Builds the request. A function, because a request message cannot be sent twice.</param>
    /// <param name="timeout">The bound of this call alone.</param>
    /// <param name="cancellationToken">The caller's cancellation, which propagates as itself.</param>
    private async Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> build,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);

        using HttpRequestMessage request = build();

        try
        {
            // Headers only: the body is read afterwards, under a bound.
            return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeoutSource.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GeneratorException(
                GeneratorErrorCode.Timeout,
                $"The generator did not answer {request.RequestUri} within {timeout.TotalSeconds:0.#} s.",
                error);
        }
        catch (HttpRequestException error)
        {
            throw new GeneratorException(
                GeneratorErrorCode.Unreachable,
                $"The generator at {http.BaseAddress} could not be reached: {error.Message}",
                error);
        }
    }

    /// <summary>Reads a JSON body under the size bound, or refuses it.</summary>
    /// <returns>The parsed body; null for a literal <c>null</c>.</returns>
    private async Task<JsonNode?> ReadJsonAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        byte[] bytes = await ReadBoundedAsync(response, options.MaxResponseJsonBytes, "JSON answer", cancellationToken)
            .ConfigureAwait(false);

        try
        {
            return JsonNode.Parse(bytes);
        }
        catch (JsonException error)
        {
            throw new GeneratorException(
                GeneratorErrorCode.OutputInvalid,
                $"The generator answered {response.RequestMessage?.RequestUri} with something that is not JSON.",
                error);
        }
    }

    /// <summary>Reads a body, never holding more than <paramref name="maxBytes"/> of it.</summary>
    /// <remarks>
    /// The bound is armed <b>before</b> reading and checked <b>during</b> the
    /// read. A missing or lying <c>Content-Length</c> must not be enough to make
    /// a gigabyte fit in memory, so the declared length is only a shortcut for
    /// an early refusal, never the check itself.
    /// </remarks>
    private async Task<byte[]> ReadBoundedAsync(
        HttpResponseMessage response,
        long maxBytes,
        string what,
        CancellationToken cancellationToken)
    {
        if (response.Content.Headers.ContentLength is long declared && declared > maxBytes)
        {
            throw TooLarge(what, maxBytes);
        }

        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(options.RequestTimeout);

        try
        {
            await using Stream stream = await response.Content.ReadAsStreamAsync(timeoutSource.Token).ConfigureAwait(false);
            using var buffer = new MemoryStream();
            byte[] chunk = new byte[81920];

            while (true)
            {
                int read = await stream.ReadAsync(chunk, timeoutSource.Token).ConfigureAwait(false);

                if (read == 0)
                {
                    return buffer.ToArray();
                }

                if (buffer.Length + read > maxBytes)
                {
                    throw TooLarge(what, maxBytes);
                }

                buffer.Write(chunk, 0, read);
            }
        }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        {
            throw new GeneratorException(
                GeneratorErrorCode.Timeout,
                $"The generator did not finish sending its {what} within {options.RequestTimeout.TotalSeconds:0.#} s.",
                error);
        }
        catch (IOException error)
        {
            throw new GeneratorException(
                GeneratorErrorCode.Unreachable,
                $"The connection to the generator was cut while it sent its {what}: {error.Message}",
                error);
        }
        catch (HttpRequestException error)
        {
            throw new GeneratorException(
                GeneratorErrorCode.Unreachable,
                $"The connection to the generator was cut while it sent its {what}: {error.Message}",
                error);
        }
    }

    private static GeneratorException TooLarge(string what, long maxBytes) =>
        new(GeneratorErrorCode.OutputInvalid,
            $"The generator's {what} is larger than the {maxBytes} bytes accepted (§E.9).");
}
