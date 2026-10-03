using System.Net;
using System.Text.Json.Nodes;

using Pawnsmith.Api.Tests.Fixtures;

namespace Pawnsmith.Api.Tests;

/// <summary>
/// The batch queue. Covers tests 17 to 22 of G.13.
/// </summary>
public class JobRoutesTests
{
    private static async Task<JsonNode> StartAsync(ApiHarness api, string folder, Guid blueprint, object body)
    {
        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Post, $"/api/projects/{folder}/blueprints/{blueprint}/jobs", body);

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
    }

    /// <summary>Polls a job until <paramref name="until"/> holds, as a client would.</summary>
    private static async Task<JsonNode> WaitForAsync(ApiHarness api, Guid job, Func<JsonNode, bool> until)
    {
        for (int attempt = 0; attempt < 500; attempt++)
        {
            JsonNode current = await api.GetJsonAsync($"/api/jobs/{job}");

            if (until(current))
            {
                return current;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException($"Job {job} never reached the expected state.");
    }

    private static string State(JsonNode job) => job["state"]!.GetValue<string>();

    private static Guid Id(JsonNode job) => job["id"]!.GetValue<Guid>();

    // --- G.13 n° 17 : un lot se lance, se termine, et ses candidats sont là --------------

    [Fact]
    public async Task ABatchIsAcceptedQueuedRunAndItsCandidatesLandInTheProject()
    {
        var generator = new FakeGenerator();
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        JsonNode accepted = await StartAsync(api, folder, blueprint, new { seeds = new[] { "42", "18446744073709551615" } });
        accepted["requested"]!.GetValue<int>().ShouldBe(2);

        JsonNode finished = await WaitForAsync(api, Id(accepted), job => State(job) == "Completed");
        finished["produced"]!.AsArray().Count.ShouldBe(2);
        finished["failureCode"].ShouldBeNull();

        JsonArray candidates = (await api.GetJsonAsync($"/api/projects/{folder}"))["blueprints"]![0]!["candidates"]!.AsArray();
        candidates.Select(candidate => candidate!["seed"]!.GetValue<string>()).ShouldBe(["42", "18446744073709551615"]);
        candidates.ShouldAllBe(candidate => candidate!["status"]!.GetValue<string>() == "Draft");

        JsonNode list = await api.GetJsonAsync("/api/jobs");
        Id(list.AsArray().Single()!).ShouldBe(Id(accepted));
    }

    [Fact]
    public async Task ACountDrawsThatManySeeds()
    {
        var generator = new FakeGenerator();
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        JsonNode accepted = await StartAsync(api, folder, blueprint, new { count = 3 });

        (await WaitForAsync(api, Id(accepted), job => State(job) == "Completed"))["produced"]!.AsArray().Count.ShouldBe(3);
    }

    // --- G.13 n° 18 : un lot trop grand -----------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(1_000_000_000)]
    public async Task ABatchOfTheWrongSizeIsRefusedAndNoJobExists(int count)
    {
        var generator = new FakeGenerator();
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Post, $"/api/projects/{folder}/blueprints/{blueprint}/jobs", new { count });

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.BadRequest, "BATCH_SIZE_INVALID"));
        (await api.GetJsonAsync("/api/jobs")).AsArray().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{ }""")]
    [InlineData("""{ "count": 2, "seeds": ["1"] }""")]
    [InlineData("""{ "seeds": ["-1"] }""")]
    [InlineData("""{ "seeds": ["1e3"] }""")]
    public async Task AJobRequestThatSaysNeitherOrBothOrABadSeedIsRequestInvalid(string body)
    {
        var generator = new FakeGenerator();
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await api.Client.PostAsync($"/api/projects/{folder}/blueprints/{blueprint}/jobs", content);

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.BadRequest, "REQUEST_INVALID"));
    }

    [Fact]
    public async Task AnUnknownBlueprintIsRefusedAndNoJobExists()
    {
        var generator = new FakeGenerator();
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);

        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Post, $"/api/projects/{folder}/blueprints/{Guid.NewGuid()}/jobs", new { count = 1 });

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.NotFound, "BLUEPRINT_NOT_FOUND"));
        (await api.GetJsonAsync("/api/jobs")).AsArray().ShouldBeEmpty();
    }

    // --- G.13 n° 19 : sans générateur configuré -----------------------------------------------

    [Fact]
    public async Task WithoutAGeneratorAJobIsRefused()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Post, $"/api/projects/{folder}/blueprints/{blueprint}/jobs", new { count = 1 });

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.ServiceUnavailable, "GENERATOR_NOT_CONFIGURED"));
    }

    // --- G.13 n° 20 : annuler un lot en file ---------------------------------------------------

    [Fact]
    public async Task AQueuedJobCancelledNeverRuns()
    {
        var generator = new FakeGenerator { Hold = new TaskCompletionSource() };
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        // The first job holds the worker; the second waits behind it.
        JsonNode first = await StartAsync(api, folder, blueprint, new { count = 1 });
        await WaitForAsync(api, Id(first), job => State(job) == "Running");
        JsonNode second = await StartAsync(api, folder, blueprint, new { count = 1 });

        JsonNode cancelled = await api.SendJsonAsync(HttpMethod.Post, $"/api/jobs/{Id(second)}/cancel", body: null);
        State(cancelled).ShouldBe("Cancelled");

        generator.Hold.SetResult();
        await WaitForAsync(api, Id(first), job => State(job) == "Completed");

        // The worker moved past the cancelled job without running it.
        await Task.Delay(100);
        generator.Started.ShouldBe(1);
        State(await api.GetJsonAsync($"/api/jobs/{Id(second)}")).ShouldBe("Cancelled");
    }

    [Fact]
    public async Task ARunningJobCancelledKeepsWhatItProduced()
    {
        // The first image goes through, the second is held at the generator.
        var generator = new FakeGenerator { Hold = new TaskCompletionSource(), HoldFromCall = 1 };
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        JsonNode job = await StartAsync(api, folder, blueprint, new { count = 3 });
        await WaitForAsync(api, Id(job), current => current["produced"]!.AsArray().Count == 1);

        await api.SendJsonAsync(HttpMethod.Post, $"/api/jobs/{Id(job)}/cancel", body: null);

        JsonNode finished = await WaitForAsync(api, Id(job), current => State(current) == "Cancelled");
        finished["produced"]!.AsArray().Count.ShouldBe(1);
        (await api.GetJsonAsync($"/api/projects/{folder}"))["blueprints"]![0]!["candidates"]!.AsArray().Count.ShouldBe(1);
        generator.Started.ShouldBe(2);
    }

    [Fact]
    public async Task AFinishedJobCannotBeCancelledAndAnUnknownOneIsNotFound()
    {
        var generator = new FakeGenerator();
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);
        JsonNode job = await StartAsync(api, folder, blueprint, new { count = 1 });
        await WaitForAsync(api, Id(job), current => State(current) == "Completed");

        using HttpResponseMessage finished = await api.SendAsync(HttpMethod.Post, $"/api/jobs/{Id(job)}/cancel", body: null);
        (await ApiHarness.ErrorOf(finished)).ShouldBe((HttpStatusCode.Conflict, "JOB_ALREADY_FINISHED"));

        using HttpResponseMessage unknown = await api.Client.GetAsync($"/api/jobs/{Guid.NewGuid()}");
        (await ApiHarness.ErrorOf(unknown)).ShouldBe((HttpStatusCode.NotFound, "JOB_NOT_FOUND"));
    }

    // --- G.13 n° 21 : deux lots ne tournent jamais ensemble -----------------------------------

    [Fact]
    public async Task TwoBatchesNeverRunAtTheSameTime()
    {
        var generator = new FakeGenerator { Hold = new TaskCompletionSource() };
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        JsonNode first = await StartAsync(api, folder, blueprint, new { count = 2 });
        JsonNode second = await StartAsync(api, folder, blueprint, new { count = 2 });

        await WaitForAsync(api, Id(first), job => State(job) == "Running");
        await Task.Delay(100);
        State(await api.GetJsonAsync($"/api/jobs/{Id(second)}")).ShouldBe("Queued");

        generator.Hold.SetResult();
        await WaitForAsync(api, Id(second), job => State(job) == "Completed");

        generator.MostAtOnce.ShouldBe(1);
        generator.Started.ShouldBe(4);
    }

    // --- G.13 n° 22 : une modification pendant un lot n'est pas perdue -------------------------

    [Fact]
    public async Task AChangeMadeWhileABatchRunsIsKept()
    {
        var generator = new FakeGenerator { Hold = new TaskCompletionSource() };
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        JsonNode job = await StartAsync(api, folder, blueprint, new { count = 1 });
        await WaitForAsync(api, Id(job), current => State(current) == "Running");

        // While the image is being generated, the user edits the blueprint.
        await api.SendJsonAsync(HttpMethod.Put, $"/api/projects/{folder}/blueprints/{blueprint}/subject-clause", new { clause = "a goblin with a red hood" });

        generator.Hold.SetResult();
        await WaitForAsync(api, Id(job), current => State(current) == "Completed");

        JsonNode saved = (await api.GetJsonAsync($"/api/projects/{folder}"))["blueprints"]![0]!;
        saved["subjectClause"]!.GetValue<string>().ShouldBe("a goblin with a red hood");
        saved["candidates"]!.AsArray().Count.ShouldBe(1);
    }

    [Fact]
    public async Task AHugeListOfSeedsIsRefusedWhileItArrives()
    {
        // A hundred thousand seeds is two megabytes of JSON: refused by the
        // server's body bound before anything reads it (MEN-007).
        var generator = new FakeGenerator();
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        string[] seeds = [.. Enumerable.Range(0, 100_000).Select(seed => $"{seed:D15}")];
        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Post, $"/api/projects/{folder}/blueprints/{blueprint}/jobs", new { seeds });

        // Kestrel answers this one itself, and discards the body the error
        // middleware writes: a 413 with no code, as section G.3 records.
        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
        (await api.GetJsonAsync("/api/jobs")).AsArray().ShouldBeEmpty();
    }
}
