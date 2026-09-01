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
}
