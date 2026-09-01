namespace Pawnsmith.Domain.Tests;

/// <summary>
/// The calibration values used by the layout tests.
/// </summary>
/// <remarks>
/// These mirror <c>config/calibration.json</c> on purpose, unlike the round
/// fixtures used by the unit-geometry tests. Capacity assertions are only
/// meaningful against realistic dimensions: a page holding twelve Medium pawns
/// says something, a page holding twelve fictional pawns says nothing.
/// <para>
/// They are still declared here rather than read from disk — B.8 requires the
/// domain tests to touch no file system. If the real calibration changes, these
/// tests keep passing and stop describing reality; the paper-print criteria of
/// B.9 are what catch that, not these tests.
/// </para>
/// </remarks>
internal static class CalibrationFixture
{
    public static readonly PaperFormat A4 = new("A4", WidthMm: 210.0, HeightMm: 297.0);

    public static readonly PaperFormat Letter = new("Letter", WidthMm: 216.0, HeightMm: 279.0);

    public static readonly LayoutSettings Layout = new(
        PageMarginMm: 10.0,
        GutterMm: 3.0,
        SilhouetteMarginMm: 1.5,
        CalibrationZoneHeightMm: 14.0);

    public static readonly GeometrySettings Geometry = new(
        FoldedTent: new FoldedTentSettings(FlapHeightMm: 8.0),
        TabAndSocket: new TabAndSocketSettings(TabWidthMm: 12.0, TabHeightMm: 10.0));

    public static PawnDimensions Pawn(Size size)
    {
        return size switch
        {
            Size.Small => new PawnDimensions(25.4, 25.4, 40.0),
            Size.Medium => new PawnDimensions(25.4, 25.4, 50.0),
            Size.Large => new PawnDimensions(50.8, 50.8, 75.0),
            Size.Huge => new PawnDimensions(76.2, 76.2, 100.0),
            Size.Gargantuan => new PawnDimensions(101.6, 101.6, 110.0),
            _ => throw new ArgumentOutOfRangeException(nameof(size), size, null),
        };
    }

    public static UnfoldedUnit Unit(Size size, Geometry geometry)
    {
        return UnfoldedUnit.Create(size, Pawn(size), geometry, Geometry);
    }

    /// <summary>The whole calibration graph, with every size present.</summary>
    public static Calibration Calibration { get; } = Build(AllSizes());

    /// <summary>Carries the Gargantuan height that shipped in v1.1 (DEC-032).</summary>
    public static Calibration CalibrationWithImpossibleGargantuan { get; } = Build(
        AllSizes().ToDictionary(
            entry => entry.Key,
            entry => entry.Key == Size.Gargantuan
                ? new PawnDimensions(101.6, 101.6, 125.0)
                : entry.Value));

    /// <summary>Missing one size, to exercise the lookup failure.</summary>
    public static Calibration CalibrationWithoutSmall { get; } = Build(
        AllSizes().Where(entry => entry.Key != Size.Small)
            .ToDictionary(entry => entry.Key, entry => entry.Value));

    private static Dictionary<Size, PawnDimensions> AllSizes()
    {
        return Enum.GetValues<Size>().ToDictionary(size => size, Pawn);
    }

    private static Calibration Build(IReadOnlyDictionary<Size, PawnDimensions> sizes)
    {
        return new Calibration(
            VersionSchema: 1,
            Paper: new PaperNote(250, "Fixture."),
            Sizes: sizes,
            Geometry: Geometry,
            Layout: Layout,
            Print: new PrintSettings(ScaleCorrectionFactor: 1.0),
            Strokes: new StrokeSettings(
                CutWidthMm: 0.25,
                FoldWidthMm: 0.25,
                ColorHex: "#B0B0B0",
                FoldDashPatternMm: [2.0, 2.0]),
            PaperFormats: new Dictionary<string, PaperFormat>
            {
                [A4.Name] = A4,
                [Letter.Name] = Letter,
            });
    }
}
