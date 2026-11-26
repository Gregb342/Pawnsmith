using System.Text.Json.Nodes;

using Pawnsmith.Api.Hosting;
using Pawnsmith.Api.Tests.Fixtures;

namespace Pawnsmith.Api.Tests;

/// <summary>
/// What the host writes to its log as it starts. Covers tests 2, 8 to 10 and 20 of H.8.
/// </summary>
public class StartupLogTests
{
    private static string Template(JsonNode logEvent) => logEvent["MessageTemplate"]!.GetValue<string>();

    // --- H.8 n° 8 : boucle locale ou non --------------------------------------------------------

    [Theory]
    [InlineData("http://localhost:8080", false)]
    [InlineData("http://LOCALHOST:8080", false)]
    [InlineData("http://127.0.0.1:8080", false)]
    [InlineData("http://127.12.0.3:8080", false)]
    [InlineData("http://[::1]:8080", false)]
    [InlineData("https://127.0.0.1:8443", false)]
    [InlineData("http://0.0.0.0:8080", true)]
    [InlineData("http://[::]:8080", true)]
    [InlineData("http://+:8080", true)]
    [InlineData("http://*:8080", true)]
    [InlineData("http://192.168.1.20:8080", true)]
    [InlineData("http://pawnsmith.lan:8080", true)]
    [InlineData("http://localhost.evil.example:8080", true)]
    public void AnAddressIsLoopbackOrItIsNot(string address, bool nonLocal)
    {
        ListeningAddresses.NonLocal([address]).Count.ShouldBe(nonLocal ? 1 : 0);
    }

    // --- H.8 n° 9 : l'alerte de MEN-004 ------------------------------------------------------------

    [Fact]
    public async Task ListeningOnLoopbackRaisesNoWarning()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        api.LogEvents().ShouldNotContain(logEvent => Template(logEvent).Contains("MEN-004", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ListeningOnEveryInterfaceRaisesAWarningThatNamesTheAddress()
    {
        await using ApiHarness api = await ApiHarness.StartAsync(generator: null, "urls=http://0.0.0.0:0");

        JsonNode warning = api.LogEvents().Single(logEvent => Template(logEvent).Contains("MEN-004", StringComparison.Ordinal));
        warning["Level"]!.GetValue<string>().ShouldBe("Warning");
        LogFiles.Property(warning, "Address")!.ShouldStartWith("http://0.0.0.0:");
    }

    // --- H.8 n° 10 : ce que le démarrage écrit ---------------------------------------------------

    [Fact]
    public async Task TheStartWritesTheVersionTheFoldersAndTheGeneratorState()
    {
        var generator = new FakeGenerator();
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());

        JsonNode started = api.LogEvents().Single(logEvent => Template(logEvent).StartsWith("Pawnsmith {Version} started", StringComparison.Ordinal));
        LogFiles.Property(started, "Version").ShouldBe("0.10.0");
        LogFiles.Property(started, "ProjectsRoot").ShouldBe(api.ProjectsRoot);
        LogFiles.Property(started, "LogsDirectory").ShouldBe(api.LogsDirectory);
        LogFiles.Property(started, "GeneratorState").ShouldBe("Configured");
        LogFiles.Property(started, "GeneratorAddress").ShouldBe("http://comfy.test:8188/");
    }

    [Fact]
    public async Task AMisconfiguredGeneratorWritesTheMessageOfItsRefusal()
    {
        await using ApiHarness api = await ApiHarness.StartAsync(
            generator: null,
            "Pawnsmith:Generator:Url=http://127.0.0.1:8188",
            "Pawnsmith:Generator:WorkflowFile=/nowhere/workflow.json");

        JsonNode refused = api.LogEvents().Single(logEvent => Template(logEvent).StartsWith("The generator is misconfigured", StringComparison.Ordinal));
        refused["Level"]!.GetValue<string>().ShouldBe("Warning");
        LogFiles.Property(refused, "Code").ShouldBe("WORKFLOW_INVALID");
        LogFiles.Property(refused, "Reason")!.ShouldContain("/nowhere/workflow.json");
    }

    [Fact]
    public async Task AnAddressWithCredentialsIsNeitherShownNorLoggedWhenTheWorkflowIsRefused()
    {
        // The address is never checked when the workflow fails first; it must
        // not travel on that path either (DEC-081).
        await using ApiHarness api = await ApiHarness.StartAsync(
            generator: null,
            "Pawnsmith:Generator:Url=http://user:secret@127.0.0.1:8188",
            "Pawnsmith:Generator:WorkflowFile=/nowhere/workflow.json");

        JsonNode generator = await api.GetJsonAsync("/api/generator");
        generator["address"].ShouldBeNull();

        api.LogEvents().ShouldNotContain(logEvent => logEvent.ToJsonString().Contains("secret", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheHostWritesItsEventsToTheConfiguredFolder()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        api.LogEvents().ShouldNotBeEmpty();
        Directory.GetFiles(api.LogsDirectory).ShouldAllBe(file => file.EndsWith(".ndjson", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheLevelIsAspNetsOwnSettingForTheFilesToo()
    {
        // Information by default, Microsoft.AspNetCore at Warning: neither the
        // framework's routing chatter nor its debug events reach the files.
        await using ApiHarness api = await ApiHarness.StartAsync(
            generator: null,
            "Logging:LogLevel:Default=Information",
            "Logging:LogLevel:Microsoft.AspNetCore=Warning");

        using HttpResponseMessage response = await api.Client.GetAsync("/api/configuration");
        response.EnsureSuccessStatusCode();

        IReadOnlyList<JsonNode> events = api.LogEvents();
        events.ShouldNotBeEmpty();
        events.ShouldAllBe(logEvent => logEvent["Level"]!.GetValue<string>() != "Debug" && logEvent["Level"]!.GetValue<string>() != "Verbose");
        events.ShouldNotContain(logEvent => logEvent["MessageTemplate"]!.GetValue<string>().StartsWith("Executing endpoint", StringComparison.Ordinal));
    }

    // --- H.8 n° 2 : désactivée, aucun dossier ------------------------------------------------

    [Fact]
    public async Task DisabledLoggingLeavesNoFolderBehind()
    {
        await using ApiHarness api = await ApiHarness.StartAsync(generator: null, "Pawnsmith:Logs:Enabled=false");

        using HttpResponseMessage response = await api.Client.GetAsync("/api/nothing-here");

        Directory.Exists(api.LogsDirectory).ShouldBeFalse();
    }

    // --- H.8 n° 20 : un démarrage impossible écrit une ligne Fatal -----------------------------

    [Fact]
    public async Task AStartThatFailsWritesAFatalLineBeforeTheProcessEnds()
    {
        string root = Path.Combine(Path.GetTempPath(), "pawnsmith-api-tests", Guid.NewGuid().ToString("N"));
        string logs = Path.Combine(root, "logs");

        // An empty configuration folder: no calibration to read.
        Directory.CreateDirectory(Path.Combine(root, "config"));

        try
        {
            string[] args =
            [
                "--urls=http://127.0.0.1:0",
                $"--contentRoot={root}",
                $"--Pawnsmith:ConfigDirectory={Path.Combine(root, "config")}",
                $"--Pawnsmith:Logs:Directory={logs}",
            ];

            await Should.ThrowAsync<Exception>(() => ApiHost.BuildAsync(args));

            JsonNode fatal = LogFiles.Events(logs).Single(logEvent => logEvent["Level"]!.GetValue<string>() == "Fatal");
            fatal["RenderedMessage"]!.GetValue<string>().ShouldContain("calibration.json");
            fatal["Exception"].ShouldNotBeNull();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
