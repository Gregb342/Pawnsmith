namespace Pawnsmith.Domain.Tests;

/// <summary>
/// Covers test 12 of B.8 — the printer correction applies to the calibration
/// mark as much as to everything else — plus the invariants of a resolved
/// layout.
/// </summary>
public class SheetLayoutBuilderTests
{
    private const double Tolerance = 1e-9;

    private static readonly SourceImageSize Portrait = new(WidthPx: 600, HeightPx: 1000);

    private static SheetItem Item(string name, Size size, int quantity)
    {
        return new SheetItem(name, size, quantity, $"{name}-front.png", $"{name}-back.png");
    }

    private static IReadOnlyDictionary<string, SourceImageSize> Images(params SheetItem[] items)
    {
        Dictionary<string, SourceImageSize> sizes = [];

        foreach (SheetItem item in items)
        {
            sizes[item.FrontImageFile] = Portrait;
            sizes[item.BackImageFile] = Portrait;
        }

        return sizes;
    }

    private static SheetLayout Build(Calibration calibration, params SheetItem[] items)
    {
        SheetRequest request = new(Geometry.TabAndSocket, CalibrationFixture.A4, items);

        return SheetLayoutBuilder.Build(request, calibration, Images(items));
    }

    // --- B.8 n° 12 : le facteur de correction s'applique à tout ------------

    [Fact]
    public void WithoutCorrectionTheMarkIsDrawnAtExactlyOneHundredMillimetres()
    {
        SheetLayout layout = Build(CalibrationFixture.Calibration, Item("goblin", Size.Medium, 1));

        layout.Pages[0].CalibrationMark.DrawnLengthMm.ShouldBe(100.0, Tolerance);
    }

    [Theory]
    [InlineData(1.02)]
    [InlineData(0.97)]
    public void TheCorrectionIsAppliedToTheCalibrationMark(double factor)
    {
        // If the printer shrinks by 2%, the PDF is enlarged by 2%, and the mark
        // drawn at 102 mm comes out at 100 mm on paper. Excluding the mark from
        // the correction would make the measurement meaningless.
        SheetLayout layout = Build(
            CalibrationFixture.CalibrationWithScale(factor),
            Item("goblin", Size.Medium, 1));

        layout.Pages[0].CalibrationMark.DrawnLengthMm.ShouldBe(100.0 * factor, Tolerance);
        layout.Pages[0].CalibrationMark.NominalLengthMm.ShouldBe(100.0, Tolerance);
    }

    [Theory]
    [InlineData(1.02)]
    [InlineData(0.97)]
    public void TheCorrectionIsAppliedToTheUnitsTheSameWay(double factor)
    {
        SheetLayout plain = Build(CalibrationFixture.Calibration, Item("goblin", Size.Medium, 1));
        SheetLayout scaled = Build(
            CalibrationFixture.CalibrationWithScale(factor),
            Item("goblin", Size.Medium, 1));

        PlacedUnit plainUnit = plain.Pages[0].Units[0];
        PlacedUnit scaledUnit = scaled.Pages[0].Units[0];

        scaledUnit.FrontImage.WidthMm.ShouldBe(plainUnit.FrontImage.WidthMm * factor, Tolerance);
        scaledUnit.FrontImage.HeightMm.ShouldBe(plainUnit.FrontImage.HeightMm * factor, Tolerance);

        double plainOutlineWidthMm = OutlineWidthMm(plainUnit);
        double scaledOutlineWidthMm = OutlineWidthMm(scaledUnit);
        scaledOutlineWidthMm.ShouldBe(plainOutlineWidthMm * factor, Tolerance);
    }

    [Fact]
    public void TheCorrectionIsAnchoredOnThePageCentre()
    {
        // A page rescaled about its centre keeps its content centred; anchoring
        // at a corner would push everything towards the opposite one.
        SheetLayout plain = Build(CalibrationFixture.Calibration, Item("goblin", Size.Medium, 1));
        SheetLayout scaled = Build(
            CalibrationFixture.CalibrationWithScale(1.5),
            Item("goblin", Size.Medium, 1));

        double centreXMm = CalibrationFixture.A4.WidthMm / 2;

        MarkCentreXMm(plain.Pages[0]).ShouldBe(centreXMm, Tolerance);
        MarkCentreXMm(scaled.Pages[0]).ShouldBe(centreXMm, Tolerance);
    }

    // --- Structure de la planche résolue ----------------------------------

    [Fact]
    public void EveryCellOfThePlanBecomesAPlacedUnit()
    {
        SheetLayout layout = Build(CalibrationFixture.Calibration, Item("goblin", Size.Medium, 13));

        layout.Pages.Count.ShouldBe(2);
        layout.Pages.Sum(page => page.Units.Count).ShouldBe(13);
    }

    [Fact]
    public void PagesAreNumberedFromOneAndKnowTheTotal()
    {
        SheetLayout layout = Build(
            CalibrationFixture.Calibration,
            Item("goblin", Size.Medium, 13),
            Item("ogre", Size.Large, 1));

        layout.Pages.Select(page => page.PageNumber).ShouldBe([1, 2, 3]);
        layout.Pages.ShouldAllBe(page => page.PageCount == 3);
    }

    [Fact]
    public void EveryPageCarriesItsOwnCalibrationMark()
    {
        // DEC-017: the marks are printed on every page and cannot be turned off.
        SheetLayout layout = Build(CalibrationFixture.Calibration, Item("goblin", Size.Medium, 13));

        layout.Pages.ShouldAllBe(page => page.CalibrationMark.DrawnLengthMm > 0);
    }

    [Fact]
    public void TheCalibrationMarkSitsBelowEveryUnit()
    {
        SheetLayout layout = Build(CalibrationFixture.Calibration, Item("goblin", Size.Medium, 12));

        SheetPage page = layout.Pages[0];
        double lowestUnitMm = page.Units.SelectMany(u => u.CutOutlineMm).Max(p => p.YMm);

        page.CalibrationMark.StartMm.YMm.ShouldBeGreaterThan(lowestUnitMm);
    }

    [Fact]
    public void TheBackImageIsTurnedAndTheFrontIsNot()
    {
        SheetLayout layout = Build(CalibrationFixture.Calibration, Item("goblin", Size.Medium, 1));

        PlacedUnit unit = layout.Pages[0].Units[0];

        unit.BackImage.Rotation.ShouldBe(ImageRotation.HalfTurn);
        unit.FrontImage.Rotation.ShouldBe(ImageRotation.None);
        unit.BackImage.FileName.ShouldBe("goblin-back.png");
        unit.FrontImage.FileName.ShouldBe("goblin-front.png");
    }

    [Fact]
    public void FoldLinesSpanTheWidthOfTheirUnit()
    {
        SheetLayout layout = Build(CalibrationFixture.Calibration, Item("goblin", Size.Medium, 1));

        PlacedUnit unit = layout.Pages[0].Units[0];

        unit.FoldLines.Count.ShouldBe(1);
        unit.FoldLines[0].StartMm.YMm.ShouldBe(unit.FoldLines[0].EndMm.YMm, Tolerance);
        (unit.FoldLines[0].EndMm.XMm - unit.FoldLines[0].StartMm.XMm)
            .ShouldBe(OutlineWidthMm(unit), Tolerance);
    }

    [Fact]
    public void AFoldedTentUnitCarriesThreeFoldLines()
    {
        SheetItem item = Item("goblin", Size.Medium, 1);
        SheetRequest request = new(Geometry.FoldedTent, CalibrationFixture.A4, [item]);

        SheetLayout layout = SheetLayoutBuilder.Build(
            request,
            CalibrationFixture.Calibration,
            Images(item));

        layout.Pages[0].Units[0].FoldLines.Count.ShouldBe(3);
    }

    [Fact]
    public void TwoUnitsOnTheSameRowAreSeparatedByTheGutter()
    {
        SheetLayout layout = Build(CalibrationFixture.Calibration, Item("goblin", Size.Medium, 2));

        IReadOnlyList<PlacedUnit> units = layout.Pages[0].Units;
        double firstRightMm = units[0].CutOutlineMm.Max(p => p.XMm);
        double secondLeftMm = units[1].CutOutlineMm.Min(p => p.XMm);

        (secondLeftMm - firstRightMm).ShouldBe(CalibrationFixture.Layout.GutterMm, Tolerance);
    }

    [Fact]
    public void AMissingImageSizeIsReportedByName()
    {
        SheetItem item = Item("goblin", Size.Medium, 1);
        SheetRequest request = new(Geometry.TabAndSocket, CalibrationFixture.A4, [item]);

        ArgumentOutOfRangeException error = Should.Throw<ArgumentOutOfRangeException>(
            () => SheetLayoutBuilder.Build(
                request,
                CalibrationFixture.Calibration,
                new Dictionary<string, SourceImageSize>()));

        error.Message.ShouldContain("goblin-front.png");
    }

    private static double OutlineWidthMm(PlacedUnit unit)
    {
        return unit.CutOutlineMm.Max(p => p.XMm) - unit.CutOutlineMm.Min(p => p.XMm);
    }

    private static double MarkCentreXMm(SheetPage page)
    {
        return (page.CalibrationMark.StartMm.XMm + page.CalibrationMark.EndMm.XMm) / 2;
    }
}
