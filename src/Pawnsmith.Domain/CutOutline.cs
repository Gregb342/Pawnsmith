namespace Pawnsmith.Domain;

/// <summary>
/// The closed polygon a unit is cut along (B.4.3), in millimetres relative to
/// the unit.
/// </summary>
/// <remarks>
/// <para>
/// The outline is symmetrical about the unit's vertical axis and about the
/// fold line. It is computed once, here, and handed to the renderer: <b>the
/// renderer never recomputes it</b>, it receives it and traces it.
/// </para>
/// <para>
/// The polygon is <i>implicitly</i> closed: the last vertex joins the first,
/// and the first vertex is not repeated at the end. Anything drawing it must
/// close the path itself.
/// </para>
/// </remarks>
public static class CutOutline
{
    /// <summary>
    /// Builds the outline for one geometry.
    /// </summary>
    /// <param name="widthMm">Width of the unit.</param>
    /// <param name="totalHeightMm">Unfolded height of the unit.</param>
    /// <param name="geometry">Geometry of the sheet.</param>
    /// <param name="geometrySettings">Appendix dimensions, read from the calibration file.</param>
    public static IReadOnlyList<PointMm> Create(
        double widthMm,
        double totalHeightMm,
        Geometry geometry,
        GeometrySettings geometrySettings)
    {
        ArgumentNullException.ThrowIfNull(geometrySettings);

        return geometry switch
        {
            // Both give a plain rectangle, for opposite reasons: the folded
            // tent's appendix spans the full width, and NoSupport has none.
            Geometry.FoldedTent or Geometry.NoSupport => Rectangle(widthMm, totalHeightMm),
            Geometry.TabAndSocket => TabbedRectangle(
                widthMm,
                totalHeightMm,
                geometrySettings.TabAndSocket),
            _ => throw new ArgumentOutOfRangeException(nameof(geometry), geometry, null),
        };
    }

    /// <summary>
    /// <c>FoldedTent</c>: the appendix spans the full pawn width, so the
    /// outline is a plain rectangle. Four vertices, clockwise from the
    /// top-left corner.
    /// </summary>
    private static IReadOnlyList<PointMm> Rectangle(double widthMm, double totalHeightMm)
    {
        return
        [
            new PointMm(0, 0),
            new PointMm(widthMm, 0),
            new PointMm(widthMm, totalHeightMm),
            new PointMm(0, totalHeightMm),
        ];
    }

    /// <summary>
    /// <c>TabAndSocket</c>: a rectangle spanning the two image bands, extended
    /// above and below by a centred tab. Twelve vertices, clockwise from the
    /// top-left corner of the upper tab.
    /// </summary>
    /// <remarks>
    /// The tab height equals the appendix height, so the rectangular body runs
    /// from <c>tabHeight</c> to <c>totalHeight - tabHeight</c> — exactly the
    /// two image bands.
    /// </remarks>
    private static IReadOnlyList<PointMm> TabbedRectangle(
        double widthMm,
        double totalHeightMm,
        TabAndSocketSettings tab)
    {
        if (tab.TabWidthMm > widthMm)
        {
            // A tab wider than the pawn would turn the outline inside out. This
            // is not in the B.3 validation list, but it can only come from a
            // hand-edited calibration file, and failing here is cheaper than
            // discovering it on paper.
            throw new ArgumentOutOfRangeException(
                nameof(tab),
                tab.TabWidthMm,
                $"Tab width ({tab.TabWidthMm} mm) cannot exceed pawn width ({widthMm} mm).");
        }

        double tabLeftMm = (widthMm - tab.TabWidthMm) / 2;
        double tabRightMm = tabLeftMm + tab.TabWidthMm;
        double bodyTopMm = tab.TabHeightMm;
        double bodyBottomMm = totalHeightMm - tab.TabHeightMm;

        return
        [
            // Upper tab, left to right.
            new PointMm(tabLeftMm, 0),
            new PointMm(tabRightMm, 0),
            new PointMm(tabRightMm, bodyTopMm),

            // Right edge of the body, top to bottom.
            new PointMm(widthMm, bodyTopMm),
            new PointMm(widthMm, bodyBottomMm),

            // Lower tab, right to left.
            new PointMm(tabRightMm, bodyBottomMm),
            new PointMm(tabRightMm, totalHeightMm),
            new PointMm(tabLeftMm, totalHeightMm),
            new PointMm(tabLeftMm, bodyBottomMm),

            // Left edge of the body, bottom to top.
            new PointMm(0, bodyBottomMm),
            new PointMm(0, bodyTopMm),
            new PointMm(tabLeftMm, bodyTopMm),
        ];
    }
}
