using Pawnsmith.Domain.Generation;

namespace Pawnsmith.Domain.Tests.Generation;

/// <summary>
/// Covers tests 7 to 9 of E.12: the split rule of DEC-079.
/// </summary>
public class PairSplitTests
{
    // --- E.12 n° 7 : largeur paire --------------------------------------------

    [Fact]
    public void AnEvenWidthGivesTwoEqualHalvesWithTheFrontOnTheLeft()
    {
        // The dimensions of DEC-043: 1216 × 832, so 608 × 832 per view.
        PairHalves halves = PairSplit.Of(1216, 832);

        halves.Front.ShouldBe(new PixelRect(X: 0, Y: 0, Width: 608, Height: 832));
        halves.Back.ShouldBe(new PixelRect(X: 608, Y: 0, Width: 608, Height: 832));
    }

    [Fact]
    public void TheTwoHalvesCoverTheWholeImageWithoutOverlap()
    {
        PairHalves halves = PairSplit.Of(10, 4);

        (halves.Front.X + halves.Front.Width).ShouldBe(halves.Back.X);
        (halves.Back.X + halves.Back.Width).ShouldBe(10);
    }

    // --- E.12 n° 8 : largeur impaire, la colonne du milieu est perdue -------

    [Fact]
    public void AnOddWidthLosesItsMiddleColumnAndKeepsBothHalvesEqual()
    {
        PairHalves halves = PairSplit.Of(1217, 832);

        halves.Front.ShouldBe(new PixelRect(X: 0, Y: 0, Width: 608, Height: 832));
        halves.Back.ShouldBe(new PixelRect(X: 609, Y: 0, Width: 608, Height: 832));

        // Column 608 belongs to neither half.
        (halves.Front.X + halves.Front.Width).ShouldBe(608);
        halves.Back.X.ShouldBe(609);
    }

    [Fact]
    public void TheSmallestSplittableImageGivesOnePixelToEachView()
    {
        PairHalves halves = PairSplit.Of(3, 1);

        halves.Front.ShouldBe(new PixelRect(0, 0, 1, 1));
        halves.Back.ShouldBe(new PixelRect(2, 0, 1, 1));
    }

    // --- E.12 n° 9 : moins de deux pixels de large ---------------------------

    [Theory]
    [InlineData(1, 832)]
    [InlineData(0, 832)]
    [InlineData(1216, 0)]
    public void AnImageTooSmallToCutIsRefused(int widthPx, int heightPx)
    {
        PairSplit.IsSplittable(widthPx, heightPx).ShouldBeFalse();
        Should.Throw<ArgumentOutOfRangeException>(() => PairSplit.Of(widthPx, heightPx));
    }

    [Fact]
    public void TwoPixelsAreEnough()
    {
        PairSplit.IsSplittable(2, 1).ShouldBeTrue();
    }
}
