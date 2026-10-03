using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.Generation;
using Pawnsmith.Infrastructure.Imaging;

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

    /// <summary>Groups this client's tasks on the ComfyUI side. Random, per instance, and meaningless outside it.</summary>
    private readonly string clientId = Guid.NewGuid().ToString("N");

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
    /// <remarks>
    /// Three steps, in the order of the table of §E.7.2: submit the graph,
    /// poll the history until the task has an outcome, fetch the one image of
    /// the output node. The whole of it runs under
    /// <see cref="ComfyUiOptions.GenerationTimeout"/>, and every call under its
    /// own <see cref="ComfyUiOptions.RequestTimeout"/>.
    /// </remarks>
    public async Task<GeneratedImage> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var generation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        generation.CancelAfter(options.GenerationTimeout);

        string? promptId = null;

        try
        {
            promptId = await SubmitAsync(request, generation.Token).ConfigureAwait(false);
            ImageReference image = await WaitForImageAsync(promptId, generation.Token).ConfigureAwait(false);

            return await FetchAsync(image, generation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        {
            // The generation's own bound fired, not the caller's cancellation.
            // The task is dropped all the same: a task Pawnsmith has given up
            // on would otherwise keep the graphics card busy until it ends.
            await DropAsync(promptId).ConfigureAwait(false);

            throw new GeneratorException(
                GeneratorErrorCode.Timeout,
                $"The generator produced nothing within {options.GenerationTimeout.TotalMinutes:0.#} min (§E.9).",
                error);
        }
        catch (OperationCanceledException)
        {
            // The caller cancelled. The cancellation goes all the way to the
            // generator (§E.7.3), then propagates as itself: it is not an error.
            await DropAsync(promptId).ConfigureAwait(false);
            throw;
        }
    }

    public void Dispose() => http.Dispose();

    /// <summary><c>POST /prompt</c>: the substituted graph goes out, a task identifier comes back.</summary>
    private async Task<string> SubmitAsync(GenerationRequest request, CancellationToken cancellationToken)
    {
        // The prompt goes in as a JSON value, untouched (DEC-077). The client
        // identifier only lets ComfyUI group this client's tasks; it carries
        // nothing about the user.
        var body = new JsonObject
        {
            ["prompt"] = workflow.Substitute(request.Prompt, request.NegativePrompt, request.Seed),
            ["client_id"] = clientId,
        };

        using HttpResponseMessage response = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Post, "prompt")
            {
                Content = new StringContent(body.ToJsonString(), new MediaTypeHeaderValue("application/json")),
            },
            options.RequestTimeout,
            cancellationToken).ConfigureAwait(false);

        if ((int)response.StatusCode is >= 400 and < 500)
        {
            // The graph is wrong, a node is unknown, a model file is missing:
            // ComfyUI says which, and its words are the useful part.
            string detail = await ReadExcerptAsync(response, cancellationToken).ConfigureAwait(false);

            throw new GeneratorException(
                GeneratorErrorCode.Rejected,
                $"The generator refused the workflow ({(int)response.StatusCode}): {detail}");
        }

        RequireOk(response, "submission");

        JsonNode? answer = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);

        if (answer?["prompt_id"] is JsonValue value && value.TryGetValue(out string? promptId) && promptId.Length > 0)
        {
            return promptId;
        }

        throw new GeneratorException(
            GeneratorErrorCode.OutputInvalid,
            "The generator accepted the workflow but returned no task identifier.");
    }

    /// <summary><c>GET /history/{id}</c>, once per poll interval, until the task has an outcome.</summary>
    private async Task<ImageReference> WaitForImageAsync(string promptId, CancellationToken cancellationToken)
    {
        while (true)
        {
            JsonObject? entry = await ReadHistoryAsync(promptId, cancellationToken).ConfigureAwait(false);

            if (entry is not null)
            {
                return ImageOf(entry, promptId);
            }

            await Task.Delay(options.PollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>The history entry of the task, or null while it has none — queued or running.</summary>
    private async Task<JsonObject?> ReadHistoryAsync(string promptId, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, $"history/{Uri.EscapeDataString(promptId)}"),
            options.RequestTimeout,
            cancellationToken).ConfigureAwait(false);

        RequireOk(response, "history");

        JsonNode? history = await ReadJsonAsync(response, cancellationToken).ConfigureAwait(false);

        if (history is not JsonObject byPrompt)
        {
            throw new GeneratorException(GeneratorErrorCode.OutputInvalid, "The generator's history is not a JSON object.");
        }

        // ComfyUI answers {} until the task is over, then { "<id>": { … } }.
        return byPrompt[promptId] as JsonObject;
    }

    /// <summary>Reads the outcome of a finished task: an execution error, or exactly one image.</summary>
    private ImageReference ImageOf(JsonObject entry, string promptId)
    {
        if (entry["status"]?["status_str"] is JsonValue status
            && status.TryGetValue(out string? state)
            && state == "error")
        {
            throw new GeneratorException(
                GeneratorErrorCode.Failed,
                $"The generator ran task {promptId} and it ended in error: {Excerpt(entry["status"]?["messages"]?.ToJsonString())}");
        }

        var images = entry["outputs"]?[workflow.OutputNodeId]?["images"] as JsonArray;
        int count = images?.Count ?? 0;

        // Exactly one. Keeping the first of several would silently throw away
        // the work of the others; keeping them all would make several
        // candidates of one seed, which the model cannot represent (§E.7.2).
        if (count != 1)
        {
            throw new GeneratorException(
                GeneratorErrorCode.OutputInvalid,
                $"The output node '{workflow.OutputNodeId}' returned {count} image(s); exactly one is expected. " +
                "A latent batch size above 1 in the workflow produces several.");
        }

        JsonNode? image = images![0];
        string? fileName = Text(image?["filename"]);

        if (string.IsNullOrEmpty(fileName))
        {
            throw new GeneratorException(GeneratorErrorCode.OutputInvalid, "The generator named an image without a file name.");
        }

        return new ImageReference(fileName, Text(image?["subfolder"]) ?? string.Empty, Text(image?["type"]) ?? "output");
    }

    /// <summary><c>GET /view</c>: the bytes of the image, bounded and checked on their header.</summary>
    private async Task<GeneratedImage> FetchAsync(ImageReference image, CancellationToken cancellationToken)
    {
        // The file name came from the server. It only travels back to the
        // server, escaped, in a query string; it is never a local path - the
        // project's file is named by Pawnsmith (MEN-002, §E.7.2).
        string query =
            $"view?filename={Uri.EscapeDataString(image.FileName)}" +
            $"&subfolder={Uri.EscapeDataString(image.Subfolder)}" +
            $"&type={Uri.EscapeDataString(image.Type)}";

        using HttpResponseMessage response = await SendAsync(
            () => new HttpRequestMessage(HttpMethod.Get, query),
            options.RequestTimeout,
            cancellationToken).ConfigureAwait(false);

        RequireOk(response, "image");

        byte[] png = await ReadBoundedAsync(response, options.MaxImageBytes, "image", cancellationToken)
            .ConfigureAwait(false);

        return Check(png);
    }

    /// <summary>MEN-005, first layer: the header is read and bounded before the image is accepted.</summary>
    private GeneratedImage Check(byte[] png)
    {
        if (!PngHeader.TryRead(png, out int widthPx, out int heightPx))
        {
            throw new GeneratorException(GeneratorErrorCode.OutputInvalid, "The generator returned something that is not a PNG image.");
        }

        if (widthPx > options.MaxImageDimensionPx || heightPx > options.MaxImageDimensionPx)
        {
            throw new GeneratorException(
                GeneratorErrorCode.OutputInvalid,
                $"The generator returned an image of {widthPx} × {heightPx} pixels; " +
                $"no side may exceed {options.MaxImageDimensionPx} (§E.9).");
        }

        if (!PairSplit.IsSplittable(widthPx, heightPx))
        {
            throw new GeneratorException(
                GeneratorErrorCode.OutputInvalid,
                $"The generator returned an image of {widthPx} × {heightPx} pixels, which cannot be cut into two views (DEC-079).");
        }

        return new GeneratedImage(png, widthPx, heightPx);
    }

    /// <summary>
    /// Asks ComfyUI to forget a task: out of the queue if it is still waiting,
    /// interrupted if it is running (§E.7.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Best effort, and never in the way.</b> Both calls run under their own
    /// short bound — the health-check bound, since they are just as small — and
    /// on <see cref="CancellationToken.None"/>: under the caller's token they
    /// would be cancelled before leaving. Their failure is swallowed, because
    /// the cancellation or the timeout that led here is what the caller must
    /// hear about; a cleanup that failed changes nothing they can act on.
    /// </para>
    /// <para>
    /// Without them, a batch cancelled in Pawnsmith would keep the graphics card
    /// busy for the whole of the generation in progress, and a user relaunching
    /// at once would wait without understanding why.
    /// </para>
    /// <para>
    /// A known limit of old ComfyUI versions: they ignore the body of
    /// <c>/interrupt</c> and interrupt whatever is running. On a single-user
    /// machine that is nearly always Pawnsmith's task; the risk is accepted and
    /// written down, not worked around.
    /// </para>
    /// </remarks>
    /// <param name="promptId">The task to drop, or null when it was never submitted — then there is nothing to do.</param>
    private async Task DropAsync(string? promptId)
    {
        if (promptId is null)
        {
            return;
        }

        await PostQuietlyAsync("queue", new JsonObject { ["delete"] = new JsonArray(promptId) }).ConfigureAwait(false);
        await PostQuietlyAsync("interrupt", new JsonObject { ["prompt_id"] = promptId }).ConfigureAwait(false);
    }

    private async Task PostQuietlyAsync(string path, JsonObject body)
    {
        try
        {
            using HttpResponseMessage response = await SendAsync(
                () => new HttpRequestMessage(HttpMethod.Post, path)
                {
                    Content = new StringContent(body.ToJsonString(), new MediaTypeHeaderValue("application/json")),
                },
                options.CheckTimeout,
                CancellationToken.None).ConfigureAwait(false);
        }
        catch (GeneratorException)
        {
            // Unreachable or slow: the task will end on its own. See DropAsync.
        }
    }

    /// <summary>Anything but 200 is a failure of the generator; a redirect is never followed (DEC-081).</summary>
    private static void RequireOk(HttpResponseMessage response, string what)
    {
        if (response.StatusCode == HttpStatusCode.OK)
        {
            return;
        }

        string why = (int)response.StatusCode is >= 300 and < 400
            ? "a redirect, which is never followed (DEC-081)"
            : $"status {(int)response.StatusCode}";

        throw new GeneratorException(GeneratorErrorCode.Failed, $"The generator answered the {what} request with {why}.");
    }

    /// <summary>The start of an error body, for a message. Bounded like any other read.</summary>
    private async Task<string> ReadExcerptAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        byte[] bytes = await ReadBoundedAsync(response, options.MaxResponseJsonBytes, "error answer", cancellationToken)
            .ConfigureAwait(false);

        return Excerpt(System.Text.Encoding.UTF8.GetString(bytes));
    }

    private static string Excerpt(string? text)
    {
        const int Shown = 500;

        if (string.IsNullOrWhiteSpace(text))
        {
            return "(no detail)";
        }

        return text.Length <= Shown ? text : text[..Shown] + "…";
    }

    private static string? Text(JsonNode? node) =>
        node is JsonValue value && value.TryGetValue(out string? text) ? text : null;

    /// <summary>Where ComfyUI keeps an image it produced: enough to ask for it, nothing more.</summary>
    private sealed record ImageReference(string FileName, string Subfolder, string Type);

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
