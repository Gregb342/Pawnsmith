using System.Text.Json.Nodes;

using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Generation;

/// <summary>What the fake ComfyUI does with one submitted task.</summary>
public enum FakeOutcome
{
    /// <summary>Finishes after a couple of polls, with one image.</summary>
    Success,

    /// <summary><c>/prompt</c> answers 400, as for an unknown node.</summary>
    Rejected,

    /// <summary>The history reports an execution error.</summary>
    ExecutionError,

    /// <summary>Finishes with no image on the output node.</summary>
    NoImage,

    /// <summary>Finishes with two images on the output node.</summary>
    TwoImages,

    /// <summary>Never finishes: the history stays empty.</summary>
    NeverFinishes,
}

/// <summary>
/// A scripted ComfyUI on top of <see cref="FakeHttpServer"/>, speaking the
/// five calls of §E.7.2 and recording what it received.
/// </summary>
/// <remarks>
/// Task <i>n</i> (counting submissions from zero) follows
/// <see cref="Outcome"/>(<i>n</i>) and, on success, serves
/// <see cref="Image"/>(<i>n</i>). Both default to the ordinary case, so a test
/// only states what is unusual about it.
/// </remarks>
internal sealed class FakeComfyUi : IAsyncDisposable
{
    /// <summary>The output node of <see cref="WorkflowFixture.Graph"/>.</summary>
    public const string OutputNodeId = "9";

    private readonly FakeHttpServer server;
    private readonly List<JsonObject> submitted = [];
    private readonly Dictionary<string, int> polls = [];

    public FakeComfyUi()
    {
        server = new FakeHttpServer(Handle);
    }

    /// <summary>What happens to task <i>n</i>.</summary>
    public Func<int, FakeOutcome> Outcome { get; set; } = _ => FakeOutcome.Success;

    /// <summary>The bytes served for task <i>n</i>.</summary>
    public Func<int, byte[]> Image { get; set; } = _ => TestPng.Create(16, 8);

    /// <summary>Whether images are sent without a declared length.</summary>
    public bool ChunkedImages { get; set; }

    /// <summary>How many empty histories a successful task answers before its result.</summary>
    public int PollsBeforeDone { get; set; } = 2;

    /// <summary>Called when a task is submitted, before the answer goes back. Lets a test act mid-batch.</summary>
    public Action<int>? OnSubmitted { get; set; }

    public string BaseAddress => server.BaseAddress;

    public IReadOnlyList<FakeRequest> Requests => server.Requests;

    /// <summary>The graphs received by <c>/prompt</c>, in order, as parsed JSON.</summary>
    public IReadOnlyList<JsonObject> Submitted
    {
        get
        {
            lock (submitted)
            {
                return [.. submitted];
            }
        }
    }

    public ValueTask DisposeAsync() => server.DisposeAsync();

    private FakeResponse Handle(FakeRequest request)
    {
        return (request.Method, request.Path) switch
        {
            ("GET", "/system_stats") => FakeResponse.Json("""{ "system": {}, "devices": [] }"""),
            ("POST", "/prompt") => Submit(request),
            ("GET", var path) when path.StartsWith("/history/", StringComparison.Ordinal) =>
                History(Uri.UnescapeDataString(path["/history/".Length..])),
            ("GET", "/view") => View(request),
            ("POST", "/queue") or ("POST", "/interrupt") => FakeResponse.Json("{}"),
            _ => FakeResponse.Json("""{ "error": "not found" }""", status: 404),
        };
    }

    private FakeResponse Submit(FakeRequest request)
    {
        var body = (JsonObject)JsonNode.Parse(request.Body)!;
        int index;

        lock (submitted)
        {
            index = submitted.Count;
            submitted.Add((JsonObject)body["prompt"]!);
        }

        OnSubmitted?.Invoke(index);

        if (Outcome(index) == FakeOutcome.Rejected)
        {
            return FakeResponse.Json("""{ "error": { "type": "invalid_prompt", "message": "Cannot execute because node Krea2Loader does not exist." } }""", status: 400);
        }

        return FakeResponse.Json($$"""{ "prompt_id": "task-{{index}}", "number": {{index}}, "node_errors": {} }""");
    }

    private FakeResponse History(string promptId)
    {
        int index = int.Parse(promptId["task-".Length..], System.Globalization.CultureInfo.InvariantCulture);
        FakeOutcome outcome = Outcome(index);

        int seen;

        lock (polls)
        {
            polls[promptId] = seen = polls.GetValueOrDefault(promptId) + 1;
        }

        if (outcome == FakeOutcome.NeverFinishes || seen <= PollsBeforeDone)
        {
            return FakeResponse.Json("{}");
        }

        JsonObject images = outcome switch
        {
            FakeOutcome.NoImage => [],
            FakeOutcome.TwoImages => new JsonObject { ["images"] = new JsonArray(Picture(index, 0), Picture(index, 1)) },
            _ => new JsonObject { ["images"] = new JsonArray(Picture(index, 0)) },
        };

        var entry = new JsonObject
        {
            ["prompt"] = new JsonArray(),
            ["outputs"] = outcome == FakeOutcome.NoImage ? new JsonObject() : new JsonObject { [OutputNodeId] = images },
            ["status"] = new JsonObject
            {
                ["status_str"] = outcome == FakeOutcome.ExecutionError ? "error" : "success",
                ["completed"] = outcome != FakeOutcome.ExecutionError,
                ["messages"] = new JsonArray("execution_error: CUDA out of memory"),
            },
        };

        return FakeResponse.Json(new JsonObject { [promptId] = entry }.ToJsonString());
    }

    private FakeResponse View(FakeRequest request)
    {
        // pawnsmith_00042_.png -> task 42, the way ComfyUI numbers its files.
        string query = request.Target.Split('?')[1];
        string fileName = Uri.UnescapeDataString(query.Split('&')[0]["filename=".Length..]);
        int index = int.Parse(fileName.Split('_')[1], System.Globalization.CultureInfo.InvariantCulture);

        return FakeResponse.Png(Image(index)) with { Chunked = ChunkedImages };
    }

    private static JsonObject Picture(int index, int which) => new()
    {
        ["filename"] = $"pawnsmith_{index:D5}_{which}.png",
        ["subfolder"] = "",
        ["type"] = "output",
    };
}
