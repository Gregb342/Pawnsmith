using System.Text.Json;
using System.Text.RegularExpressions;

using Pawnsmith.Infrastructure.Logging;

using Serilog.Core;

namespace Pawnsmith.Infrastructure.Tests.Logging;

/// <summary>
/// The log files as the sink writes them. Covers tests 1 to 3 and 17 of H.8.
/// </summary>
/// <remarks>
/// No fake logger: the file sink is exercised as it runs, in a temporary
/// folder, and the files are read back the way an operator would.
/// </remarks>
public sealed class LogFileTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "pawnsmith-log-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private LogOptions Options(bool enabled = true, int retainedFileCount = 31, long fileSizeLimitBytes = 50L * 1024 * 1024) =>
        new(directory, enabled, retainedFileCount, fileSizeLimitBytes);

    /// <summary>Every line of every file, read while the sink may still hold them open.</summary>
    private string[] Lines() =>
    [
        .. Directory.GetFiles(directory).Order(StringComparer.Ordinal).SelectMany(ReadShared),
    ];

    private static IEnumerable<string> ReadShared(string path)
    {
        // The sink keeps the current file open for writing; a reader must let it.
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);

        return reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    // --- H.8 n° 1 : un fichier par jour, une ligne JSON par événement ---------------------

    [Fact]
    public void AFileNamedByTheDayIsWrittenWithOneJsonObjectPerLine()
    {
        using (Logger log = LogSink.Create(Options())!)
        {
            log.Information("Batch {JobId} started", Guid.NewGuid());
            log.Warning("Batch failed with {Code}", "GENERATOR_UNREACHABLE");
        }

        string name = Path.GetFileName(Directory.GetFiles(directory).Single());
        Regex.IsMatch(name, @"^pawnsmith-\d{8}\.ndjson$").ShouldBeTrue(name);

        string[] lines = Lines();
        lines.Length.ShouldBe(2);

        using var second = JsonDocument.Parse(lines[1]);
        second.RootElement.GetProperty("Level").GetString().ShouldBe("Warning");
        second.RootElement.GetProperty("Properties").GetProperty("Code").GetString().ShouldBe("GENERATOR_UNREACHABLE");

        // The message is rendered, so a reader needs no template engine.
        second.RootElement.GetProperty("RenderedMessage").GetString().ShouldBe("Batch failed with \"GENERATOR_UNREACHABLE\"");
    }

    // --- H.8 n° 2 : désactivée, rien n'est écrit ---------------------------------------------

    [Fact]
    public void DisabledLoggingWritesNoFileAndCreatesNoFolder()
    {
        LogSink.Create(Options(enabled: false)).ShouldBeNull();

        Directory.Exists(directory).ShouldBeFalse();
    }

    // --- H.8 n° 3 : la rétention borne le nombre de fichiers ----------------------------------

    [Fact]
    public void RetentionKeepsNoMoreFilesThanConfigured()
    {
        // A tiny size limit makes the sink roll on size many times within one
        // day - the same retention runs on a daily roll, which a test cannot
        // wait for.
        using (Logger log = LogSink.Create(Options(retainedFileCount: 3, fileSizeLimitBytes: 300))!)
        {
            for (int i = 0; i < 40; i++)
            {
                log.Information("Event {Index} of a long day", i);
            }
        }

        string[] files = [.. Directory.GetFiles(directory).Select(Path.GetFileName).Order(StringComparer.Ordinal)!];

        files.Length.ShouldBe(3);
        files.ShouldAllBe(file => Regex.IsMatch(file, @"^pawnsmith-\d{8}(_\d{3,})?\.ndjson$"));

        // The newest are the ones kept: the last event is still there.
        Lines().Last().ShouldContain("\"Index\":39");
    }

    [Theory]
    [InlineData(0, 1024)]
    [InlineData(1, 0)]
    public void ACountOrASizeBelowOneIsRefusedByTheNameOfItsSetting(int retainedFileCount, long fileSizeLimitBytes)
    {
        ArgumentOutOfRangeException error = Should.Throw<ArgumentOutOfRangeException>(
            () => LogSink.Create(Options(retainedFileCount: retainedFileCount, fileSizeLimitBytes: fileSizeLimitBytes)));

        error.ParamName.ShouldStartWith("Pawnsmith:Logs:");
    }

    // --- H.8 n° 17 : MEN-011, un saut de ligne ne fabrique pas d'événement ---------------------

    [Fact]
    public void ANewlineInAValueCannotForgeASecondEvent()
    {
        // A project name is free text, possibly from someone else's archive,
        // and error messages quote it.
        const string forged = "donjon\n{\"Timestamp\":\"2026-01-01T00:00:00Z\",\"Level\":\"Fatal\",\"MessageTemplate\":\"forged\"}";

        using (Logger log = LogSink.Create(Options())!)
        {
            log.Warning("Project {Folder} could not be read", forged);
        }

        string line = Lines().ShouldHaveSingleItem();

        using var parsed = JsonDocument.Parse(line);
        parsed.RootElement.GetProperty("Level").GetString().ShouldBe("Warning");
        parsed.RootElement.GetProperty("Properties").GetProperty("Folder").GetString().ShouldBe(forged);
    }
}
