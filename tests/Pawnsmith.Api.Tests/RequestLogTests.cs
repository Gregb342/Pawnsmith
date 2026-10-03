using System.Net;
using System.Text.Json.Nodes;

using Pawnsmith.Api.Tests.Fixtures;
using Pawnsmith.Application.Ports;

namespace Pawnsmith.Api.Tests;

/// <summary>
/// What reaches the log when a request fails, or a batch runs. Covers tests 4
/// to 7 of H.8.
/// </summary>
public class RequestLogTests
{
    private static string Level(JsonNode logEvent) => logEvent["Level"]!.GetValue<string>();

    private static async Task<Guid> StartBatchAsync(ApiHarness api, string folder, Guid blueprint)
    {
        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Post, $"/api/projects/{folder}/blueprints/{blueprint}/jobs", new { count = 2 });

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        return JsonNode.Parse(await response.Content.ReadAsStringAsync())!["id"]!.GetValue<Guid>();
    }

    /// <summary>Waits until the log holds an event that ends the batch.</summary>
    private static async Task<IReadOnlyList<JsonNode>> WaitForEndAsync(ApiHarness api, Guid job)
    {
        for (int attempt = 0; attempt < 500; attempt++)
        {
            IReadOnlyList<JsonNode> events = api.LogEvents();

            if (events.Any(logEvent => LogFiles.Property(logEvent, "JobId") == job.ToString()
                && logEvent["MessageTemplate"]!.GetValue<string>().StartsWith("Batch ", StringComparison.Ordinal)
                && !logEvent["MessageTemplate"]!.GetValue<string>().StartsWith("Batch started", StringComparison.Ordinal)))
            {
                return events;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException($"Job {job} never logged its end.");
    }

    // --- H.8 n° 4 : le message va au journal, le code seul à la réponse ------------------------

    [Fact]
    public async Task ACodedErrorWritesItsCodeAndMessageToTheLogAndOnlyItsCodeToTheResponse()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        var unknown = Guid.NewGuid();

        using HttpResponseMessage response = await api.SendAsync(
            HttpMethod.Delete, $"/api/projects/{folder}/blueprints/{unknown}?note=private", body: null);

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.NotFound, "BLUEPRINT_NOT_FOUND"));

        JsonNode logged = api.LogEvents().Single(logEvent => LogFiles.Property(logEvent, "Code") == "BLUEPRINT_NOT_FOUND");
        Level(logged).ShouldBe("Warning");
        LogFiles.Property(logged, "Method").ShouldBe("DELETE");
        LogFiles.Property(logged, "Status").ShouldBe("404");

        // The message names the blueprint; the response did not.
        LogFiles.Property(logged, "Reason")!.ShouldContain(unknown.ToString());

        // The path, without its query string.
        LogFiles.Property(logged, "Path").ShouldBe($"/api/projects/{folder}/blueprints/{unknown}");
        logged.ToJsonString().ShouldNotContain("private");
    }

    // --- H.8 n° 5 : une erreur sans code écrit l'exception ----------------------------------------

    [Fact]
    public async Task AnErrorWithoutACodeWritesTheWholeExceptionAsAnError()
    {
        var generator = new FakeGenerator { Failure = new InvalidOperationException("Something nobody expected") };
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());

        using HttpResponseMessage response = await api.Client.GetAsync("/api/generator");

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.InternalServerError, "INTERNAL_ERROR"));

        JsonNode logged = api.LogEvents().Single(logEvent => LogFiles.Property(logEvent, "Code") == "INTERNAL_ERROR");
        Level(logged).ShouldBe("Error");
        logged["Exception"]!.GetValue<string>().ShouldContain("Something nobody expected");
    }

    // --- H.8 n° 6 : chaque événement d'un lot porte son JobId -----------------------------------

    [Fact]
    public async Task EveryEventOfABatchCarriesItsJobIdAndNoOtherEventDoes()
    {
        var generator = new FakeGenerator();
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        Guid job = await StartBatchAsync(api, folder, blueprint);
        IReadOnlyList<JsonNode> events = await WaitForEndAsync(api, job);

        List<JsonNode> batch = [.. events.Where(logEvent => logEvent["MessageTemplate"]!.GetValue<string>().StartsWith("Batch ", StringComparison.Ordinal))];
        batch.Count.ShouldBe(2);
        batch.ShouldAllBe(logEvent => LogFiles.Property(logEvent, "JobId") == job.ToString());
        LogFiles.Property(batch[0], "BlueprintId").ShouldBe(blueprint.ToString());
        LogFiles.Property(batch[1], "State").ShouldBe("Completed");
        LogFiles.Property(batch[1], "Produced").ShouldBe("2");

        // Pushed for the batch only: the start-up line, written outside any
        // batch, does not carry it.
        events.ShouldContain(logEvent => LogFiles.Property(logEvent, "JobId") == null);
    }

    // --- H.8 n° 7 : un lot en échec écrit son code et son message --------------------------------

    [Fact]
    public async Task AFailedBatchWritesItsCodeAndMessage()
    {
        var generator = new FakeGenerator
        {
            Failure = new GeneratorException(GeneratorErrorCode.Unreachable, "Connection refused by comfy.test:8188"),
        };
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        Guid job = await StartBatchAsync(api, folder, blueprint);
        IReadOnlyList<JsonNode> events = await WaitForEndAsync(api, job);

        JsonNode failed = events.Single(logEvent => LogFiles.Property(logEvent, "JobId") == job.ToString() && Level(logEvent) == "Warning");
        LogFiles.Property(failed, "Code").ShouldBe("GENERATOR_UNREACHABLE");
        LogFiles.Property(failed, "Reason").ShouldBe("Connection refused by comfy.test:8188");
        LogFiles.Property(failed, "Produced").ShouldBe("0");
    }
}
