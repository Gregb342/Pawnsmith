using System.Text.Json.Nodes;

using Pawnsmith.Api.Hosting;
using Pawnsmith.Api.Tests.Fixtures;

namespace Pawnsmith.Api.Tests;

/// <summary>
/// What the host writes to its log as it starts. Covers tests 2 and 20 of H.8.
/// </summary>
public class StartupLogTests
{
    [Fact]
    public async Task TheHostWritesItsEventsToTheConfiguredFolder()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        api.LogEvents().ShouldNotBeEmpty();
        Directory.GetFiles(api.LogsDirectory).ShouldAllBe(file => file.EndsWith(".ndjson", StringComparison.Ordinal));
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
