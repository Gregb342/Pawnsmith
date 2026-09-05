using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;

namespace Pawnsmith.Infrastructure.Tests.Fixtures;

/// <summary>
/// A calibration for the project tests, mirroring <c>config/calibration.json</c>.
/// </summary>
/// <remarks>
/// Realistic values rather than round ones: the diagnostics compare a tab
/// override against a real pawn width, and a fictional width would make the
/// assertions say nothing.
/// </remarks>
internal static class ProjectCalibration
{
    /// <summary>A calibration declaring exactly the paper formats named.</summary>
    /// <remarks>
    /// The formats are a parameter because one test needs a calibration that
    /// does <b>not</b> know the project's format — that is the whole of the
    /// unknown-format diagnostic.
    /// </remarks>
    public static Calibration WithPaperFormats(params string[] names)
    {
        Dictionary<string, PaperFormat> formats = names.ToDictionary(
            name => name,
            name => new PaperFormat(name, WidthMm: 210.0, HeightMm: 297.0));

        return new Calibration(
            VersionSchema: 1,
            Paper: new PaperNote(GrammageGsm: 250, Note: "reference stock"),
            Sizes: new Dictionary<Size, PawnDimensions>
            {
                [Size.Small] = new(25.4, 25.4, 40.0),
                [Size.Medium] = new(25.4, 25.4, 50.0),
                [Size.Large] = new(50.8, 50.8, 75.0),
                [Size.Huge] = new(76.2, 76.2, 100.0),
                [Size.Gargantuan] = new(101.6, 101.6, 110.0),
            },
            Geometry: new GeometrySettings(
                FoldedTent: new FoldedTentSettings(FlapHeightMm: 8.0),
                TabAndSocket: new TabAndSocketSettings(TabWidthMm: 12.0, TabHeightMm: 10.0)),
            Layout: new LayoutSettings(
                PageMarginMm: 10.0,
                GutterMm: 3.0,
                SilhouetteMarginMm: 1.5,
                CalibrationZoneHeightMm: 14.0),
            Print: new PrintSettings(ScaleCorrectionFactor: 1.0),
            Strokes: new StrokeSettings(
                CutWidthMm: 0.25,
                FoldWidthMm: 0.25,
                ColorHex: "#B0B0B0",
                FoldDashPatternMm: [2.0, 2.0]),
            PaperFormats: formats);
    }
}
