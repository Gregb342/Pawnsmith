namespace Pawnsmith.Domain.Tests;

/// <summary>
/// Covers tests 6 and 10 of B.8, plus DEC-041 (the pair shares one scale) and
/// the width-limited report of DEC-042.
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

    private const double BoxWidthMm = WidthMm - (2 * MarginMm);
    private const double BoxHeightMm = PawnHeightMm - MarginMm;

    private const double Tolerance = 1e-9;

    private static readonly PawnDimensions Pawn =
        new(GridFootprintMm: 25.4, PawnWidthMm: WidthMm, PawnHeightMm: PawnHeightMm);

    private static readonly GeometrySettings GeometrySettings = new(
        FoldedTent: new FoldedTentSettings(FlapHeightMm: 5.0),
        TabAndSocket: new TabAndSocketSettings(TabWidthMm: 12.0, TabHeightMm: TabHeightMm));

    private static readonly LayoutSettings Layout = new(
        PageMarginMm: 10.0,
        GutterMm: 3.0,
        SilhouetteMarginMm: MarginMm,
        CalibrationZoneHeightMm: 14.0);

    /// <summary>Narrow and tall: the height is the limiting side.</summary>
    private static readonly SourceImageSize Tall = new(WidthPx: 100, HeightPx: 1000);

    /// <summary>Wide and short: the width is the limiting side.</summary>
    private static readonly SourceImageSize Wide = new(WidthPx: 1000, HeightPx: 100);

    private static UnfoldedUnit Unit()
    {
        return UnfoldedUnit.Create(Size.Medium, Pawn, Geometry.TabAndSocket, GeometrySettings);
    }

    private static ImagePair Pair(SourceImageSize front, SourceImageSize? back = null)
    {
        return ImagePlacement.ForPair(Unit(), front, back ?? front, Layout);
    }

    // --- B.8 n° 6 : le verso est tourné et placé au-dessus du pli ---------

    [Fact]
    public void BackImageIsTurnedByAHalfTurnAndFrontIsNot()
    {
        ImagePair pair = Pair(Tall);

        pair.Back.Rotation.ShouldBe(ImageRotation.HalfTurn);
        pair.Front.Rotation.ShouldBe(ImageRotation.None);
    }

    [Fact]
    public void BackSitsAboveTheFoldAndFrontBelow()
    {
        UnfoldedUnit unit = Unit();
        ImagePair pair = Pair(Tall);

        pair.Back.BottomMm.ShouldBeLessThanOrEqualTo(unit.FoldLineYMm + Tolerance);
        pair.Front.YMm.ShouldBeGreaterThanOrEqualTo(unit.FoldLineYMm - Tolerance);
    }

    // --- DEC-041 : le couple partage une échelle unique -------------------

    [Fact]
    public void TheTwoFacesShareOneScale()
    {
        // Two views of the same character with different pixel dimensions: a
        // raised weapon on the back is enough. Scaled independently they would
        // be magnified differently; scaled as a pair they must not be.
        ImagePair pair = Pair(new SourceImageSize(100, 1000), new SourceImageSize(110, 1000));

        double frontScale = pair.Front.WidthMm / 100;
        double backScale = pair.Back.WidthMm / 110;

        frontScale.ShouldBe(backScale, Tolerance);
    }

    [Fact]
    public void TheSharedScaleIsTheOneThatLetsBothFit()
    {
        // The back is wider, so it is the back that decides. Taking the front's
        // scale would push the back past its box.
        ImagePair pair = Pair(new SourceImageSize(100, 1000), new SourceImageSize(200, 1000));

        pair.Back.WidthMm.ShouldBeLessThanOrEqualTo(BoxWidthMm + Tolerance);
        pair.Front.WidthMm.ShouldBeLessThanOrEqualTo(BoxWidthMm + Tolerance);
        (pair.Back.WidthMm / 200).ShouldBe(pair.Front.WidthMm / 100, Tolerance);
    }

    [Fact]
    public void AFaceThatWouldHaveBeenBiggerAloneIsHeldBackByItsPartner()
    {
        // The measured defect of DEC-041, in miniature: the narrow face alone
        // would fill its box, but it must follow the wide one.
        ImagePair alone = Pair(new SourceImageSize(100, 1000));
        ImagePair paired = Pair(new SourceImageSize(100, 1000), new SourceImageSize(400, 1000));

        paired.Front.HeightMm.ShouldBeLessThan(alone.Front.HeightMm);
    }

    // --- B.8 n° 10 : rapport d'aspect, ligne des pieds, marge -------------

    [Theory]
    [InlineData(100, 1000)]
    [InlineData(1000, 100)]
    [InlineData(640, 480)]
    public void AspectRatioIsPreserved(double widthPx, double heightPx)
    {
        ImagePair pair = Pair(new SourceImageSize(widthPx, heightPx));

        (pair.Front.WidthMm / pair.Front.HeightMm).ShouldBe(widthPx / heightPx, Tolerance);
        (pair.Back.WidthMm / pair.Back.HeightMm).ShouldBe(widthPx / heightPx, Tolerance);
    }

    [Fact]
    public void EachFaceStandsOnItsOwnFeetLine()
    {
        // The front's feet line is the bottom of its band; the back's is the top
        // of its own. The two face each other across the fold, which is what
        // makes the back land the right way up once folded.
        UnfoldedUnit unit = Unit();
        ImagePair pair = Pair(Tall);

        pair.Front.BottomMm.ShouldBe(unit.FrontImage.BottomMm, Tolerance);
        pair.Back.YMm.ShouldBe(unit.BackImage.TopMm, Tolerance);
    }

    [Fact]
    public void ShortImageIsNotVerticallyCentred()
    {
        // A wide, short source leaves room above the head. That room must all
        // be above, never split.
        UnfoldedUnit unit = Unit();
        ImagePair pair = Pair(Wide);

        pair.Front.HeightMm.ShouldBeLessThan(unit.FrontImage.HeightMm / 2);
        pair.Front.BottomMm.ShouldBe(unit.FrontImage.BottomMm, Tolerance);
    }

    [Theory]
    [InlineData(100, 1000)]
    [InlineData(1000, 100)]
    public void ImagesAreHorizontallyCentred(double widthPx, double heightPx)
    {
        UnfoldedUnit unit = Unit();
        ImagePair pair = Pair(new SourceImageSize(widthPx, heightPx));

        (pair.Front.XMm + (pair.Front.WidthMm / 2)).ShouldBe(unit.WidthMm / 2, Tolerance);
        (pair.Back.XMm + (pair.Back.WidthMm / 2)).ShouldBe(unit.WidthMm / 2, Tolerance);
    }

    [Theory]
    [InlineData(100, 1000)]
    [InlineData(1000, 100)]
    public void SilhouetteMarginIsRespectedOnTheSidesAndAbove(double widthPx, double heightPx)
    {
        UnfoldedUnit unit = Unit();
        ImagePair pair = Pair(new SourceImageSize(widthPx, heightPx));

        pair.Front.XMm.ShouldBeGreaterThanOrEqualTo(MarginMm - Tolerance);
        pair.Front.YMm.ShouldBeGreaterThanOrEqualTo(
            unit.FrontImage.TopMm + MarginMm - Tolerance);
    }

    [Fact]
    public void NoMarginIsLeftBelowTheFeet()
    {
        // The margin applies to the sides and the top only: the drawing has to
        // reach the fold or the tab.
        UnfoldedUnit unit = Unit();

        Pair(Tall).Front.BottomMm.ShouldBe(unit.FrontImage.BottomMm, Tolerance);
    }

    [Fact]
    public void ATallPairFillsTheAvailableHeightExactly()
    {
        Pair(Tall).Front.HeightMm.ShouldBe(BoxHeightMm, Tolerance);
    }

    [Fact]
    public void ASourceSmallerThanItsBoxIsScaledUp()
    {
        // Not scaling up would print a pawn shorter than its size demands.
        Pair(new SourceImageSize(10, 100)).Front.HeightMm.ShouldBe(BoxHeightMm, Tolerance);
    }

    // --- DEC-042 : le facteur limitant est rapporté ------------------------

    [Fact]
    public void ATallPairIsNotReportedAsWidthLimited()
    {
        ImagePair pair = Pair(Tall);

        pair.IsWidthLimited.ShouldBeFalse();
        pair.HeightUsage.ShouldBe(1.0, Tolerance);
    }

    [Fact]
    public void AWidePairIsReportedAsWidthLimited()
    {
        // The case the framing clause is meant to make rare, and that imported
        // artwork can always bring back.
        ImagePair pair = Pair(Wide);

        pair.IsWidthLimited.ShouldBeTrue();
        pair.HeightUsage.ShouldBeLessThan(0.1);
    }

    [Fact]
    public void OneWideFaceIsEnoughToReportThePair()
    {
        // Both faces are scaled by the wide one, so both print short. Reporting
        // only when *both* are wide would hide exactly that case.
        ImagePair pair = Pair(Tall, Wide);

        pair.IsWidthLimited.ShouldBeTrue();
    }

    [Fact]
    public void HeightUsageSaysHowMuchOfTheHeightIsActuallyUsed()
    {
        // 200 px wide for 400 tall in a 26 × 98 box: width decides, scale is
        // 26/200 = 0.13, so the drawing is 52 mm out of 98 available.
        ImagePair pair = Pair(new SourceImageSize(200, 400));

        pair.Front.HeightMm.ShouldBe(52.0, Tolerance);
        pair.HeightUsage.ShouldBe(52.0 / BoxHeightMm, Tolerance);
    }

    // --- Garde-fous -------------------------------------------------------

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 0)]
    [InlineData(-1, 100)]
    public void ANonPositiveSourceSizeIsRejected(double widthPx, double heightPx)
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => Pair(new SourceImageSize(widthPx, heightPx)));
    }

    [Fact]
    public void ASilhouetteMarginLeavingNoRoomIsRejected()
    {
        LayoutSettings impossible = Layout with { SilhouetteMarginMm = WidthMm };

        Should.Throw<ArgumentOutOfRangeException>(
            () => ImagePlacement.ForPair(Unit(), Tall, Tall, impossible));
    }
}
