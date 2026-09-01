namespace Pawnsmith.Domain;

/// <summary>
/// The uniform grid of one page (B.5.2), in millimetres from the page's
/// top-left corner.
/// </summary>
/// <remarks>
/// <para>
/// A page holds one size only (DEC-005), so its cells are all identical and no
/// packing is involved. The grid is <b>centred horizontally</b> in the usable
/// width and <b>aligned to the top</b> of the usable height — the slack ends up
/// at the bottom, next to the calibration zone.
/// </para>
/// <para>
/// Cells are filled left to right, then top to bottom.
/// </para>
/// </remarks>
/// <param name="Columns">Number of cells per row.</param>
/// <param name="Rows">Number of rows.</param>
/// <param name="CellWidthMm">Width of one cell, which is the pawn width.</param>
/// <param name="CellHeightMm">Height of one cell, which is the unfolded height.</param>
/// <param name="GutterMm">Space between two neighbouring cut outlines.</param>
/// <param name="OriginXMm">Left edge of the first column, from the left of the page.</param>
/// <param name="OriginYMm">Top edge of the first row, from the top of the page.</param>
public sealed record PageGrid(
    int Columns,
    int Rows,
    double CellWidthMm,
    double CellHeightMm,
    double GutterMm,
    double OriginXMm,
    double OriginYMm)
{
    /// <summary>How many units one page of this grid holds.</summary>
    public int Capacity => Columns * Rows;

    /// <summary>
    /// Top-left corner of one cell, by its rank in filling order.
    /// </summary>
    /// <param name="index">Zero-based rank, filling left to right then top to bottom.</param>
    public PointMm CellOrigin(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Capacity);

        int row = index / Columns;
        int column = index % Columns;

        return new PointMm(
            OriginXMm + (column * (CellWidthMm + GutterMm)),
            OriginYMm + (row * (CellHeightMm + GutterMm)));
    }

    /// <summary>
    /// Computes the grid for one paper format and one unit.
    /// </summary>
    /// <exception cref="PageCapacityException">The cell does not fit at all.</exception>
    public static PageGrid Create(PaperFormat paper, UnfoldedUnit unit, LayoutSettings layout)
    {
        ArgumentNullException.ThrowIfNull(paper);
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(layout);

        // The margin is uniform on all four sides — an arbitration, not an
        // oversight (DEC-035). The calibration zone is taken off the bottom.
        double usableWidthMm = paper.WidthMm - (2 * layout.PageMarginMm);
        double usableHeightMm =
            paper.HeightMm - (2 * layout.PageMarginMm) - layout.CalibrationZoneHeightMm;

        int columns = FitCount(usableWidthMm, unit.WidthMm, layout.GutterMm);
        int rows = FitCount(usableHeightMm, unit.TotalHeightMm, layout.GutterMm);

        if (columns == 0 || rows == 0)
        {
            throw new PageCapacityException(
                unit.Size,
                unit.Geometry,
                paper.Name,
                unit.WidthMm,
                unit.TotalHeightMm,
                usableWidthMm,
                usableHeightMm);
        }

        double gridWidthMm = (columns * unit.WidthMm) + ((columns - 1) * layout.GutterMm);

        return new PageGrid(
            Columns: columns,
            Rows: rows,
            CellWidthMm: unit.WidthMm,
            CellHeightMm: unit.TotalHeightMm,
            GutterMm: layout.GutterMm,
            // Centred horizontally: the leftover width is split evenly.
            OriginXMm: layout.PageMarginMm + ((usableWidthMm - gridWidthMm) / 2),
            // Aligned to the top: the leftover height stays at the bottom.
            OriginYMm: layout.PageMarginMm);
    }

    /// <summary>
    /// How many cells of <paramref name="cellMm"/> fit in
    /// <paramref name="availableMm"/>, separated by a gutter.
    /// </summary>
    /// <remarks>
    /// The gutter is added to the numerator to account for the one that is
    /// <i>not</i> drawn after the last cell: n cells need
    /// <c>n × cell + (n − 1) × gutter</c>, which rearranges to
    /// <c>(available + gutter) / (cell + gutter)</c>. This is the formula given
    /// verbatim in B.5.2, written out here so the trick is not mistaken for one.
    /// </remarks>
    private static int FitCount(double availableMm, double cellMm, double gutterMm)
    {
        if (availableMm <= 0 || cellMm <= 0)
        {
            return 0;
        }

        return (int)Math.Floor((availableMm + gutterMm) / (cellMm + gutterMm));
    }
}
