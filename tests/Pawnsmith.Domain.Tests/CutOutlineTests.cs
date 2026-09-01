namespace Pawnsmith.Domain.Tests;

/// <summary>
/// Covers test 9 of B.8: rectangle for <c>FoldedTent</c>, centred-tab polygon
/// for <c>TabAndSocket</c>.
/// </summary>
/// <remarks>
/// Fixture values, not calibration values — see <see cref="UnfoldedUnitTests"/>
/// for why. Width 30 and tab width 12 give tab edges at 9 and 21, which are
/// checkable by hand.
/// </remarks>
public class CutOutlineTests
{
    private const double WidthMm = 30.0;
    private const double PawnHeightMm = 100.0;
    private const double FlapHeightMm = 5.0;
    private const double TabWidthMm = 12.0;
    private const double TabHeightMm = 20.0;

    private const double Tolerance = 1e-9;

    private static readonly PawnDimensions Pawn =
        new(GridFootprintMm: 25.4, PawnWidthMm: WidthMm, PawnHeightMm: PawnHeightMm);

    private static readonly GeometrySettings Settings = new(
        FoldedTent: new FoldedTentSettings(FlapHeightMm),
        TabAndSocket: new TabAndSocketSettings(TabWidthMm, TabHeightMm));

    private static UnfoldedUnit Create(Geometry geometry)
    {
        return UnfoldedUnit.Create(Pawn, geometry, Settings);
    }

    // --- B.8 n° 9 : forme du contour --------------------------------------

    [Fact]
    public void FoldedTentIsAPlainRectangle()
    {
        UnfoldedUnit unit = Create(Geometry.FoldedTent);

        unit.CutOutlineMm.Count.ShouldBe(4);
        unit.CutOutlineMm[0].ShouldBe(new PointMm(0, 0));
        unit.CutOutlineMm[1].ShouldBe(new PointMm(WidthMm, 0));
        unit.CutOutlineMm[2].ShouldBe(new PointMm(WidthMm, unit.TotalHeightMm));
        unit.CutOutlineMm[3].ShouldBe(new PointMm(0, unit.TotalHeightMm));
    }

    [Fact]
    public void TabAndSocketIsARectangleWithATabAtEachEnd()
    {
        UnfoldedUnit unit = Create(Geometry.TabAndSocket);

        unit.CutOutlineMm.Count.ShouldBe(12);
    }

    [Fact]
    public void TheTabIsHorizontallyCentredAndTheRightWidth()
    {
        UnfoldedUnit unit = Create(Geometry.TabAndSocket);

        // The topmost vertices are the two upper corners of the tab.
        IReadOnlyList<PointMm> top = [.. unit.CutOutlineMm.Where(p => p.YMm == 0)];

        top.Count.ShouldBe(2);
        double leftMm = top.Min(p => p.XMm);
        double rightMm = top.Max(p => p.XMm);

        (rightMm - leftMm).ShouldBe(TabWidthMm, Tolerance);
        ((leftMm + rightMm) / 2).ShouldBe(WidthMm / 2, Tolerance);
    }

    [Fact]
    public void TheBodySpansExactlyTheTwoImageBands()
    {
        UnfoldedUnit unit = Create(Geometry.TabAndSocket);

        // The full-width edges are where the body starts and ends.
        IReadOnlyList<double> fullWidthYMm =
            [.. unit.CutOutlineMm.Where(p => p.XMm is 0 or WidthMm).Select(p => p.YMm).Distinct().Order()];

        fullWidthYMm.Count.ShouldBe(2);
        fullWidthYMm[0].ShouldBe(unit.BackImage.TopMm, Tolerance);
        fullWidthYMm[1].ShouldBe(unit.FrontImage.BottomMm, Tolerance);
    }

    // --- Symétries exigées par B.4.3 --------------------------------------

    [Theory]
    [InlineData(Geometry.FoldedTent)]
    [InlineData(Geometry.TabAndSocket)]
    public void OutlineIsSymmetricalAboutTheVerticalAxis(Geometry geometry)
    {
        UnfoldedUnit unit = Create(geometry);
        double axisMm = unit.WidthMm / 2;

        foreach (PointMm point in unit.CutOutlineMm)
        {
            PointMm mirrored = new(2 * axisMm - point.XMm, point.YMm);
            HasVertexAt(unit.CutOutlineMm, mirrored).ShouldBeTrue(
                $"no vertex mirrors {point} about x = {axisMm}");
        }
    }

    [Theory]
    [InlineData(Geometry.FoldedTent)]
    [InlineData(Geometry.TabAndSocket)]
    public void OutlineIsSymmetricalAboutTheFoldLine(Geometry geometry)
    {
        UnfoldedUnit unit = Create(geometry);

        foreach (PointMm point in unit.CutOutlineMm)
        {
            PointMm mirrored = new(point.XMm, 2 * unit.FoldLineYMm - point.YMm);
            HasVertexAt(unit.CutOutlineMm, mirrored).ShouldBeTrue(
                $"no vertex mirrors {point} about y = {unit.FoldLineYMm}");
        }
    }

    [Theory]
    [InlineData(Geometry.FoldedTent)]
    [InlineData(Geometry.TabAndSocket)]
    public void OutlineStaysInsideTheUnit(Geometry geometry)
    {
        UnfoldedUnit unit = Create(geometry);

        foreach (PointMm point in unit.CutOutlineMm)
        {
            point.XMm.ShouldBeInRange(0, unit.WidthMm);
            point.YMm.ShouldBeInRange(0, unit.TotalHeightMm);
        }
    }

    [Theory]
    [InlineData(Geometry.FoldedTent)]
    [InlineData(Geometry.TabAndSocket)]
    public void OutlineIsNotExplicitlyClosed(Geometry geometry)
    {
        // The last vertex joins the first; repeating it would make anything
        // drawing the path emit a zero-length segment.
        UnfoldedUnit unit = Create(geometry);

        unit.CutOutlineMm[^1].ShouldNotBe(unit.CutOutlineMm[0]);
    }

    // --- Garde-fou --------------------------------------------------------

    [Fact]
    public void ATabWiderThanThePawnIsRejected()
    {
        GeometrySettings impossible = new(
            FoldedTent: new FoldedTentSettings(FlapHeightMm),
            TabAndSocket: new TabAndSocketSettings(TabWidthMm: WidthMm + 1, TabHeightMm));

        Should.Throw<ArgumentOutOfRangeException>(
            () => UnfoldedUnit.Create(Pawn, Geometry.TabAndSocket, impossible));
    }

    private static bool HasVertexAt(IReadOnlyList<PointMm> outline, PointMm expected)
    {
        return outline.Any(p =>
            Math.Abs(p.XMm - expected.XMm) < Tolerance &&
            Math.Abs(p.YMm - expected.YMm) < Tolerance);
    }
}
