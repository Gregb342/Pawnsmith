using System.Text.Json;
using System.Text.Json.Serialization;

using Pawnsmith.Domain;

namespace Pawnsmith.Infrastructure;

/// <summary>
/// Reads <c>config/calibration.json</c> into the domain's calibration graph.
/// </summary>
/// <remarks>
/// The mapping is written out by hand, field by field. It is verbose on
/// purpose: DEC-021 rejects automatic mapping precisely because it is invisible
/// in review, and a silent mismatch here would move a pawn by millimetres
/// without anything failing.
/// <para>
/// <c>System.Text.Json</c> ships with .NET and adds no dependency (A.2).
/// </para>
/// </remarks>
public static class CalibrationReader
{
    /// <summary>Schema version this reader understands.</summary>
    private const int SupportedVersionSchema = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Reads and validates a calibration file.</summary>
    /// <exception cref="ManifestException">The file is missing, malformed or incomplete.</exception>
    public static async Task<Calibration> ReadAsync(string path, CancellationToken cancellationToken)
    {
        CalibrationDocument document = await JsonFile
            .ReadAsync<CalibrationDocument>(path, Options, "calibration file", cancellationToken)
            .ConfigureAwait(false);

        if (document.VersionSchema != SupportedVersionSchema)
        {
            throw new ManifestException(
                $"Calibration file '{path}' declares schema version {document.VersionSchema}; " +
                $"only version {SupportedVersionSchema} is supported.");
        }

        return Map(document, path);
    }

    private static Calibration Map(CalibrationDocument document, string path)
    {
        Require(document.Sizes, "sizes", path);
        Require(document.Geometry, "geometry", path);
        Require(document.Layout, "layout", path);
        Require(document.Print, "print", path);
        Require(document.Strokes, "strokes", path);
        Require(document.PaperFormats, "paperFormats", path);

        return new Calibration(
            VersionSchema: document.VersionSchema,
            // The paper block is documentary and no calculation reads it. It is
            // accepted and ignored, never rejected (B.2, test 19 of B.8).
            Paper: new PaperNote(
                document.Paper?.GrammageGsm ?? 0,
                document.Paper?.Note ?? string.Empty),
            Sizes: MapSizes(document.Sizes!, path),
            Geometry: MapGeometry(document.Geometry!, path),
            Layout: new LayoutSettings(
                document.Layout!.PageMarginMm,
                document.Layout.GutterMm,
                document.Layout.SilhouetteMarginMm,
                document.Layout.CalibrationZoneHeightMm),
            Print: new PrintSettings(document.Print!.ScaleCorrectionFactor),
            Strokes: new StrokeSettings(
                document.Strokes!.CutWidthMm,
                document.Strokes.FoldWidthMm,
                document.Strokes.ColorHex ?? "#000000",
                document.Strokes.FoldDashPatternMm ?? []),
            PaperFormats: MapPaperFormats(document.PaperFormats!, path));
    }

    private static IReadOnlyDictionary<Size, PawnDimensions> MapSizes(
        Dictionary<string, PawnDimensionsDocument> sizes,
        string path)
    {
        Dictionary<Size, PawnDimensions> mapped = [];

        foreach ((string name, PawnDimensionsDocument dimensions) in sizes)
        {
            if (!Enum.TryParse(name, ignoreCase: false, out Size size))
            {
                throw new ManifestException(
                    $"Calibration file '{path}' declares an unknown size '{name}'. " +
                    $"Known sizes are: {string.Join(", ", Enum.GetNames<Size>())}.");
            }

            mapped[size] = new PawnDimensions(
                dimensions.GridFootprintMm,
                dimensions.PawnWidthMm,
                dimensions.PawnHeightMm);
        }

        return mapped;
    }

    private static GeometrySettings MapGeometry(GeometryDocument geometry, string path)
    {
        Require(geometry.FoldedTent, "geometry.foldedTent", path);
        Require(geometry.TabAndSocket, "geometry.tabAndSocket", path);

        return new GeometrySettings(
            new FoldedTentSettings(geometry.FoldedTent!.FlapHeightMm),
            new TabAndSocketSettings(
                geometry.TabAndSocket!.TabWidthMm,
                geometry.TabAndSocket.TabHeightMm));
    }

    private static IReadOnlyDictionary<string, PaperFormat> MapPaperFormats(
        Dictionary<string, PaperFormatDocument> formats,
        string path)
    {
        if (formats.Count == 0)
        {
            throw new ManifestException($"Calibration file '{path}' declares no paper format.");
        }

        return formats.ToDictionary(
            entry => entry.Key,
            entry => new PaperFormat(entry.Key, entry.Value.WidthMm, entry.Value.HeightMm));
    }

    private static void Require(object? value, string field, string path)
    {
        if (value is null)
        {
            throw new ManifestException($"Calibration file '{path}' is missing the '{field}' block.");
        }
    }

    // --- Documents de sérialisation --------------------------------------
    // Un jeu de types plat, calqué sur le fichier, distinct des types du
    // domaine. Le domaine ne porte aucun attribut de sérialisation : il ne
    // référence rien.

    private sealed record CalibrationDocument
    {
        [JsonPropertyName("versionSchema")]
        public int VersionSchema { get; init; }

        [JsonPropertyName("paper")]
        public PaperDocument? Paper { get; init; }

        [JsonPropertyName("sizes")]
        public Dictionary<string, PawnDimensionsDocument>? Sizes { get; init; }

        [JsonPropertyName("geometry")]
        public GeometryDocument? Geometry { get; init; }

        [JsonPropertyName("layout")]
        public LayoutDocument? Layout { get; init; }

        [JsonPropertyName("print")]
        public PrintDocument? Print { get; init; }

        [JsonPropertyName("strokes")]
        public StrokesDocument? Strokes { get; init; }

        [JsonPropertyName("paperFormats")]
        public Dictionary<string, PaperFormatDocument>? PaperFormats { get; init; }
    }

    private sealed record PaperDocument
    {
        [JsonPropertyName("grammageGsm")]
        public int GrammageGsm { get; init; }

        [JsonPropertyName("note")]
        public string? Note { get; init; }
    }

    private sealed record PawnDimensionsDocument
    {
        [JsonPropertyName("gridFootprintMm")]
        public double GridFootprintMm { get; init; }

        [JsonPropertyName("pawnWidthMm")]
        public double PawnWidthMm { get; init; }

        [JsonPropertyName("pawnHeightMm")]
        public double PawnHeightMm { get; init; }
    }

    private sealed record GeometryDocument
    {
        [JsonPropertyName("foldedTent")]
        public FoldedTentDocument? FoldedTent { get; init; }

        [JsonPropertyName("tabAndSocket")]
        public TabAndSocketDocument? TabAndSocket { get; init; }
    }

    private sealed record FoldedTentDocument
    {
        [JsonPropertyName("flapHeightMm")]
        public double FlapHeightMm { get; init; }
    }

    private sealed record TabAndSocketDocument
    {
        [JsonPropertyName("tabWidthMm")]
        public double TabWidthMm { get; init; }

        [JsonPropertyName("tabHeightMm")]
        public double TabHeightMm { get; init; }
    }

    private sealed record LayoutDocument
    {
        [JsonPropertyName("pageMarginMm")]
        public double PageMarginMm { get; init; }

        [JsonPropertyName("gutterMm")]
        public double GutterMm { get; init; }

        [JsonPropertyName("silhouetteMarginMm")]
        public double SilhouetteMarginMm { get; init; }

        [JsonPropertyName("calibrationZoneHeightMm")]
        public double CalibrationZoneHeightMm { get; init; }
    }

    private sealed record PrintDocument
    {
        [JsonPropertyName("scaleCorrectionFactor")]
        public double ScaleCorrectionFactor { get; init; }
    }

    private sealed record StrokesDocument
    {
        [JsonPropertyName("cutWidthMm")]
        public double CutWidthMm { get; init; }

        [JsonPropertyName("foldWidthMm")]
        public double FoldWidthMm { get; init; }

        [JsonPropertyName("colorHex")]
        public string? ColorHex { get; init; }

        [JsonPropertyName("foldDashPatternMm")]
        public double[]? FoldDashPatternMm { get; init; }
    }

    private sealed record PaperFormatDocument
    {
        [JsonPropertyName("widthMm")]
        public double WidthMm { get; init; }

        [JsonPropertyName("heightMm")]
        public double HeightMm { get; init; }
    }
}
