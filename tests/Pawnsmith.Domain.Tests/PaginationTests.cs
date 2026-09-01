namespace Pawnsmith.Domain.Tests;

/// <summary>
/// Covers tests 3, 4, 5 and 14 of B.8.
/// </summary>
public class PaginationTests
{
    private static SheetItem Item(string name, Size size, int quantity)
    {
        return new SheetItem(name, size, quantity, $"{name}-front.png", $"{name}-back.png");
    }

    private static SheetRequest Request(params SheetItem[] items)
    {
        return new SheetRequest(Geometry.TabAndSocket, CalibrationFixture.A4, items);
    }

    private static IReadOnlyList<PagePlan> Plan(SheetRequest request)
    {
        return Pagination.Plan(request, CalibrationFixture.Calibration);
    }

    // --- B.8 n° 3 : développement des quantités ---------------------------

    [Fact]
    public void AnItemOfQuantitySixProducesSixCells()
    {
        IReadOnlyList<PagePlan> pages = Plan(Request(Item("goblin", Size.Medium, 6)));

        pages.Sum(page => page.Cells.Count).ShouldBe(6);
        pages.SelectMany(page => page.Cells)
            .ShouldAllBe(cell => cell.Item.Name == "goblin");
    }

    [Fact]
    public void EachCopyKeepsItsOwnRank()
    {
        IReadOnlyList<PagePlan> pages = Plan(Request(Item("goblin", Size.Medium, 3)));

        pages.SelectMany(page => page.Cells)
            .Select(cell => cell.CopyIndex)
            .ShouldBe([0, 1, 2]);
    }

    [Fact]
    public void AQuantityBelowOneIsRejected()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => Plan(Request(Item("goblin", Size.Medium, 0))));
    }

    // --- B.8 n° 4 : pagination -------------------------------------------

    [Fact]
    public void AGroupExceedingCapacitySpillsOntoTheNextPage()
    {
        // Medium holds 12 per A4 page, so 13 copies need two pages and the
        // second one holds a single pawn.
        IReadOnlyList<PagePlan> pages = Plan(Request(Item("goblin", Size.Medium, 13)));

        pages.Count.ShouldBe(2);
        pages[0].Cells.Count.ShouldBe(12);
        pages[1].Cells.Count.ShouldBe(1);
    }

    [Fact]
    public void AGroupFillingExactlyOnePageDoesNotOpenASecond()
    {
        IReadOnlyList<PagePlan> pages = Plan(Request(Item("goblin", Size.Medium, 12)));

        pages.Count.ShouldBe(1);
        pages[0].Cells.Count.ShouldBe(12);
    }

    [Fact]
    public void AnItemSpanningAPageBreakIsNotSplitIntoTwoItems()
    {
        // A quantity running past the end of a page continues onto the next,
        // which is the natural behaviour B.5.2 describes.
        IReadOnlyList<PagePlan> pages = Plan(Request(Item("goblin", Size.Medium, 20)));

        pages.SelectMany(page => page.Cells).Count().ShouldBe(20);
        pages.ShouldAllBe(page => page.Cells.All(cell => cell.Item.Name == "goblin"));
    }

    // --- B.8 n° 5 : deux tailles ne partagent jamais une page -------------

    [Fact]
    public void TwoSizesNeverShareAPage()
    {
        IReadOnlyList<PagePlan> pages = Plan(Request(
            Item("goblin", Size.Medium, 2),
            Item("ogre", Size.Large, 1)));

        pages.Count.ShouldBe(2);
        pages.ShouldAllBe(page => page.Cells.All(cell => cell.Item.Size == page.Size));
    }

    [Fact]
    public void SizesComeOutInTheOrderTheyFirstAppearInTheRequest()
    {
        // B.5.1 asks for the manifest's order, not the enumeration's.
        IReadOnlyList<PagePlan> pages = Plan(Request(
            Item("dragon", Size.Gargantuan, 1),
            Item("goblin", Size.Small, 1)));

        pages.Select(page => page.Size).ShouldBe([Size.Gargantuan, Size.Small]);
    }

    [Fact]
    public void TwoItemsOfTheSameSizeShareTheirGroupEvenWhenListedApart()
    {
        IReadOnlyList<PagePlan> pages = Plan(Request(
            Item("goblin", Size.Medium, 1),
            Item("ogre", Size.Large, 1),
            Item("kobold", Size.Medium, 1)));

        pages.Count.ShouldBe(2);
        pages[0].Size.ShouldBe(Size.Medium);
        pages[0].Cells.Count.ShouldBe(2);
        pages[1].Size.ShouldBe(Size.Large);
    }

    // --- B.8 n° 14 : Small et Medium ne partagent jamais une page ---------

    [Fact]
    public void SmallAndMediumDoNotSharePagesDespiteTheSameFootprint()
    {
        // Same grid footprint of 25.4 mm, different pawn heights, therefore
        // different cell heights and different grids (DEC-031).
        IReadOnlyList<PagePlan> pages = Plan(Request(
            Item("gnome", Size.Small, 1),
            Item("human", Size.Medium, 1)));

        pages.Count.ShouldBe(2);
        pages[0].Size.ShouldBe(Size.Small);
        pages[1].Size.ShouldBe(Size.Medium);
    }

    [Fact]
    public void ASingleSmallPawnStillCostsAWholePage()
    {
        // The consequence DEC-031 accepts out loud: one gnome costs one page.
        IReadOnlyList<PagePlan> pages = Plan(Request(
            Item("gnome", Size.Small, 1),
            Item("human", Size.Medium, 12)));

        pages.Count.ShouldBe(2);
        pages.Single(page => page.Size == Size.Small).Cells.Count.ShouldBe(1);
    }

    // --- Capacité nulle propagée -----------------------------------------

    [Fact]
    public void ASizeThatDoesNotFitStopsThePlanning()
    {
        SheetRequest request = new(
            Geometry.TabAndSocket,
            CalibrationFixture.A4,
            [Item("colossus", Size.Gargantuan, 1)]);

        Should.Throw<PageCapacityException>(
            () => Pagination.Plan(request, CalibrationFixture.CalibrationWithImpossibleGargantuan));
    }

    [Fact]
    public void ASizeAbsentFromTheCalibrationIsReported()
    {
        Should.Throw<ArgumentOutOfRangeException>(
            () => Pagination.Plan(
                Request(Item("gnome", Size.Small, 1)),
                CalibrationFixture.CalibrationWithoutSmall));
    }
}
