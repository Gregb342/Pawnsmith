using System.Net;
using System.Text;
using System.Text.Json.Nodes;

using Pawnsmith.Api.Tests.Fixtures;

namespace Pawnsmith.Api.Tests;

/// <summary>
/// The log viewer, server side. Covers tests 11 to 16 of H.8 (MEN-002).
/// </summary>
/// <remarks>
/// The files are planted in the host's own log folder, beside the one the host
/// is writing, under dates far in the past so they never collide with it.
/// </remarks>
public class LogRoutesTests
{
    private static void Plant(ApiHarness api, string name, string content)
    {
        Directory.CreateDirectory(api.LogsDirectory);
        File.WriteAllText(Path.Combine(api.LogsDirectory, name), content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static string[] Names(JsonNode list) =>
        [.. list["files"]!.AsArray().Select(file => file!["name"]!.GetValue<string>())];

    private static string[] Lines(JsonNode tail) =>
        [.. tail["lines"]!.AsArray().Select(line => line!.GetValue<string>())];

    // --- H.8 n° 11 : seuls les noms du motif, du plus récent au plus ancien ------------------------

    [Fact]
    public async Task TheListHoldsOnlyTheNamesOfThePatternNewestFirst()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        Plant(api, "pawnsmith-20200101.ndjson", "{}\n");
        Plant(api, "pawnsmith-20200102.ndjson", "{}\n");
        Plant(api, "pawnsmith-20200102_001.ndjson", "{}\n{}\n");

        // Outside the pattern, each for a reason.
        Plant(api, "notes.txt", "{}\n");
        Plant(api, "pawnsmith-2020.ndjson", "{}\n");
        Plant(api, "pawnsmith-20200101.ndjson.bak", "{}\n");
        Plant(api, "PAWNSMITH-20200101.NDJSON", "{}\n");
        Plant(api, "pawnsmith-٢٠٢٠٠١٠١.ndjson", "{}\n");
        Plant(api, "pawnsmith-20200101.json", "{}\n");

        JsonNode list = await api.GetJsonAsync("/api/logs");

        list["enabled"]!.GetValue<bool>().ShouldBeTrue();
        string[] names = Names(list);

        // The host's own file first - today is after 2020 - then the planted ones.
        names.Length.ShouldBe(4);
        names[0].ShouldStartWith("pawnsmith-2");
        names[1..].ShouldBe(["pawnsmith-20200102_001.ndjson", "pawnsmith-20200102.ndjson", "pawnsmith-20200101.ndjson"]);

        JsonNode rolled = list["files"]!.AsArray().Single(file => file!["name"]!.GetValue<string>() == "pawnsmith-20200102_001.ndjson")!;
        rolled["sizeBytes"]!.GetValue<long>().ShouldBe(6);
    }

    [Fact]
    public async Task WithLoggingDisabledTheListSaysSoAndStillShowsWhatIsThere()
    {
        await using ApiHarness api = await ApiHarness.StartAsync(generator: null, "Pawnsmith:Logs:Enabled=false");
        Plant(api, "pawnsmith-20200101.ndjson", "{}\n");

        JsonNode list = await api.GetJsonAsync("/api/logs");

        list["enabled"]!.GetValue<bool>().ShouldBeFalse();
        Names(list).ShouldBe(["pawnsmith-20200101.ndjson"]);
    }

    // --- H.8 n° 12 : hors motif, ni listé ni lisible ----------------------------------------------

    [Theory]
    [InlineData("notes.txt")]
    [InlineData("pawnsmith-20200101.ndjson.bak")]
    [InlineData("PAWNSMITH-20200101.NDJSON")]
    [InlineData("pawnsmith-20200101.json")]
    [InlineData("pawnsmith-20991231.ndjson")]
    public async Task ANameOutsideTheWhitelistIsNotFoundEvenWhenTheFileExists(string name)
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        Plant(api, name, "{}\n");

        // Matching the pattern is not enough: the last one is not on the disk.
        File.Delete(Path.Combine(api.LogsDirectory, "pawnsmith-20991231.ndjson"));

        using HttpResponseMessage response = await api.Client.GetAsync($"/api/logs/{Uri.EscapeDataString(name)}");

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.NotFound, "LOG_NOT_FOUND"));
    }

    // --- H.8 n° 13 : un nom de traversée ------------------------------------------------------------

    [Theory]
    [InlineData("..%2Fprojects%2Fdonjon%2Fproject.json")]
    [InlineData("..%2F..%2Fetc%2Fpasswd")]
    [InlineData("%2Fetc%2Fpasswd")]
    [InlineData("..%5Cpawnsmith-20200101.ndjson")]
    [InlineData("pawnsmith-20200101.ndjson%0A")]
    [InlineData("%2E%2E")]
    public async Task ATraversalNameIsNotFound(string encoded)
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        Plant(api, "pawnsmith-20200101.ndjson", "{}\n");

        using HttpResponseMessage response = await api.Client.GetAsync($"/api/logs/{encoded}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ApiHarness.ErrorOf(response)).Code.ShouldBeOneOf("LOG_NOT_FOUND", "ROUTE_NOT_FOUND");
    }

    [Fact]
    public async Task ANulByteInTheNameIsRefusedByTheServerBeforeAnyRoute()
    {
        // Kestrel refuses a NUL in a path itself, with a bodiless 400, as it
        // does for a malformed request (§G.3.1): nothing of ours runs.
        await using ApiHarness api = await ApiHarness.StartAsync();
        Plant(api, "pawnsmith-20200101.ndjson", "{}\n");

        using HttpResponseMessage response = await api.Client.GetAsync("/api/logs/pawnsmith-20200101.ndjson%00");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // --- H.8 n° 14 : un lien symbolique au nom valide ------------------------------------------------

    [Fact]
    public async Task ASymbolicLinkUnderAValidNameIsLeftOut()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string secret = Path.Combine(api.Root, "secret.txt");
        File.WriteAllText(secret, "{\"secret\":true}\n");
        Directory.CreateDirectory(api.LogsDirectory);
        string link = Path.Combine(api.LogsDirectory, "pawnsmith-20200101.ndjson");

        try
        {
            File.CreateSymbolicLink(link, secret);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            // Windows without the symbolic link privilege: nothing to plant,
            // so nothing to test here. The Ubuntu CI certifies it (MEN-008).
            return;
        }

        Names(await api.GetJsonAsync("/api/logs")).ShouldNotContain("pawnsmith-20200101.ndjson");

        using HttpResponseMessage response = await api.Client.GetAsync("/api/logs/pawnsmith-20200101.ndjson");
        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.NotFound, "LOG_NOT_FOUND"));
    }

    // --- H.8 n° 15 : les N dernières lignes, et les bornes --------------------------------------------

    [Fact]
    public async Task TheReadGivesTheLastLinesInFileOrderAndSaysWhenThereIsMore()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        Plant(api, "pawnsmith-20200101.ndjson", string.Concat(Enumerable.Range(1, 10).Select(n => $"{{\"n\":{n}}}\n")));

        JsonNode three = await api.GetJsonAsync("/api/logs/pawnsmith-20200101.ndjson?lines=3");
        three["name"]!.GetValue<string>().ShouldBe("pawnsmith-20200101.ndjson");
        Lines(three).ShouldBe(["{\"n\":8}", "{\"n\":9}", "{\"n\":10}"]);
        three["truncated"]!.GetValue<bool>().ShouldBeTrue();

        JsonNode all = await api.GetJsonAsync("/api/logs/pawnsmith-20200101.ndjson?lines=10");
        Lines(all).Length.ShouldBe(10);
        all["truncated"]!.GetValue<bool>().ShouldBeFalse();
    }

    [Theory]
    [InlineData("?lines=0")]
    [InlineData("?lines=-1")]
    [InlineData("?lines=5001")]
    [InlineData("?lines=many")]
    public async Task ALineCountOutsideItsBoundsIsRequestInvalid(string query)
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        Plant(api, "pawnsmith-20200101.ndjson", "{}\n");

        using HttpResponseMessage response = await api.Client.GetAsync($"/api/logs/pawnsmith-20200101.ndjson{query}");

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.BadRequest, "REQUEST_INVALID"));
    }

    [Fact]
    public async Task AFileLargerThanTheReadWindowGivesOnlyCompleteLines()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();

        // About six mebibytes of lines, past the four-mebibyte window, with a
        // multi-byte character in each so that a cut inside one would show.
        StringBuilder content = new();
        for (int n = 0; n < 120_000; n++)
        {
            content.Append($"{{\"n\":{n},\"name\":\"Dédale-{n:D30}\"}}\n");
        }

        Plant(api, "pawnsmith-20200101.ndjson", content.ToString());

        JsonNode tail = await api.GetJsonAsync($"/api/logs/pawnsmith-20200101.ndjson?lines=5000");

        string[] lines = Lines(tail);
        lines.Length.ShouldBe(5000);
        lines.ShouldAllBe(line => JsonNode.Parse(line, null, default) != null);
        lines[^1].ShouldStartWith("{\"n\":119999,");
        tail["truncated"]!.GetValue<bool>().ShouldBeTrue();
    }

    // --- H.8 n° 16 : la dernière ligne inachevée, et le fichier en cours ------------------------------

    [Fact]
    public async Task ALastLineStillBeingWrittenIsNotReturned()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        Plant(api, "pawnsmith-20200101.ndjson", "{\"n\":1}\r\n{\"n\":2}\r\n{\"n\":3,\"half");

        JsonNode tail = await api.GetJsonAsync("/api/logs/pawnsmith-20200101.ndjson");

        Lines(tail).ShouldBe(["{\"n\":1}", "{\"n\":2}"]);
    }

    [Fact]
    public async Task TheFileTheHostIsWritingCanBeRead()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string current = Names(await api.GetJsonAsync("/api/logs"))[0];

        JsonNode tail = await api.GetJsonAsync($"/api/logs/{current}");

        Lines(tail).ShouldNotBeEmpty();
        Lines(tail).ShouldContain(line => line.Contains("Pawnsmith {Version} started", StringComparison.Ordinal));
    }
}
