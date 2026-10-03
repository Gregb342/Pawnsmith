using System.Text.Json.Nodes;

using Pawnsmith.Application.Ports;
using Pawnsmith.Infrastructure.Generation;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Generation;

/// <summary>
/// Covers tests 22 to 28 and 30 of E.12: one generation against the fake
/// ComfyUI.
/// </summary>
public class GeneratorGenerationTests
{
    private static readonly GenerationRequest Request = new("a goblin skirmisher", "blurry", 123456789UL);

    /// <summary>A client with short delays, so that a test of a timeout lasts a fraction of a second.</summary>
    private static ComfyUiImageGenerator Client(string address, Func<ComfyUiOptions, ComfyUiOptions>? tune = null)
    {
        var options = new ComfyUiOptions(address)
        {
            PollInterval = TimeSpan.FromMilliseconds(10),
            RequestTimeout = TimeSpan.FromSeconds(5),
            GenerationTimeout = TimeSpan.FromSeconds(10),
        };

        return new ComfyUiImageGenerator(
            tune is null ? options : tune(options),
            WorkflowTemplateReader.Parse(WorkflowFixture.File(), "workflow.test.json"));
    }

    private static async Task<GeneratorException> FailsWith(FakeComfyUi comfy, GeneratorErrorCode code)
    {
        using ComfyUiImageGenerator client = Client(comfy.BaseAddress);

        GeneratorException error = await Should.ThrowAsync<GeneratorException>(() =>
            client.GenerateAsync(Request, CancellationToken.None));

        error.Code.ShouldBe(code);
        return error;
    }

    // --- E.12 n° 22 : soumettre, interroger, rapatrier ---------------------------

    [Fact]
    public async Task AGenerationSubmitsTheGraphPollsTheHistoryAndBringsBackTheImage()
    {
        await using var comfy = new FakeComfyUi { Image = _ => TestPng.Create(20, 10) };
        using ComfyUiImageGenerator client = Client(comfy.BaseAddress);

        GeneratedImage image = await client.GenerateAsync(Request, CancellationToken.None);

        image.Png.ShouldBe(TestPng.Create(20, 10));
        image.WidthPx.ShouldBe(20);
        image.HeightPx.ShouldBe(10);

        // The substituted graph is what went out.
        JsonObject graph = comfy.Submitted.Single();
        WorkflowFixture.Input(graph, "4", "text")!.GetValue<string>().ShouldBe("a goblin skirmisher");
        WorkflowFixture.Input(graph, "5", "text")!.GetValue<string>().ShouldBe("blurry");
        WorkflowFixture.Input(graph, "7", "seed")!.GetValue<ulong>().ShouldBe(123456789UL);

        // The order of §E.7.2: one submission, polls until done, one fetch.
        string[] calls = [.. comfy.Requests.Select(request => $"{request.Method} {request.Path}")];
        calls[0].ShouldBe("POST /prompt");
        calls[1..^1].ShouldAllBe(call => call == "GET /history/task-0");
        calls.Length.ShouldBe(1 + 3 + 1);
        calls[^1].ShouldBe("GET /view");
        comfy.Requests[^1].Target.ShouldBe("/view?filename=pawnsmith_00000_0.png&subfolder=&type=output");
    }

    [Fact]
    public async Task TheFileNameGivenByTheServerIsOnlyEverSentBackEscaped()
    {
        // A hostile server names its image with a path. It is asked for again,
        // escaped, and nothing else happens with it (MEN-002).
        await using var server = new FakeHttpServer(request => (request.Method, request.Path) switch
        {
            ("POST", "/prompt") => FakeResponse.Json("""{ "prompt_id": "x" }"""),
            ("GET", "/history/x") => FakeResponse.Json("""
                { "x": { "status": { "status_str": "success" },
                         "outputs": { "9": { "images": [ { "filename": "../../etc/passwd", "subfolder": "a&b", "type": "output" } ] } } } }
                """),
            ("GET", "/view") => FakeResponse.Png(TestPng.Create(4, 4)),
            _ => FakeResponse.Json("{}", status: 404),
        });

        using ComfyUiImageGenerator client = Client(server.BaseAddress);

        await client.GenerateAsync(Request, CancellationToken.None);

        server.Requests[^1].Target.ShouldBe("/view?filename=..%2F..%2Fetc%2Fpasswd&subfolder=a%26b&type=output");
    }

    // --- E.12 n° 23 : port fermé ---------------------------------------------------

    [Fact]
    public async Task AClosedPortIsUnreachable()
    {
        using ComfyUiImageGenerator client = Client(FakeHttpServer.ClosedAddress());

        GeneratorException error = await Should.ThrowAsync<GeneratorException>(() =>
            client.GenerateAsync(Request, CancellationToken.None));

        error.Code.ShouldBe(GeneratorErrorCode.Unreachable);
        error.WireCode.ShouldBe("GENERATOR_UNREACHABLE");
    }

    // --- E.12 n° 24 : /prompt en 400 ------------------------------------------------

    [Fact]
    public async Task ARefusedWorkflowIsRejectedWithTheGeneratorsOwnWords()
    {
        await using var comfy = new FakeComfyUi { Outcome = _ => FakeOutcome.Rejected };

        GeneratorException error = await FailsWith(comfy, GeneratorErrorCode.Rejected);

        error.Message.ShouldContain("Krea2Loader does not exist");
    }

    // --- E.12 n° 25 : erreur d'exécution ----------------------------------------------

    [Fact]
    public async Task AnExecutionErrorIsAFailure()
    {
        await using var comfy = new FakeComfyUi { Outcome = _ => FakeOutcome.ExecutionError };

        GeneratorException error = await FailsWith(comfy, GeneratorErrorCode.Failed);

        error.Message.ShouldContain("CUDA out of memory");
    }

    [Fact]
    public async Task AServerErrorIsAFailure()
    {
        await using var server = new FakeHttpServer(_ => FakeResponse.Json("""{ "error": "boom" }""", status: 500));
        using ComfyUiImageGenerator client = Client(server.BaseAddress);

        GeneratorException error = await Should.ThrowAsync<GeneratorException>(() =>
            client.GenerateAsync(Request, CancellationToken.None));

        error.Code.ShouldBe(GeneratorErrorCode.Failed);
    }

    // --- E.12 n° 26 : aucun résultat dans le délai -------------------------------------

    [Fact]
    public async Task ATaskThatNeverFinishesTimesOut()
    {
        await using var comfy = new FakeComfyUi { Outcome = _ => FakeOutcome.NeverFinishes };
        using ComfyUiImageGenerator client = Client(
            comfy.BaseAddress,
            options => options with { GenerationTimeout = TimeSpan.FromMilliseconds(300) });

        GeneratorException error = await Should.ThrowAsync<GeneratorException>(() =>
            client.GenerateAsync(Request, CancellationToken.None));

        error.Code.ShouldBe(GeneratorErrorCode.Timeout);
        error.WireCode.ShouldBe("GENERATOR_TIMEOUT");
    }

    [Fact]
    public async Task ACallThatNeverAnswersTimesOut()
    {
        await using var server = new FakeHttpServer(async _ =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            return FakeResponse.Json("{}");
        });

        using ComfyUiImageGenerator client = Client(
            server.BaseAddress,
            options => options with { RequestTimeout = TimeSpan.FromMilliseconds(200) });

        GeneratorException error = await Should.ThrowAsync<GeneratorException>(() =>
            client.GenerateAsync(Request, CancellationToken.None));

        error.Code.ShouldBe(GeneratorErrorCode.Timeout);
    }

    // --- E.12 n° 27 : zéro image, deux images, pas un PNG ------------------------------

    [Theory]
    [InlineData(FakeOutcome.NoImage, "0 image(s)")]
    [InlineData(FakeOutcome.TwoImages, "2 image(s)")]
    public async Task AnythingButOneImageIsInvalid(FakeOutcome outcome, string said)
    {
        await using var comfy = new FakeComfyUi { Outcome = _ => outcome };

        GeneratorException error = await FailsWith(comfy, GeneratorErrorCode.OutputInvalid);

        error.Message.ShouldContain(said);
        comfy.Requests.ShouldNotContain(request => request.Path == "/view");
    }

    [Fact]
    public async Task BytesThatAreNotAPngAreInvalid()
    {
        await using var comfy = new FakeComfyUi { Image = _ => "<html>error</html>"u8.ToArray() };

        GeneratorException error = await FailsWith(comfy, GeneratorErrorCode.OutputInvalid);

        error.Message.ShouldContain("not a PNG");
    }

    [Fact]
    public async Task AnImageThatCannotBeCutInTwoIsInvalid()
    {
        await using var comfy = new FakeComfyUi { Image = _ => TestPng.Create(1, 8) };

        GeneratorException error = await FailsWith(comfy, GeneratorErrorCode.OutputInvalid);

        error.Message.ShouldContain("DEC-079");
    }

    [Fact]
    public async Task AnAnswerThatIsNotJsonIsInvalid()
    {
        await using var server = new FakeHttpServer(_ => new FakeResponse(200, "<html>proxy page</html>"u8.ToArray(), "text/html"));
        using ComfyUiImageGenerator client = Client(server.BaseAddress);

        GeneratorException error = await Should.ThrowAsync<GeneratorException>(() =>
            client.GenerateAsync(Request, CancellationToken.None));

        error.Code.ShouldBe(GeneratorErrorCode.OutputInvalid);
    }

    // --- E.12 n° 28 : bornes de taille, vérifiées sur l'en-tête -------------------------

    [Fact]
    public async Task AnImageHeavierThanTheBoundIsRefused()
    {
        await using var comfy = new FakeComfyUi { Image = _ => TestPng.Create(64, 64) };
        using ComfyUiImageGenerator client = Client(comfy.BaseAddress, options => options with { MaxImageBytes = 50 });

        GeneratorException error = await Should.ThrowAsync<GeneratorException>(() =>
            client.GenerateAsync(Request, CancellationToken.None));

        error.Code.ShouldBe(GeneratorErrorCode.OutputInvalid);
        error.Message.ShouldContain("50 bytes");
    }

    [Fact]
    public async Task AnImageWithNoDeclaredLengthIsBoundedWhileItIsRead()
    {
        // No Content-Length: the shortcut cannot refuse it, so only the check
        // made during the read stands between the server and the memory.
        await using var comfy = new FakeComfyUi { Image = _ => TestPng.Create(64, 64), ChunkedImages = true };
        using ComfyUiImageGenerator client = Client(comfy.BaseAddress, options => options with { MaxImageBytes = 50 });

        GeneratorException error = await Should.ThrowAsync<GeneratorException>(() =>
            client.GenerateAsync(Request, CancellationToken.None));

        error.Code.ShouldBe(GeneratorErrorCode.OutputInvalid);
        error.Message.ShouldContain("50 bytes");
    }

    [Fact]
    public async Task AnImageWithNoDeclaredLengthUnderTheBoundArrivesWhole()
    {
        await using var comfy = new FakeComfyUi { Image = _ => TestPng.Create(64, 64), ChunkedImages = true };
        using ComfyUiImageGenerator client = Client(comfy.BaseAddress);

        GeneratedImage image = await client.GenerateAsync(Request, CancellationToken.None);

        image.Png.ShouldBe(TestPng.Create(64, 64));
    }

    [Theory]
    [InlineData(8193, 832)]
    [InlineData(1216, 8193)]
    [InlineData(100_000, 100_000)]
    public async Task AnImageLargerThanTheDimensionBoundIsRefusedOnItsHeader(int widthPx, int heightPx)
    {
        // Twenty-four bytes claiming an enormous image: refused on what it
        // declares, before anything would try to decode it (MEN-005).
        await using var comfy = new FakeComfyUi { Image = _ => TestPng.HeaderOnly(widthPx, heightPx) };

        GeneratorException error = await FailsWith(comfy, GeneratorErrorCode.OutputInvalid);

        error.Message.ShouldContain("8192");
    }

    [Fact]
    public async Task ATooLargeJsonAnswerIsRefused()
    {
        await using var comfy = new FakeComfyUi();
        using ComfyUiImageGenerator client = Client(comfy.BaseAddress, options => options with { MaxResponseJsonBytes = 10 });

        GeneratorException error = await Should.ThrowAsync<GeneratorException>(() =>
            client.GenerateAsync(Request, CancellationToken.None));

        error.Code.ShouldBe(GeneratorErrorCode.OutputInvalid);
    }

    // --- E.12 n° 30 : une redirection n'est pas suivie -----------------------------------

    [Fact]
    public async Task ARedirectIsNotFollowed()
    {
        await using var comfy = new FakeComfyUi();
        await using var redirecting = new FakeHttpServer(_ => FakeResponse.Redirect(comfy.BaseAddress + "prompt"));
        using ComfyUiImageGenerator client = Client(redirecting.BaseAddress);

        GeneratorException error = await Should.ThrowAsync<GeneratorException>(() =>
            client.GenerateAsync(Request, CancellationToken.None));

        error.Code.ShouldBe(GeneratorErrorCode.Failed);
        error.Message.ShouldContain("redirect");
        comfy.Requests.ShouldBeEmpty();
    }

    // --- E.12 n° 29 : l'annulation va jusqu'au générateur -----------------------------

    [Fact]
    public async Task ACancellationDropsTheTaskFromTheQueueAndInterruptsItByItsIdentifier()
    {
        await using var comfy = new FakeComfyUi { Outcome = _ => FakeOutcome.NeverFinishes };
        using ComfyUiImageGenerator client = Client(comfy.BaseAddress);
        using var cancellation = new CancellationTokenSource();

        Task<GeneratedImage> generation = client.GenerateAsync(Request, cancellation.Token);

        // Cancel once the task is known to be waiting at the generator.
        await WaitUntil(() => comfy.Requests.Any(request => request.Path == "/history/task-0"));
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => generation);

        FakeRequest drop = comfy.Requests.Single(request => request.Path == "/queue");
        drop.Method.ShouldBe("POST");
        JsonNode.Parse(drop.Body)!["delete"]!.AsArray().Select(id => id!.GetValue<string>()).ShouldBe(["task-0"]);

        FakeRequest interrupt = comfy.Requests.Single(request => request.Path == "/interrupt");
        JsonNode.Parse(interrupt.Body)!["prompt_id"]!.GetValue<string>().ShouldBe("task-0");
    }

    [Fact]
    public async Task AGenerationThatTimesOutIsDroppedAtTheGeneratorToo()
    {
        await using var comfy = new FakeComfyUi { Outcome = _ => FakeOutcome.NeverFinishes };
        using ComfyUiImageGenerator client = Client(
            comfy.BaseAddress,
            options => options with { GenerationTimeout = TimeSpan.FromMilliseconds(300) });

        await Should.ThrowAsync<GeneratorException>(() => client.GenerateAsync(Request, CancellationToken.None));

        comfy.Requests.ShouldContain(request => request.Path == "/queue");
        comfy.Requests.ShouldContain(request => request.Path == "/interrupt");
    }

    [Fact]
    public async Task ACancellationIsStillACancellationWhenTheCleanupFails()
    {
        // A server that accepts the task and then answers nothing else in time:
        // the cleanup calls time out, and the caller still hears a cancellation.
        int submitted = 0;
        await using var server = new FakeHttpServer(async request =>
        {
            if (request.Path == "/prompt" && Interlocked.Exchange(ref submitted, 1) == 0)
            {
                return FakeResponse.Json("""{ "prompt_id": "x" }""");
            }

            if (request.Path.StartsWith("/history", StringComparison.Ordinal))
            {
                return FakeResponse.Json("{}");
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
            return FakeResponse.Json("{}");
        });

        using ComfyUiImageGenerator client = Client(server.BaseAddress, options => options with { CheckTimeout = TimeSpan.FromMilliseconds(100) });
        using var cancellation = new CancellationTokenSource();

        Task<GeneratedImage> generation = client.GenerateAsync(Request, cancellation.Token);
        await WaitUntil(() => server.Requests.Any(request => request.Path == "/history/x"));
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => generation);
    }

    [Fact]
    public async Task ACancellationBeforeAnythingWasSubmittedCallsNothing()
    {
        await using var comfy = new FakeComfyUi();
        using ComfyUiImageGenerator client = Client(comfy.BaseAddress);

        await Should.ThrowAsync<OperationCanceledException>(() =>
            client.GenerateAsync(Request, new CancellationToken(canceled: true)));

        comfy.Requests.ShouldBeEmpty();
    }

    private static async Task WaitUntil(Func<bool> condition)
    {
        for (int attempt = 0; attempt < 500 && !condition(); attempt++)
        {
            await Task.Delay(10);
        }

        condition().ShouldBeTrue("the fake server never saw the expected request");
    }

    // --- Relecture : une réponse de forme inattendue reste un refus codé -------------

    [Theory]
    [InlineData("""[ "not", "an", "object" ]""", null)]
    [InlineData("""{ "prompt_id": { "nested": true } }""", null)]
    [InlineData("""{ "prompt_id": "x", "prompt_id": "y" }""", null)]
    [InlineData("""{ "prompt_id": "x" }""", """{ "x": { "status": "error", "outputs": { } } }""")]
    [InlineData("""{ "prompt_id": "x" }""", """{ "x": { "outputs": [ 1, 2 ] } }""")]
    [InlineData("""{ "prompt_id": "x" }""", """{ "x": { "outputs": { "9": { "images": [ "just-a-name.png" ] } } } }""")]
    [InlineData("""{ "prompt_id": "x" }""", """{ "x": { "outputs": { "9": "nothing" } } }""")]
    [InlineData("""{ "prompt_id": "x" }""", """{ "x": { }, "x": { } }""")]
    [InlineData("""{ "prompt_id": "x" }""", """{ "x": "finished" }""")]
    public async Task AnAnswerOfAnUnexpectedShapeIsACodedRefusal(string submitAnswer, string? history)
    {
        await using var server = new FakeHttpServer(request => (request.Method, request.Path) switch
        {
            ("POST", "/prompt") => FakeResponse.Json(submitAnswer),
            ("GET", "/history/x") => FakeResponse.Json(history ?? "{}"),
            _ => FakeResponse.Png(TestPng.Create(4, 4)),
        });

        using ComfyUiImageGenerator client = Client(server.BaseAddress);

        // A generator is an untrusted input: whatever shape it answers with,
        // the outcome is a GeneratorException with a code - never an exception
        // nobody planned for, which would end a job as JOB_UNEXPECTED_ERROR.
        GeneratorException error = await Should.ThrowAsync<GeneratorException>(() =>
            client.GenerateAsync(Request, CancellationToken.None));

        error.Code.ShouldBe(GeneratorErrorCode.OutputInvalid);
    }
}
