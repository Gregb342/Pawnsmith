namespace Pawnsmith.Domain.Tests;

/// <summary>
/// Covers tests 6 and 10 of B.8. Test 6 is the one that matters most: omitting
/// the half turn produces a character standing on its head after folding.
/// </summary>
/// <remarks>
/// Fixture values, not calibration values. Width 30, pawn height 100, margin 2
/// give a box of 26 × 98 mm, which is checkable by hand.
/// </remarks>
public class ImagePlacementTests
{
    private const double WidthMm = 30.0;
    private const double PawnHeightMm = 100.0;
    private const double TabHeightMm = 20.0;
    private const double MarginMm = 2.0;

    private const double Tolerance = 1e-9;

    private static readonly PawnDimensions Pawn =
        new(GridFootprintMm: 25.4, PawnWidthMm: WidthMm, PawnHeightMm: PawnHeightMm);

    private static readonly GeometrySettings Geometry_ = new(
        FoldedTent: new FoldedTentSettings(FlapHeightMm: 5.0),
        TabAndSocket: new TabAndSocketSettings(TabWidthMm: 12.0, TabHeightMm: TabHeightMm));

    private static readonly LayoutSettings Layout = new(
        PageMarginMm: 10.0,
        GutterMm: 3.0,
        SilhouetteMarginMm: MarginMm,
        CalibrationZoneHeightMm: 14.0);

    /// <summary>Taller than its box is wide: the height is the limiting side.</summary>
    private static readonly SourceImageSize TallSource = new(WidthPx: 100, HeightPx: 1000);

    /// <summary>Wider than its box is tall: the width is the limiting side.</summary>
    private static readonly SourceImageSize WideSource = new(WidthPx: 1000, HeightPx: 100);

    private static UnfoldedUnit Unit()
    {
        return UnfoldedUnit.Create(Size.Medium, Pawn, Geometry.TabAndSocket, Geometry_);
    }

    // --- B.8 n° 6 : le verso est tourné et placé au-dessus du pli ---------

    [Fact]
    public void BackImageIsTurnedByAHalfTurn()
    {
        var back = ImagePlacement.ForBack(Unit(), TallSource, Layout);

        back.Rotation.ShouldBe(ImageRotation.HalfTurn);
    }

    [Fact]
    public void FrontImageIsNotTurned()
    {
        var front = ImagePlacement.ForFront(Unit(), TallSource, Layout);

        front.Rotation.ShouldBe(ImageRotation.None);
    }

    [Fact]
    public void BackImageSitsEntirelyAboveTheFoldLine()
    {
        UnfoldedUnit unit = Unit();

        var back = ImagePlacement.ForBack(unit, TallSource, Layout);

        back.BottomMm.ShouldBeLessThanOrEqualTo(unit.FoldLineYMm + Tolerance);
    }

    [Fact]
    public void FrontImageSitsEntirelyBelowTheFoldLine()
    {
        UnfoldedUnit unit = Unit();

        var front = ImagePlacement.ForFront(unit, TallSource, Layout);

        front.YMm.ShouldBeGreaterThanOrEqualTo(unit.FoldLineYMm - Tolerance);
    }

    // --- B.8 n° 10 : rapport d'aspect, ligne des pieds, marge -------------

    [Theory]
    [InlineData(100, 1000)]
    [InlineData(1000, 100)]
    [InlineData(640, 480)]
    public void AspectRatioIsPreserved(double widthPx, double heightPx)
    {
        SourceImageSize source = new(widthPx, heightPx);
        UnfoldedUnit unit = Unit();

        var front = ImagePlacement.ForFront(unit, source, Layout);

        (front.WidthMm / front.HeightMm).ShouldBe(widthPx / heightPx, Tolerance);
    }

    [Fact]
    public void FrontImageStandsOnTheBottomOfItsBand()
    {
        // The feet line of the front panel is its boundary with the front
        // appendix, below it.
        UnfoldedUnit unit = Unit();

        var front = ImagePlacement.ForFront(unit, TallSource, Layout);

        front.BottomMm.ShouldBe(unit.FrontImage.BottomMm, Tolerance);
    }

    [Fact]
    public void BackImageStandsOnTheTopOfItsBand()
    {
        // The feet line of the back panel is its boundary with the back
        // appendix, above it. The two feet lines face each other across the
        // fold, which is what makes the back land the right way up.
        UnfoldedUnit unit = Unit();

        var back = ImagePlacement.ForBack(unit, TallSource, Layout);

        back.YMm.ShouldBe(unit.BackImage.TopMm, Tolerance);
    }

    [Fact]
    public void ShortImageIsNotVerticallyCentred()
    {
        // A wide, short source leaves room above the head. That room must all
        // be above, never split.
        UnfoldedUnit unit = Unit();

        var front = ImagePlacement.ForFront(unit, WideSource, Layout);

        front.HeightMm.ShouldBeLessThan(unit.FrontImage.HeightMm / 2);
        front.BottomMm.ShouldBe(unit.FrontImage.BottomMm, Tolerance);
    }

    [Theory]
    [InlineData(100, 1000)]
    [InlineData(1000, 100)]
    public void ImageIsHorizontallyCentred(double widthPx, double heightPx)
    {
        SourceImageSize source = new(widthPx, heightPx);
        UnfoldedUnit unit = Unit();

        var front = ImagePlacement.ForFront(unit, source, Layout);

        (front.XMm + (front.WidthMm / 2)).ShouldBe(unit.WidthMm / 2, Tolerance);
    }

    [Theory]
    [InlineData(100, 1000)]
    [InlineData(1000, 100)]
    public void SilhouetteMarginIsRespectedOnTheSidesAndAbove(double widthPx, double heightPx)
    {
        SourceImageSize source = new(widthPx, heightPx);
        UnfoldedUnit unit = Unit();

        var front = ImagePlacement.ForFront(unit, source, Layout);

        front.XMm.ShouldBeGreaterThanOrEqualTo(MarginMm - Tolerance);
        (unit.WidthMm - front.XMm - front.WidthMm).ShouldBeGreaterThanOrEqualTo(MarginMm - Tolerance);
        front.YMm.ShouldBeGreaterThanOrEqualTo(unit.FrontImage.TopMm + MarginMm - Tolerance);
    }

    [Fact]
    public void NoMarginIsLeftBelowTheFeet()
    {
        // The margin applies to the sides and the top only: the drawing has to
        // reach the fold or the tab.
        UnfoldedUnit unit = Unit();

        var front = ImagePlacement.ForFront(unit, TallSource, Layout);

        front.BottomMm.ShouldBe(unit.FrontImage.BottomMm, Tolerance);
    }

    [Fact]
    public void ImageFillsTheLimitingSideOfItsBox()
    {
        // A tall source is limited by the box height, so it must reach it
        // exactly — otherwise the pawn would print shorter than its size.
        UnfoldedUnit unit = Unit();

        var front = ImagePlacement.ForFront(unit, TallSource, Layout);

        front.HeightMm.ShouldBe(unit.FrontImage.HeightMm - MarginMm, Tolerance);
    }

    [Fact]
    public void ASourceSmallerThanItsBoxIsScaledUp()
    {
        UnfoldedUnit unit = Unit();
        SourceImageSize tiny = new(WidthPx: 10, HeightPx: 100);

        var front = ImagePlacement.ForFront(unit, tiny, Layout);

        front.HeightMm.ShouldBeGreaterThan(100.0 / 25.4);
    }

    // --- Garde-fous -------------------------------------------------------

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 0)]
    [InlineData(-1, 100)]
    public void ANonPositiveSourceSizeIsRejected(double widthPx, double heightPx)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => ImagePlacement.ForFront(Unit(), new SourceImageSize(widthPx, heightPx), Layout));
    }

    [Fact]
    public void ASilhouetteMarginLeavingNoRoomIsRejected()
    {
        LayoutSettings impossible = Layout with { SilhouetteMarginMm = WidthMm };

        Should.Throw<ArgumentOutOfRangeException>(
            () => ImagePlacement.ForFront(Unit(), TallSource, impossible));
    }
}
