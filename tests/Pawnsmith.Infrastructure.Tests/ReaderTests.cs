using Pawnsmith.Domain;

namespace Pawnsmith.Infrastructure.Tests;

/// <summary>
/// Covers tests 17, 18 and 19 of B.8: an invalid manifest is reported
/// explicitly, a missing image names the file, and the documentary
/// <c>paper</c> block is accepted and ignored.
/// </summary>
public class ReaderTests
{
    private const string ValidCalibration = """
        {
          "versionSchema": 1,
          "paper": { "grammageGsm": 250, "note": "Reference stock." },
          "sizes": {
            "Small":      { "gridFootprintMm": 25.4,  "pawnWidthMm": 25.4,  "pawnHeightMm": 40.0 },
            "Medium":     { "gridFootprintMm": 25.4,  "pawnWidthMm": 25.4,  "pawnHeightMm": 50.0 },
            "Large":      { "gridFootprintMm": 50.8,  "pawnWidthMm": 50.8,  "pawnHeightMm": 75.0 },
            "Huge":       { "gridFootprintMm": 76.2,  "pawnWidthMm": 76.2,  "pawnHeightMm": 100.0 },
            "Gargantuan": { "gridFootprintMm": 101.6, "pawnWidthMm": 101.6, "pawnHeightMm": 110.0 }
          },
          "geometry": {
            "foldedTent":   { "flapHeightMm": 8.0 },
            "tabAndSocket": { "tabWidthMm": 12.0, "tabHeightMm": 10.0 }
          },
          "layout": {
            "pageMarginMm": 10.0,
            "gutterMm": 3.0,
            "silhouetteMarginMm": 1.5,
            "calibrationZoneHeightMm": 14.0
          },
          "print": { "scaleCorrectionFactor": 1.0 },
          "strokes": {
            "cutWidthMm": 0.25,
            "foldWidthMm": 0.25,
            "colorHex": "#B0B0B0",
            "foldDashPatternMm": [2.0, 2.0]
          },
          "paperFormats": {
            "A4":     { "widthMm": 210.0, "heightMm": 297.0 },
            "Letter": { "widthMm": 216.0, "heightMm": 279.0 }
          }
        }
        """;

    private static string Manifest(string items, string geometry = "TabAndSocket", int version = 1)
    {
        return $$"""
            {
              "versionSchema": {{version}},
              "geometry": "{{geometry}}",
              "paperFormat": "A4",
              "culture": "fr-FR",
              "imagesDirectory": "./images",
              "items": [{{items}}]
            }
            """;
    }

    private const string GoblinItem = """
        {
          "name": "goblin",
          "size": "Medium",
          "quantity": 6,
          "rectoFile": "goblin-front.png",
          "versoFile": "goblin-back.png"
        }
        """;

    // --- B.8 n° 19 : le bloc paper est accepté et ignoré ------------------

    [Fact]
    public async Task ACalibrationCarryingThePaperBlockIsReadWithoutError()
    {
        using TempWorkspace workspace = new();
        string path = workspace.WriteFile("calibration.json", ValidCalibration);

        Calibration calibration = await CalibrationReader.ReadAsync(path, CancellationToken.None);

        calibration.Paper.GrammageGsm.ShouldBe(250);
        calibration.Sizes.Count.ShouldBe(5);
        calibration.PaperFormats["A4"].HeightMm.ShouldBe(297.0);
    }

    [Fact]
    public async Task ACalibrationWithoutThePaperBlockIsAlsoRead()
    {
        // The block is documentary: absent is as acceptable as present, since
        // no calculation reads it.
        using TempWorkspace workspace = new();
        string withoutPaper = ValidCalibration.Replace(
            "\"paper\": { \"grammageGsm\": 250, \"note\": \"Reference stock.\" },",
            string.Empty,
            StringComparison.Ordinal);
        string path = workspace.WriteFile("calibration.json", withoutPaper);

        Calibration calibration = await CalibrationReader.ReadAsync(path, CancellationToken.None);

        calibration.Sizes.Count.ShouldBe(5);
    }

    [Fact]
    public async Task AnUnknownCalibrationSchemaVersionIsRejected()
    {
        using TempWorkspace workspace = new();
        string path = workspace.WriteFile(
            "calibration.json",
            ValidCalibration.Replace("\"versionSchema\": 1", "\"versionSchema\": 99", StringComparison.Ordinal));

        ManifestException error = await Should.ThrowAsync<ManifestException>(
            () => CalibrationReader.ReadAsync(path, CancellationToken.None));

        error.Message.ShouldContain("99");
    }

    // --- B.8 n° 17 : manifeste invalide, erreur explicite ----------------

    [Fact]
    public async Task AMalformedManifestIsReportedWithoutAnUnhandledException()
    {
        using TempWorkspace workspace = new();
        Calibration calibration = await ReadCalibrationAsync(workspace);
        string path = workspace.WriteFile("manifest.json", "{ this is not json");

        ManifestException error = await Should.ThrowAsync<ManifestException>(
            () => ManifestReader.ReadAsync(path, calibration, CancellationToken.None));

        error.Message.ShouldContain("manifest");
    }

    [Fact]
    public async Task AMissingManifestIsReportedByPath()
    {
        using TempWorkspace workspace = new();
        Calibration calibration = await ReadCalibrationAsync(workspace);
        string path = Path.Combine(workspace.Root, "absent.json");

        ManifestException error = await Should.ThrowAsync<ManifestException>(
            () => ManifestReader.ReadAsync(path, calibration, CancellationToken.None));

        error.Message.ShouldContain("absent.json");
    }

    [Fact]
    public async Task AnUnknownGeometryIsReportedWithTheKnownOnes()
    {
        using TempWorkspace workspace = new();
        Calibration calibration = await ReadCalibrationAsync(workspace);
        workspace.WritePng("goblin-front.png", 600, 1000);
        workspace.WritePng("goblin-back.png", 600, 1000);
        string path = workspace.WriteFile("manifest.json", Manifest(GoblinItem, geometry: "Origami"));

        ManifestException error = await Should.ThrowAsync<ManifestException>(
            () => ManifestReader.ReadAsync(path, calibration, CancellationToken.None));

        error.Message.ShouldContain("Origami");
        error.Message.ShouldContain("FoldedTent");
    }

    [Fact]
    public async Task AnUnknownPaperFormatIsReportedWithTheKnownOnes()
    {
        using TempWorkspace workspace = new();
        Calibration calibration = await ReadCalibrationAsync(workspace);
        workspace.WritePng("goblin-front.png", 600, 1000);
        workspace.WritePng("goblin-back.png", 600, 1000);
        string path = workspace.WriteFile(
            "manifest.json",
            Manifest(GoblinItem).Replace("\"A4\"", "\"A3\"", StringComparison.Ordinal));

        ManifestException error = await Should.ThrowAsync<ManifestException>(
            () => ManifestReader.ReadAsync(path, calibration, CancellationToken.None));

        error.Message.ShouldContain("A3");
        error.Message.ShouldContain("Letter");
    }

    [Fact]
    public async Task AQuantityBelowOneIsReportedWithTheItemName()
    {
        using TempWorkspace workspace = new();
        Calibration calibration = await ReadCalibrationAsync(workspace);
        workspace.WritePng("goblin-front.png", 600, 1000);
        workspace.WritePng("goblin-back.png", 600, 1000);
        string path = workspace.WriteFile(
            "manifest.json",
            Manifest(GoblinItem.Replace("\"quantity\": 6", "\"quantity\": 0", StringComparison.Ordinal)));

        ManifestException error = await Should.ThrowAsync<ManifestException>(
            () => ManifestReader.ReadAsync(path, calibration, CancellationToken.None));

        error.Message.ShouldContain("goblin");
        error.Message.ShouldContain("at least 1");
    }

    [Fact]
    public async Task AnUnknownSchemaVersionIsRejected()
    {
        using TempWorkspace workspace = new();
        Calibration calibration = await ReadCalibrationAsync(workspace);
        string path = workspace.WriteFile("manifest.json", Manifest(GoblinItem, version: 2));

        await Should.ThrowAsync<ManifestException>(
            () => ManifestReader.ReadAsync(path, calibration, CancellationToken.None));
    }

    // --- B.8 n° 18 : image manquante, erreur nommant le fichier ----------

    [Fact]
    public async Task AMissingImageIsReportedByFileName()
    {
        using TempWorkspace workspace = new();
        Calibration calibration = await ReadCalibrationAsync(workspace);
        workspace.WritePng("goblin-front.png", 600, 1000);
        // goblin-back.png is deliberately absent.
        string path = workspace.WriteFile("manifest.json", Manifest(GoblinItem));

        ManifestException error = await Should.ThrowAsync<ManifestException>(
            () => ManifestReader.ReadAsync(path, calibration, CancellationToken.None));

        error.Message.ShouldContain("goblin-back.png");
        error.Message.ShouldContain("goblin");
    }

    [Fact]
    public async Task AValidManifestIsRead()
    {
        using TempWorkspace workspace = new();
        Calibration calibration = await ReadCalibrationAsync(workspace);
        workspace.WritePng("goblin-front.png", 600, 1000);
        workspace.WritePng("goblin-back.png", 600, 1000);
        string path = workspace.WriteFile("manifest.json", Manifest(GoblinItem));

        Manifest manifest = await ManifestReader.ReadAsync(path, calibration, CancellationToken.None);

        manifest.Request.Geometry.ShouldBe(Geometry.TabAndSocket);
        manifest.Request.PaperFormat.Name.ShouldBe("A4");
        manifest.Request.Items.Count.ShouldBe(1);
        manifest.Request.Items[0].Quantity.ShouldBe(6);
        manifest.Request.Items[0].Size.ShouldBe(Size.Medium);
        manifest.Culture.ShouldBe("fr-FR");
    }

    // --- Mesure des images -----------------------------------------------

    [Fact]
    public async Task ImageDimensionsAreReadFromThePngHeader()
    {
        using TempWorkspace workspace = new();
        workspace.WritePng("goblin-front.png", 600, 1000);
        workspace.WritePng("goblin-back.png", 640, 480);
        SheetItem item = new("goblin", Size.Medium, 1, "goblin-front.png", "goblin-back.png");

        FileImageSizeReader reader = new();
        IReadOnlyDictionary<string, SourceImageSize> sizes = await reader.MeasureAsync(
            workspace.ImagesDirectory,
            [item],
            CancellationToken.None);

        sizes["goblin-front.png"].WidthPx.ShouldBe(600);
        sizes["goblin-front.png"].HeightPx.ShouldBe(1000);
        sizes["goblin-back.png"].WidthPx.ShouldBe(640);
        sizes["goblin-back.png"].HeightPx.ShouldBe(480);
    }

    [Fact]
    public async Task AFileThatIsNotAPngIsRejected()
    {
        using TempWorkspace workspace = new();
        File.WriteAllText(Path.Combine(workspace.ImagesDirectory, "goblin-front.png"), "not a png at all");
        SheetItem item = new("goblin", Size.Medium, 1, "goblin-front.png", "goblin-front.png");

        FileImageSizeReader reader = new();

        ManifestException error = await Should.ThrowAsync<ManifestException>(
            () => reader.MeasureAsync(workspace.ImagesDirectory, [item], CancellationToken.None));

        error.Message.ShouldContain("goblin-front.png");
    }

    private static async Task<Calibration> ReadCalibrationAsync(TempWorkspace workspace)
    {
        string path = workspace.WriteFile("calibration.json", ValidCalibration);

        return await CalibrationReader.ReadAsync(path, CancellationToken.None);
    }
}
