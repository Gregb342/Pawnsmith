namespace Pawnsmith.Domain;

/// <summary>
/// One page's worth of cells, before any position is computed (B.5.1).
/// </summary>
/// <param name="Size">Size of every pawn on this page. A page never mixes sizes.</param>
/// <param name="Cells">The pawns, in filling order. One entry per printed copy.</param>
public sealed record PagePlan(Size Size, IReadOnlyList<PlannedCell> Cells);

/// <summary>
/// One printed copy of one item: what will occupy a single cell.
/// </summary>
/// <remarks>
/// An item of quantity 6 produces six of these, all identical apart from
/// <paramref name="CopyIndex"/>. They are separate entries rather than a count,
/// because each one gets its own position, its own outline and its own pair of
/// images once the page is resolved.
/// </remarks>
/// <param name="Item">The item this copy comes from.</param>
/// <param name="CopyIndex">Zero-based rank of this copy within its item.</param>
public sealed record PlannedCell(SheetItem Item, int CopyIndex);

/// <summary>
/// Groups the requested items by size and splits them into pages (B.5.1).
/// </summary>
public static class Pagination
{
    /// <summary>
    /// Plans the pages of a request.
    /// </summary>
    /// <remarks>
    /// Four steps, in the order B.5.1 gives them: group by size, expand the
    /// quantities, fill the pages of one group, then move to the next group.
    /// Sizes come out in the order they first appear in the request.
    /// </remarks>
    /// <exception cref="PageCapacityException">A size does not fit on the page at all.</exception>
    public static IReadOnlyList<PagePlan> Plan(SheetRequest request, Calibration calibration)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(calibration);

        List<PagePlan> pages = [];

        foreach (Size size in SizesInRequestOrder(request))
        {
            int capacity = CapacityFor(size, request, calibration);
            IReadOnlyList<PlannedCell> cells = ExpandQuantities(request, size);

            // Fill this group's pages, then move on. A page is never shared
            // with the next group, even when both sizes have the same
            // footprint: Small and Medium have different cell heights, hence
            // different grids (DEC-005, DEC-031).
            for (int start = 0; start < cells.Count; start += capacity)
            {
                pages.Add(new PagePlan(
                    size,
                    [.. cells.Skip(start).Take(capacity)]));
            }
        }

        return pages;
    }

    /// <summary>
    /// The distinct sizes of the request, in the order they first appear.
    /// </summary>
    /// <remarks>
    /// B.5.1 asks for the manifest's own order, not the declaration order of
    /// the enumeration. Two items of the same size, listed apart, still belong
    /// to the same group.
    /// </remarks>
    private static IReadOnlyList<Size> SizesInRequestOrder(SheetRequest request)
    {
        return [.. request.Items.Select(item => item.Size).Distinct()];
    }

    /// <summary>
    /// Expands the quantities of every item of one size, in request order.
    /// </summary>
    private static IReadOnlyList<PlannedCell> ExpandQuantities(SheetRequest request, Size size)
    {
        List<PlannedCell> cells = [];

        foreach (SheetItem item in request.Items.Where(item => item.Size == size))
        {
            if (item.Quantity < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(request),
                    item.Quantity,
                    $"Item '{item.Name}' has a quantity of {item.Quantity}; it must be at least 1.");
            }

            for (int copy = 0; copy < item.Quantity; copy++)
            {
                cells.Add(new PlannedCell(item, copy));
            }
        }

        return cells;
    }

    private static int CapacityFor(Size size, SheetRequest request, Calibration calibration)
    {
        if (!calibration.Sizes.TryGetValue(size, out PawnDimensions? pawn))
        {
            throw new ArgumentOutOfRangeException(
                nameof(calibration),
                size,
                $"Size {size} is referenced by the request but absent from the calibration file.");
        }

        var unit = UnfoldedUnit.Create(size, pawn, request.Geometry, calibration.Geometry);

        // Throws PageCapacityException when the cell does not fit at all, which
        // is what B.5.2 asks for: an explicit error rather than an empty page.
        return PageGrid.Create(request.PaperFormat, unit, calibration.Layout).Capacity;
    }
}
