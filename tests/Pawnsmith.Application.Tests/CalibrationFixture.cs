using Pawnsmith.Domain;

namespace Pawnsmith.Application.Tests;

/// <summary>
/// A calibration mirroring <c>config/calibration.json</c>, for the tests of the
/// effective-calibration merge.
/// </summary>
/// <remarks>
/// It duplicates the fixture of the domain tests rather than sharing it. The
/// alternatives were worse: a test project referencing another test project, or
/// an <c>InternalsVisibleTo</c> opening one assembly to the other. Twenty lines
/// of literal values are cheaper than either, and a fixture that drifts from
/// reality is caught by the acceptance criteria of B.9, which are measured on
/// paper rather than asserted here.
/// <para>
/// Realistic values are needed, not round ones: the capacity assertions of
/// test 13 only mean something against real millimetres.
/// </para>
/// </remarks>
internal static class CalibrationFixture
{
    public const double MediumPawnHeightMm = 50.0;
    public const double TabWidthMm = 12.0;
    public const double TabHeightMm = 10.0;

    public static readonly PaperFormat A4 = new("A4", WidthMm: 210.0, HeightMm: 297.0);

    public static Calibration Calibration()
    {
        return new Calibration(
            VersionSchema: 1,
            Paper: new PaperNote(GrammageGsm: 250, Note: "reference stock"),
            Sizes: new Dictionary<Size, PawnDimensions>
            {
                [Size.Small] = new(25.4, 25.4, 40.0),
                [Size.Medium] = new(25.4, 25.4, MediumPawnHeightMm),
                [Size.Large] = new(50.8, 50.8, 75.0),
                [Size.Huge] = new(76.2, 76.2, 100.0),
                [Size.Gargantuan] = new(101.6, 101.6, 110.0),
            },
            Geometry: new GeometrySettings(
                FoldedTent: new FoldedTentSettings(FlapHeightMm: 8.0),
                TabAndSocket: new TabAndSocketSettings(TabWidthMm, TabHeightMm)),
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
            PaperFormats: new Dictionary<string, PaperFormat> { ["A4"] = A4 });
    }

    /// <summary>How many units one A4 page holds, using the T1 engine unchanged.</summary>
    /// <remarks>
    /// This goes through <see cref="UnfoldedUnit"/> and <see cref="PageGrid"/>
    /// exactly as the layout use case does. Recomputing the capacity by hand
    /// here would test the arithmetic of the test rather than the coupling
    /// DEC-040 announced.
    /// </remarks>
    public static int CapacityOnA4(Calibration calibration, Size size, Geometry geometry)
    {
        var unit = UnfoldedUnit.Create(
            size,
            calibration.Sizes[size],
            geometry,
            calibration.Geometry);

        return PageGrid.Create(A4, unit, calibration.Layout).Capacity;
    }
}
