namespace Pawnsmith.Domain.Tests;

/// <summary>
/// Covers tests 1, 2, 11 and 13 of B.8.
/// </summary>
public class PageGridTests
{
    private const double Tolerance = 1e-9;

    /// <summary>The Gargantuan height that shipped in calibration v1.1.</summary>
    private const double ImpossibleHeightMm = 125.0;

    // --- B.8 n° 1 : capacité pour chaque taille × format × géométrie ------

    [Theory]
    [InlineData(Size.Small, Geometry.TabAndSocket, "A4", 6, 2)]
    [InlineData(Size.Medium, Geometry.TabAndSocket, "A4", 6, 2)]
    [InlineData(Size.Large, Geometry.TabAndSocket, "A4", 3, 1)]
    [InlineData(Size.Huge, Geometry.TabAndSocket, "A4", 2, 1)]
    [InlineData(Size.Gargantuan, Geometry.TabAndSocket, "A4", 1, 1)]
    [InlineData(Size.Small, Geometry.FoldedTent, "A4", 6, 2)]
    [InlineData(Size.Medium, Geometry.FoldedTent, "A4", 6, 2)]
    [InlineData(Size.Large, Geometry.FoldedTent, "A4", 3, 1)]
    [InlineData(Size.Huge, Geometry.FoldedTent, "A4", 2, 1)]
    [InlineData(Size.Gargantuan, Geometry.FoldedTent, "A4", 1, 1)]
    [InlineData(Size.Small, Geometry.TabAndSocket, "Letter", 7, 2)]
    [InlineData(Size.Medium, Geometry.TabAndSocket, "Letter", 7, 2)]
    [InlineData(Size.Large, Geometry.TabAndSocket, "Letter", 3, 1)]
    [InlineData(Size.Huge, Geometry.TabAndSocket, "Letter", 2, 1)]
    [InlineData(Size.Gargantuan, Geometry.TabAndSocket, "Letter", 1, 1)]
    [InlineData(Size.Small, Geometry.FoldedTent, "Letter", 7, 2)]
    [InlineData(Size.Medium, Geometry.FoldedTent, "Letter", 7, 2)]
    [InlineData(Size.Large, Geometry.FoldedTent, "Letter", 3, 1)]
    [InlineData(Size.Huge, Geometry.FoldedTent, "Letter", 2, 1)]
    [InlineData(Size.Gargantuan, Geometry.FoldedTent, "Letter", 1, 1)]
    public void CapacityIsComputedForEveryCombination(
        Size size,
        Geometry geometry,
        string paperName,
        int expectedColumns,
        int expectedRows)
    {
        // US Letter is 6 mm wider than A4, which is exactly enough to gain a
        // seventh column on the two smallest sizes. It is also 18 mm shorter,
        // which costs nothing here — a reminder that the two formats are not
        // interchangeable (DEC-016).
        PaperFormat paper = Paper(paperName);

        var grid = PageGrid.Create(
            paper,
            CalibrationFixture.Unit(size, geometry),
            CalibrationFixture.Layout);

        grid.Columns.ShouldBe(expectedColumns);
        grid.Rows.ShouldBe(expectedRows);
        grid.Capacity.ShouldBe(expectedColumns * expectedRows);
    }

    // --- B.8 n° 13 : non-régression du plafond (DEC-032) ------------------

    [Theory]
    [InlineData("A4")]
    [InlineData("Letter")]
    public void TheHeightThatShippedInCalibrationV11ProducesZeroCapacity(string paperName)
    {
        // The real defect, not a synthetic case: a 125 mm pawn with a 10 mm tab
        // gives a 270 mm cell, against 263 mm of usable A4 and 245 mm of usable
        // US Letter. Zero capacity on both, on the largest size of the catalogue.
        Should.Throw<PageCapacityException>(
            () => PageGrid.Create(
                Paper(paperName),
                ImpossibleUnit(),
                CalibrationFixture.Layout));
    }

    // --- B.8 n° 2 : la capacité nulle est signalée explicitement ----------

    [Fact]
    public void ZeroCapacityNamesTheSizeTheGeometryAndThePaper()
    {
        PageCapacityException error = Should.Throw<PageCapacityException>(
            () => PageGrid.Create(
                CalibrationFixture.A4,
                ImpossibleUnit(),
                CalibrationFixture.Layout));

        error.Size.ShouldBe(Size.Gargantuan);
        error.Geometry.ShouldBe(Geometry.TabAndSocket);
        error.PaperFormatName.ShouldBe("A4");

        // Knowing that "something does not fit" is useless without the three.
        error.Message.ShouldContain("Gargantuan");
        error.Message.ShouldContain("TabAndSocket");
        error.Message.ShouldContain("A4");
    }

    [Fact]
    public void APawnWiderThanThePageAlsoProducesZeroCapacity()
    {
        // Height is the usual culprit, but width has to be caught too.
        var tooWide = UnfoldedUnit.Create(
            Size.Gargantuan,
            new PawnDimensions(101.6, PawnWidthMm: 500.0, PawnHeightMm: 40.0),
            Geometry.TabAndSocket,
            CalibrationFixture.Geometry);

        Should.Throw<PageCapacityException>(
            () => PageGrid.Create(CalibrationFixture.A4, tooWide, CalibrationFixture.Layout));
    }

    // --- B.8 n° 11 : grille centrée horizontalement -----------------------

    [Theory]
    [InlineData(Size.Small)]
    [InlineData(Size.Medium)]
    [InlineData(Size.Large)]
    [InlineData(Size.Huge)]
    [InlineData(Size.Gargantuan)]
    public void GridIsCentredHorizontally(Size size)
    {
        var grid = PageGrid.Create(
            CalibrationFixture.A4,
            CalibrationFixture.Unit(size, Geometry.TabAndSocket),
            CalibrationFixture.Layout);

        double gridWidthMm =
            (grid.Columns * grid.CellWidthMm) + ((grid.Columns - 1) * grid.GutterMm);
        double leftSlackMm = grid.OriginXMm;
        double rightSlackMm = CalibrationFixture.A4.WidthMm - grid.OriginXMm - gridWidthMm;

        leftSlackMm.ShouldBe(rightSlackMm, Tolerance);
    }

    [Fact]
    public void GridIsAlignedToTheTopOfTheUsableArea()
    {
        // The vertical slack stays at the bottom, next to the calibration zone.
        var grid = PageGrid.Create(
            CalibrationFixture.A4,
            CalibrationFixture.Unit(Size.Medium, Geometry.TabAndSocket),
            CalibrationFixture.Layout);

        grid.OriginYMm.ShouldBe(CalibrationFixture.Layout.PageMarginMm, Tolerance);
    }

    // --- Ordre de remplissage et bornes -----------------------------------

    [Fact]
    public void CellsAreFilledLeftToRightThenTopToBottom()
    {
        var grid = PageGrid.Create(
            CalibrationFixture.A4,
            CalibrationFixture.Unit(Size.Medium, Geometry.TabAndSocket),
            CalibrationFixture.Layout);

        PointMm first = grid.CellOrigin(0);
        PointMm second = grid.CellOrigin(1);
        PointMm firstOfNextRow = grid.CellOrigin(grid.Columns);

        second.YMm.ShouldBe(first.YMm, Tolerance);
        second.XMm.ShouldBe(first.XMm + grid.CellWidthMm + grid.GutterMm, Tolerance);

        firstOfNextRow.XMm.ShouldBe(first.XMm, Tolerance);
        firstOfNextRow.YMm.ShouldBe(first.YMm + grid.CellHeightMm + grid.GutterMm, Tolerance);
    }

    [Fact]
    public void TheGutterSeparatesOutlinesAndIsNotAddedAfterTheLastCell()
    {
        // B.5.3: the gutter is the space between two neighbouring outlines.
        // The formula adds one to the numerator to compensate for the one that
        // is never drawn, which is why the grid can be wider than a naive
        // count suggests.
        var grid = PageGrid.Create(
            CalibrationFixture.A4,
            CalibrationFixture.Unit(Size.Medium, Geometry.TabAndSocket),
            CalibrationFixture.Layout);

        PointMm last = grid.CellOrigin(grid.Columns - 1);
        double gridWidthMm = last.XMm + grid.CellWidthMm - grid.OriginXMm;

        gridWidthMm.ShouldBe(
            (grid.Columns * grid.CellWidthMm) + ((grid.Columns - 1) * grid.GutterMm),
            Tolerance);
    }

    [Fact]
    public void TheGridStaysInsideTheUsableArea()
    {
        var grid = PageGrid.Create(
            CalibrationFixture.A4,
            CalibrationFixture.Unit(Size.Medium, Geometry.TabAndSocket),
            CalibrationFixture.Layout);

        PointMm last = grid.CellOrigin(grid.Capacity - 1);
        double usableBottomMm = CalibrationFixture.A4.HeightMm
            - CalibrationFixture.Layout.PageMarginMm
            - CalibrationFixture.Layout.CalibrationZoneHeightMm;
        double usableRightMm =
            CalibrationFixture.A4.WidthMm - CalibrationFixture.Layout.PageMarginMm;

        (last.YMm + grid.CellHeightMm).ShouldBeLessThanOrEqualTo(usableBottomMm + Tolerance);
        (last.XMm + grid.CellWidthMm).ShouldBeLessThanOrEqualTo(usableRightMm + Tolerance);
    }

    [Fact]
    public void ACellIndexOutsideTheGridIsRejected()
    {
        var grid = PageGrid.Create(
            CalibrationFixture.A4,
            CalibrationFixture.Unit(Size.Medium, Geometry.TabAndSocket),
            CalibrationFixture.Layout);

        Should.Throw<ArgumentOutOfRangeException>(() => grid.CellOrigin(grid.Capacity));
        Should.Throw<ArgumentOutOfRangeException>(() => grid.CellOrigin(-1));
    }

    private static PaperFormat Paper(string name)
    {
        return name == "A4" ? CalibrationFixture.A4 : CalibrationFixture.Letter;
    }

    private static UnfoldedUnit ImpossibleUnit()
    {
        return UnfoldedUnit.Create(
            Size.Gargantuan,
            new PawnDimensions(101.6, 101.6, ImpossibleHeightMm),
            Geometry.TabAndSocket,
            CalibrationFixture.Geometry);
    }
}
