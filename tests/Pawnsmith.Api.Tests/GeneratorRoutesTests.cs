using System.Net;
using System.Text.Json.Nodes;

using Pawnsmith.Api.Tests.Fixtures;

namespace Pawnsmith.Api.Tests;

/// <summary>The generator address set from the interface (§I.5, DEC-108), tests 9 to 11 of §I.12.</summary>
public class GeneratorRoutesTests
{
    // Port 9 is "discard": nothing listens there on a test machine, so the
    // generator is configured and unreachable, and the check fails fast.
    private const string Unreachable = "http://127.0.0.1:9/";

    private static string ExampleWorkflow()
    {
        string? directory = AppContext.BaseDirectory;

        while (directory is not null && !File.Exists(Path.Combine(directory, "Pawnsmith.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        return Path.Combine(directory!, "config", "workflow.comfyui.example.json");
    }

    private static string[] Settings(string user) =>
    [
        $"Pawnsmith:UserDirectory={user}",
        "Pawnsmith:Generator:Url=http://127.0.0.1:1/",
        $"Pawnsmith:Generator:WorkflowFile={ExampleWorkflow()}",
    ];

    [Fact]
    public async Task TheAddressIsSavedAndARestartReadsItBeforeTheConfiguration()
    {
        string user = Path.Combine(Path.GetTempPath(), "pawnsmith-api-tests", "user-" + Guid.NewGuid().ToString("N"));

        try
        {
            await using (ApiHarness first = await ApiHarness.StartAsync(settings: Settings(user)))
            {
                JsonNode changed = await first.SendJsonAsync(HttpMethod.Put, "/api/generator", new { address = Unreachable });

                changed["state"]!.GetValue<string>().ShouldBe("Unreachable");
                changed["address"]!.GetValue<string>().ShouldStartWith("http://127.0.0.1:9");
                File.Exists(Path.Combine(user, "generator.json")).ShouldBeTrue();
            }

            await using ApiHarness second = await ApiHarness.StartAsync(settings: Settings(user));
            (await second.GetJsonAsync("/api/generator"))["address"]!.GetValue<string>().ShouldStartWith("http://127.0.0.1:9");
        }
        finally
        {
            Directory.Delete(user, recursive: true);
        }
    }

    [Fact]
    public async Task NoAddressMeansNoGeneratorAndIsKeptAsAChoice()
    {
        // The configuration names an address; the user's "none" wins over it.
        string user = Path.Combine(Path.GetTempPath(), "pawnsmith-api-tests", "user-" + Guid.NewGuid().ToString("N"));

        try
        {
            await using ApiHarness api = await ApiHarness.StartAsync(settings: Settings(user));

            JsonNode cleared = await api.SendJsonAsync(HttpMethod.Put, "/api/generator", new { address = (string?)null });

            cleared["state"]!.GetValue<string>().ShouldBe("NotConfigured");
            (await File.ReadAllTextAsync(Path.Combine(user, "generator.json"))).ShouldContain("null");
        }
        finally
        {
            Directory.Delete(user, recursive: true);
        }
    }

    [Theory]
    [InlineData("http://user:secret@127.0.0.1:9/")]
    [InlineData("ftp://127.0.0.1/")]
    [InlineData("not an address")]
    public async Task ARefusedAddressSavesNothingAndIsNotRepeated(string address)
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        using HttpResponseMessage response = await api.SendAsync(HttpMethod.Put, "/api/generator", new { address });

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.UnprocessableEntity, "GENERATOR_ADDRESS_INVALID"));
        File.Exists(Path.Combine(api.Root, "data", "user", "generator.json")).ShouldBeFalse();
        api.LogEvents().Select(line => line.ToJsonString()).ShouldNotContain(line => line.Contains("secret", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AQueuedBatchKeepsTheGeneratorItWasAcceptedWith()
    {
        var generator = new FakeGenerator { Hold = new TaskCompletionSource() };
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        using HttpResponseMessage started = await api.SendAsync(HttpMethod.Post, $"/api/projects/{folder}/blueprints/{blueprint}/jobs", new { count = 1 });
        Guid job = JsonNode.Parse(await started.Content.ReadAsStringAsync())!["id"]!.GetValue<Guid>();

        for (int attempt = 0; generator.Started == 0 && attempt < 500; attempt++)
        {
            await Task.Delay(10);
        }

        // The address changes while the batch is running on the fake.
        await api.SendJsonAsync(HttpMethod.Put, "/api/generator", new { address = Unreachable });
        generator.Hold.SetResult();

        JsonNode finished = await api.GetJsonAsync($"/api/jobs/{job}");

        for (int attempt = 0; finished["state"]!.GetValue<string>() is "Queued" or "Running" && attempt < 500; attempt++)
        {
            await Task.Delay(10);
            finished = await api.GetJsonAsync($"/api/jobs/{job}");
        }

        finished["state"]!.GetValue<string>().ShouldBe("Completed");
        finished["produced"]!.AsArray().Count.ShouldBe(1);
    }
}
