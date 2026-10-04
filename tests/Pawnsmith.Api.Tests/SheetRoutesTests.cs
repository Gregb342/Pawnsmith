using System.Net;
using System.Text.Json.Nodes;

using Microsoft.Extensions.DependencyInjection;

using Pawnsmith.Api.Tests.Fixtures;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Sheets;
using Pawnsmith.Domain.Units;

namespace Pawnsmith.Api.Tests;

/// <summary>
/// The sheet routes. Covers tests 23 to 25 of G.13, and question C (DEC-082).
/// </summary>
public class SheetRoutesTests
{
    private static object Settings(string styleClause, double? tabHeightMm = null) => new
    {
        name = "Donjon",
        universe = "Fantasy",
        geometry = "TabAndSocket",
        paperFormat = "A4",
        style = new { name = "", styleClause, negativeClause = "", palette = "" },
        calibrationOverrides = new { tabWidthMm = (double?)null, tabHeightMm },
    };

    /// <summary>A project with one blueprint of six Medium goblins, elected and cut out.</summary>
    private static async Task<(string Folder, Guid Blueprint, Guid Candidate)> ReadyToPrintAsync(ApiHarness api)
    {
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder, quantity: 6);
        Guid candidate = await ProjectSeed.AddCandidateAsync(api, folder, blueprint, cutOut: true, FakeGenerator.Framing, elect: true);

        return (folder, blueprint, candidate);
    }

    /// <summary>The capacity of a Medium page, through the T1 engine, on the given calibration.</summary>
    private static int MediumCapacity(Calibration calibration)
    {
        var unit = UnfoldedUnit.Create(Size.Medium, calibration.Sizes[Size.Medium], Geometry.TabAndSocket, calibration.Geometry);
        return PageGrid.Create(calibration.PaperFormats["A4"], unit, calibration.Layout).Capacity;
    }

    // --- G.13 n° 23 : capacité et occupation, sur la calibration effective ----------------

    [Fact]
    public async Task TheReportGivesCapacityAndOccupationOnTheEffectiveCalibration()
    {
        var generator = new FakeGenerator();
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        (string folder, _, _) = await ReadyToPrintAsync(api);
        Calibration calibration = api.Services.GetRequiredService<Calibration>();

        JsonNode page = (await api.GetJsonAsync($"/api/projects/{folder}/sheet/report"))["pages"]!.AsArray().Single()!;
        page["size"]!.GetValue<string>().ShouldBe("Medium");
        page["used"]!.GetValue<int>().ShouldBe(6);
        page["capacity"]!.GetValue<int>().ShouldBe(MediumCapacity(calibration));

        // A taller tab, overridden on this project only, changes the capacity
        // of its pages (DEC-040, DEC-053).
        await api.SendJsonAsync(HttpMethod.Put, $"/api/projects/{folder}/settings", Settings(styleClause: "", tabHeightMm: 40.0));
        int overridden = (await api.GetJsonAsync($"/api/projects/{folder}/sheet/report"))["pages"]![0]!["capacity"]!.GetValue<int>();

        overridden.ShouldBeLessThan(MediumCapacity(calibration));
    }

    [Fact]
    public async Task ABlueprintWithoutElectionIsReportedAsSkippedByItsReason()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        Guid blueprint = await ProjectSeed.AddBlueprintAsync(api, folder);

        JsonNode report = await api.GetJsonAsync($"/api/projects/{folder}/sheet/report");

        report["pages"]!.AsArray().ShouldBeEmpty();
        JsonNode skipped = report["skipped"]!.AsArray().Single()!;
        skipped["blueprintId"]!.GetValue<Guid>().ShouldBe(blueprint);
        skipped["reason"]!.GetValue<string>().ShouldBe("NoElectedCandidate");
    }

    // --- G.13 n° 24 : l'élu désaligné est exporté et listé (DEC-082) ----------------------

    [Fact]
    public async Task AMisalignedElectedCandidateIsListedWithItsClausesAndStillPrinted()
    {
        var generator = new FakeGenerator();
        await using ApiHarness api = await ApiHarness.StartAsync(generator.Setup());
        (string folder, Guid blueprint, Guid candidate) = await ReadyToPrintAsync(api);

        // The subject moves; the style no longer can once a proposal exists (DEC-112).
        await api.SendJsonAsync(HttpMethod.Put, $"/api/projects/{folder}/blueprints/{blueprint}/subject-clause", new { clause = "a scarred goblin" });

        JsonNode report = await api.GetJsonAsync($"/api/projects/{folder}/sheet/report");
        report["misalignmentKnown"]!.GetValue<bool>().ShouldBeTrue();
        JsonNode misaligned = report["misalignedElections"]!.AsArray().Single()!;
        misaligned["blueprintId"]!.GetValue<Guid>().ShouldBe(blueprint);
        misaligned["candidateId"]!.GetValue<Guid>().ShouldBe(candidate);
        misaligned["clauses"]!.AsArray().Select(clause => clause!.GetValue<string>()).ShouldBe(["Subject"]);

        using HttpResponseMessage pdf = await api.Client.GetAsync($"/api/projects/{folder}/sheet.pdf?culture=en");
        pdf.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WithoutAWorkflowTheReportSaysTheMisalignmentIsUnknown()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        (string folder, _, _) = await ReadyToPrintAsync(api);

        JsonNode report = await api.GetJsonAsync($"/api/projects/{folder}/sheet/report");

        report["misalignmentKnown"]!.GetValue<bool>().ShouldBeFalse();
        report["misalignedElections"]!.AsArray().ShouldBeEmpty();
    }

    // --- G.13 n° 25 : le PDF, dans la culture demandée ----------------------------------------

    [Theory]
    [InlineData("fr")]
    [InlineData("en")]
    public async Task ThePdfComesOutInTheRequestedCulture(string culture)
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        (string folder, _, _) = await ReadyToPrintAsync(api);

        using HttpResponseMessage response = await api.Client.GetAsync($"/api/projects/{folder}/sheet.pdf?culture={culture}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/pdf");
        byte[] pdf = await response.Content.ReadAsByteArrayAsync();
        System.Text.Encoding.ASCII.GetString(pdf, 0, 5).ShouldBe("%PDF-");
    }

    [Theory]
    [InlineData("?culture=de")]
    [InlineData("?culture=FR")]
    [InlineData("")]
    public async Task AnUnknownOrMissingCultureIsRequestInvalid(string query)
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        (string folder, _, _) = await ReadyToPrintAsync(api);

        using HttpResponseMessage response = await api.Client.GetAsync($"/api/projects/{folder}/sheet.pdf{query}");

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.BadRequest, "REQUEST_INVALID"));
    }

    [Fact]
    public async Task NothingToPrintIsRefusedRatherThanAnEmptyPage()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        string folder = await ProjectSeed.CreateAsync(api);
        await ProjectSeed.AddBlueprintAsync(api, folder);

        using HttpResponseMessage response = await api.Client.GetAsync($"/api/projects/{folder}/sheet.pdf?culture=en");

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.UnprocessableEntity, "SHEET_EMPTY"));
    }

    [Fact]
    public async Task AnElectedImageMissingFromTheDiskIsSheetInputInvalid()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        (string folder, _, Guid candidate) = await ReadyToPrintAsync(api);
        File.Delete(Path.Combine(api.ProjectsRoot, folder, "images", $"{candidate}-front.png"));

        using HttpResponseMessage response = await api.Client.GetAsync($"/api/projects/{folder}/sheet/report");

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.UnprocessableEntity, "SHEET_INPUT_INVALID"));
    }

    // --- H.8 n° 19 (T7) : MEN-005, une image trop grande n'est jamais décodée ----------------------

    [Fact]
    public async Task AnElectedImageTooLargeToDecodeIsRefusedBeforeTheSheetIsRendered()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        (string folder, _, Guid candidate) = await ReadyToPrintAsync(api);

        // What an imported archive can carry: a few bytes announcing 60 000 ×
        // 60 000 pixels, fourteen gigabytes once decoded.
        await File.WriteAllBytesAsync(
            Path.Combine(api.ProjectsRoot, folder, "images", $"{candidate}-front.png"),
            TestPng.HeaderOnly(60000, 60000));

        using HttpResponseMessage pdf = await api.Client.GetAsync($"/api/projects/{folder}/sheet.pdf?culture=en");
        (await ApiHarness.ErrorOf(pdf)).ShouldBe((HttpStatusCode.UnprocessableEntity, "SHEET_INPUT_INVALID"));

        using HttpResponseMessage report = await api.Client.GetAsync($"/api/projects/{folder}/sheet/report");
        (await ApiHarness.ErrorOf(report)).ShouldBe((HttpStatusCode.UnprocessableEntity, "SHEET_INPUT_INVALID"));
    }

    [Fact]
    public async Task AnUnknownPaperFormatIsRefusedWhenASheetIsAskedFor()
    {
        await using ApiHarness api = await ApiHarness.StartAsync();
        (string folder, _, _) = await ReadyToPrintAsync(api);
        await api.SendJsonAsync(HttpMethod.Put, $"/api/projects/{folder}/settings", new
        {
            name = "Donjon",
            universe = "Fantasy",
            geometry = "TabAndSocket",
            paperFormat = "Tabloid",
            style = new { name = "", styleClause = "", negativeClause = "", palette = "" },
            calibrationOverrides = new { tabWidthMm = (double?)null, tabHeightMm = (double?)null },
        });

        using HttpResponseMessage response = await api.Client.GetAsync($"/api/projects/{folder}/sheet/report");

        (await ApiHarness.ErrorOf(response)).ShouldBe((HttpStatusCode.UnprocessableEntity, "PAPER_FORMAT_UNKNOWN"));
    }
}
