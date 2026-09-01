namespace Pawnsmith.Domain.Tests;

/// <summary>
/// The third geometry of DEC-039: nothing below the feet line.
/// </summary>
/// <remarks>
/// Its invariants are the ones the two other geometries already have, plus the
/// two that make it different: a zero appendix and a plain rectangle outline.
/// </remarks>
public class NoSupportGeometryTests
{
    private const double WidthMm = 30.0;
    private const double PawnHeightMm = 100.0;
    private const double Tolerance = 1e-9;

    private static readonly PawnDimensions Pawn =
        new(GridFootprintMm: 25.4, PawnWidthMm: WidthMm, PawnHeightMm: PawnHeightMm);

    private static readonly GeometrySettings Settings = new(
        FoldedTent: new FoldedTentSettings(FlapHeightMm: 5.0),
        TabAndSocket: new TabAndSocketSettings(TabWidthMm: 12.0, TabHeightMm: 20.0));

    private static UnfoldedUnit Unit()
    {
        return UnfoldedUnit.Create(Size.Medium, Pawn, Geometry.NoSupport, Settings);
    }

    [Fact]
    public void TheUnitIsExactlyTwiceThePawnHeight()
    {
        // No appendix, so the formula 2 × (height + appendix) collapses.
        Unit().TotalHeightMm.ShouldBe(2 * PawnHeightMm, Tolerance);
    }

    [Fact]
    public void BothAppendixBandsHaveZeroHeight()
    {
        UnfoldedUnit unit = Unit();

        unit.BackAppendix.HeightMm.ShouldBe(0, Tolerance);
        unit.FrontAppendix.HeightMm.ShouldBe(0, Tolerance);
    }

    [Fact]
    public void TheImageBandsFillTheWholeUnit()
    {
        UnfoldedUnit unit = Unit();

        unit.BackImage.TopMm.ShouldBe(0, Tolerance);
        unit.FrontImage.BottomMm.ShouldBe(unit.TotalHeightMm, Tolerance);
    }

    [Fact]
    public void TheFoldStillSitsAtTheTopOfTheFrontImage()
    {
        // The invariant of B.4.1 holds whatever the geometry.
        UnfoldedUnit unit = Unit();

        unit.FoldLineYMm.ShouldBe(unit.FrontImage.TopMm, Tolerance);
        unit.FoldLineYMm.ShouldBe(unit.TotalHeightMm / 2, Tolerance);
    }

    [Fact]
    public void ThereIsExactlyOneFoldLine()
    {
        Unit().FoldLinesYMm.Count.ShouldBe(1);
    }

    [Fact]
    public void TheOutlineIsAPlainRectangle()
    {
        UnfoldedUnit unit = Unit();

        unit.CutOutlineMm.Count.ShouldBe(4);
        unit.CutOutlineMm[0].ShouldBe(new PointMm(0, 0));
        unit.CutOutlineMm[2].ShouldBe(new PointMm(WidthMm, unit.TotalHeightMm));
    }

    [Fact]
    public void TheFeetLinesAreTheOuterEdgesOfTheUnit()
    {
        // With no appendix, the boundary between an image and its appendix is
        // the edge of the unit itself. The artwork must reach it.
        UnfoldedUnit unit = Unit();
        LayoutSettings layout = new(10.0, 3.0, SilhouetteMarginMm: 1.5, 14.0);
        SourceImageSize source = new(100, 1000);

        var front = ImagePlacement.ForFront(unit, source, layout);
        var back = ImagePlacement.ForBack(unit, source, layout);

        front.BottomMm.ShouldBe(unit.TotalHeightMm, Tolerance);
        back.YMm.ShouldBe(0, Tolerance);
        back.Rotation.ShouldBe(ImageRotation.HalfTurn);
    }

    [Fact]
    public void SmallGainsARowOnA4WithoutAnAppendix()
    {
        // The gain is real but narrow, and it is worth pinning down rather than
        // claiming in general: with the provisional heights, Small on A4 is the
        // only combination that gains anything — 100 mm of cell against 80 lets
        // a third row through. T0b may move that boundary.
        Capacity(Size.Small, Geometry.NoSupport).ShouldBe(18);
        Capacity(Size.Small, Geometry.TabAndSocket).ShouldBe(12);
    }

    [Theory]
    [InlineData(Size.Medium)]
    [InlineData(Size.Large)]
    [InlineData(Size.Huge)]
    [InlineData(Size.Gargantuan)]
    public void RemovingTheAppendixNeverCostsCapacity(Size size)
    {
        // A shorter cell can never fit worse. Where no row is gained, the count
        // must at least stay put.
        Capacity(size, Geometry.NoSupport)
            .ShouldBeGreaterThanOrEqualTo(Capacity(size, Geometry.TabAndSocket));
    }

    private static int Capacity(Size size, Geometry geometry)
    {
        return PageGrid.Create(
            CalibrationFixture.A4,
            CalibrationFixture.Unit(size, geometry),
            CalibrationFixture.Layout).Capacity;
    }

    [Theory]
    [InlineData(Size.Small)]
    [InlineData(Size.Medium)]
    [InlineData(Size.Large)]
    [InlineData(Size.Huge)]
    [InlineData(Size.Gargantuan)]
    public void EverySizeStillFitsOnAPage(Size size)
    {
        // Removing the appendix can only shorten the cell, so a size that fitted
        // with one must still fit without.
        var grid = PageGrid.Create(
            CalibrationFixture.A4,
            CalibrationFixture.Unit(size, Geometry.NoSupport),
            CalibrationFixture.Layout);

        grid.Capacity.ShouldBeGreaterThan(0);
    }
}
