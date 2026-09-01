namespace Pawnsmith.Domain.Tests;

/// <summary>
/// Covers tests 7 and 8 of B.8, plus the invariants they rest on.
/// </summary>
/// <remarks>
/// The numbers below are <b>test fixtures, not calibration values</b>. They are
/// deliberately round and deliberately unlike the provisional calibration, so
/// that a change to config/calibration.json can never make these tests fail,
/// and so that a reader can check the arithmetic in their head.
/// </remarks>
public class UnfoldedUnitTests
{
    private const double WidthMm = 30.0;
    private const double PawnHeightMm = 100.0;
    private const double FlapHeightMm = 5.0;
    private const double TabHeightMm = 20.0;

    private const double Tolerance = 1e-9;

    private static readonly PawnDimensions Pawn =
        new(GridFootprintMm: 25.4, PawnWidthMm: WidthMm, PawnHeightMm: PawnHeightMm);

    private static readonly GeometrySettings Settings = new(
        FoldedTent: new FoldedTentSettings(FlapHeightMm),
        TabAndSocket: new TabAndSocketSettings(TabWidthMm: 12.0, TabHeightMm: TabHeightMm));

    private static UnfoldedUnit Create(Geometry geometry)
    {
        return UnfoldedUnit.Create(Pawn, geometry, Settings);
    }

    // --- B.8 n° 8 : hauteur totale dépliée -------------------------------

    [Theory]
    [InlineData(Geometry.FoldedTent, 2 * (PawnHeightMm + FlapHeightMm))]
    [InlineData(Geometry.TabAndSocket, 2 * (PawnHeightMm + TabHeightMm))]
    public void TotalHeightFollowsTheFormula(Geometry geometry, double expectedMm)
    {
        UnfoldedUnit unit = Create(geometry);

        unit.TotalHeightMm.ShouldBe(expectedMm, Tolerance);
    }

    [Theory]
    [InlineData(Geometry.FoldedTent, FlapHeightMm)]
    [InlineData(Geometry.TabAndSocket, TabHeightMm)]
    public void AppendixHeightComesFromTheGeometryItBelongsTo(Geometry geometry, double expectedMm)
    {
        UnfoldedUnit unit = Create(geometry);

        unit.FrontAppendix.HeightMm.ShouldBe(expectedMm, Tolerance);
        unit.BackAppendix.HeightMm.ShouldBe(expectedMm, Tolerance);
    }

    // --- B.8 n° 7 : position de la ligne de pliage -----------------------

    [Theory]
    [InlineData(Geometry.FoldedTent)]
    [InlineData(Geometry.TabAndSocket)]
    public void FoldLineSitsExactlyAtTheTopOfTheFrontImage(Geometry geometry)
    {
        UnfoldedUnit unit = Create(geometry);

        unit.FoldLineYMm.ShouldBe(unit.FrontImage.TopMm, Tolerance);
    }

    [Theory]
    [InlineData(Geometry.FoldedTent)]
    [InlineData(Geometry.TabAndSocket)]
    public void FoldLineSitsAtHalfTheTotalHeight(Geometry geometry)
    {
        // Consequence of the unit being symmetrical about the fold: worth
        // asserting separately, because it is what makes the folded halves
        // line up.
        UnfoldedUnit unit = Create(geometry);

        unit.FoldLineYMm.ShouldBe(unit.TotalHeightMm / 2, Tolerance);
    }

    // --- Invariants de structure -----------------------------------------

    [Theory]
    [InlineData(Geometry.FoldedTent)]
    [InlineData(Geometry.TabAndSocket)]
    public void BandsTileTheUnitWithoutGapOrOverlap(Geometry geometry)
    {
        UnfoldedUnit unit = Create(geometry);

        unit.BackAppendix.TopMm.ShouldBe(0, Tolerance);
        unit.BackImage.TopMm.ShouldBe(unit.BackAppendix.BottomMm, Tolerance);
        unit.FrontImage.TopMm.ShouldBe(unit.BackImage.BottomMm, Tolerance);
        unit.FrontAppendix.TopMm.ShouldBe(unit.FrontImage.BottomMm, Tolerance);
        unit.FrontAppendix.BottomMm.ShouldBe(unit.TotalHeightMm, Tolerance);
    }

    [Theory]
    [InlineData(Geometry.FoldedTent)]
    [InlineData(Geometry.TabAndSocket)]
    public void TheTwoImageBandsAreTheSameHeight(Geometry geometry)
    {
        UnfoldedUnit unit = Create(geometry);

        unit.BackImage.HeightMm.ShouldBe(PawnHeightMm, Tolerance);
        unit.FrontImage.HeightMm.ShouldBe(PawnHeightMm, Tolerance);
    }

    [Theory]
    [InlineData(Geometry.FoldedTent)]
    [InlineData(Geometry.TabAndSocket)]
    public void WidthIsThePawnWidth(Geometry geometry)
    {
        UnfoldedUnit unit = Create(geometry);

        unit.WidthMm.ShouldBe(WidthMm, Tolerance);
    }

    // --- Lignes de pliage tracées (B.4.2, B.5.4) --------------------------

    [Fact]
    public void FoldedTentHasThreeFoldsAtTheImageBoundaries()
    {
        UnfoldedUnit unit = Create(Geometry.FoldedTent);

        unit.FoldLinesYMm.Count.ShouldBe(3);
        unit.FoldLinesYMm[0].ShouldBe(unit.BackImage.TopMm, Tolerance);
        unit.FoldLinesYMm[1].ShouldBe(unit.FoldLineYMm, Tolerance);
        unit.FoldLinesYMm[2].ShouldBe(unit.FrontImage.BottomMm, Tolerance);
    }

    [Fact]
    public void TabAndSocketHasTheMainFoldOnly()
    {
        // The tab is rigid with the figure and slides into the socket: folding
        // it would be a defect, not a feature.
        UnfoldedUnit unit = Create(Geometry.TabAndSocket);

        unit.FoldLinesYMm.Count.ShouldBe(1);
        unit.FoldLinesYMm[0].ShouldBe(unit.FoldLineYMm, Tolerance);
    }
}
