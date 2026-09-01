namespace Pawnsmith.Domain;

/// <summary>
/// A fully resolved sheet: everything the renderer needs, and nothing it has to
/// work out for itself (B.6).
/// </summary>
/// <remarks>
/// Every coordinate is in millimetres, from the top-left corner of the page,
/// and already carries the printer correction. <b>The renderer decides
/// nothing</b>: it converts millimetres to points once and traces.
/// </remarks>
/// <param name="Pages">The pages, in order, across all size groups.</param>
/// <param name="Strokes">Stroke widths and colour of the printing marks.</param>
/// <param name="WidthLimitedItems">Items that print shorter than their size allows (DEC-042).</param>
public sealed record SheetLayout(
    IReadOnlyList<SheetPage> Pages,
    StrokeSettings Strokes,
    IReadOnlyList<WidthLimitedItem> WidthLimitedItems);

/// <summary>
/// An item whose artwork is held back by its width rather than its height
/// (DEC-042).
/// </summary>
/// <remarks>
/// <para>
/// Such a pawn prints shorter than its size demands, and <b>nothing on the
/// sheet reveals it</b> — it simply looks like a smaller creature. Measured on
/// real artwork, two pawns of the same declared size came out with a ratio of
/// 1.87 between them.
/// </para>
/// <para>
/// The framing clause is what makes the case rare, but it only governs what the
/// generator produces: artwork brought in by hand, and later the external import
/// of EVO-010, escape it. So the engine reports rather than hides, and the sheet
/// is still produced — a short pawn is usable, a missing one is not.
/// </para>
/// <para>
/// One entry per item, not per copy: six copies of the same goblin are one
/// problem, not six.
/// </para>
/// </remarks>
/// <param name="ItemName">Name of the item, as written in the manifest.</param>
/// <param name="Size">Size it was declared at.</param>
/// <param name="PrintedHeightMm">Height the artwork actually reaches.</param>
/// <param name="AvailableHeightMm">Height its size would have allowed.</param>
public sealed record WidthLimitedItem(
    string ItemName,
    Size Size,
    double PrintedHeightMm,
    double AvailableHeightMm)
{
    /// <summary>Share of the available height actually used, from 0 to 1.</summary>
    public double HeightUsage => PrintedHeightMm / AvailableHeightMm;
}

/// <summary>One page of the sheet.</summary>
/// <param name="Size">Size of every pawn on this page (DEC-005).</param>
/// <param name="Geometry">Geometry of every pawn on this page.</param>
/// <param name="PaperFormat">Paper this page is laid out for.</param>
/// <param name="PageNumber">One-based rank of this page in the whole document.</param>
/// <param name="PageCount">Total number of pages in the document.</param>
/// <param name="Units">The placed units, in filling order.</param>
/// <param name="CalibrationMark">The 100 mm reference mark of B.5.4.</param>
public sealed record SheetPage(
    Size Size,
    Geometry Geometry,
    PaperFormat PaperFormat,
    int PageNumber,
    int PageCount,
    IReadOnlyList<PlacedUnit> Units,
    CalibrationMark CalibrationMark);

/// <summary>
/// One unit at its place on the page, with everything already in page
/// coordinates.
/// </summary>
/// <param name="Item">The item this copy comes from, for diagnostics.</param>
/// <param name="CutOutlineMm">Closed polygon to cut along. Not explicitly closed (DEC-038).</param>
/// <param name="FoldLines">Fold lines, each a pair of end points.</param>
/// <param name="FrontImage">Where the front artwork goes.</param>
/// <param name="BackImage">Where the back artwork goes, turned by a half turn.</param>
public sealed record PlacedUnit(
    SheetItem Item,
    IReadOnlyList<PointMm> CutOutlineMm,
    IReadOnlyList<FoldLine> FoldLines,
    PlacedImage FrontImage,
    PlacedImage BackImage);

/// <summary>A fold line, as drawn: a horizontal segment across the unit.</summary>
/// <param name="StartMm">Left end.</param>
/// <param name="EndMm">Right end.</param>
public sealed record FoldLine(PointMm StartMm, PointMm EndMm);

/// <summary>An image at its place on the page.</summary>
/// <param name="FileName">Name of the source file, resolved by the caller.</param>
/// <param name="XMm">Left edge of the bounding box.</param>
/// <param name="YMm">Top edge of the bounding box.</param>
/// <param name="WidthMm">Drawn width.</param>
/// <param name="HeightMm">Drawn height.</param>
/// <param name="Rotation">Half turn for the back panel, none for the front.</param>
public sealed record PlacedImage(
    string FileName,
    double XMm,
    double YMm,
    double WidthMm,
    double HeightMm,
    ImageRotation Rotation);

/// <summary>
/// The 100 mm reference mark printed at the bottom of every page (B.5.4).
/// </summary>
/// <remarks>
/// It is the only protection against a print that came out at the wrong scale,
/// and DEC-017 makes it non-optional. Its drawn length is 100 mm multiplied by
/// the printer correction, precisely so that it measures 100 mm on paper once
/// the printer has done its own scaling.
/// </remarks>
/// <param name="StartMm">Left end of the horizontal segment.</param>
/// <param name="EndMm">Right end of the horizontal segment.</param>
/// <param name="TickHeightMm">Height of the vertical tick at each end.</param>
/// <param name="NominalLengthMm">The length it is meant to measure on paper: 100 mm.</param>
public sealed record CalibrationMark(
    PointMm StartMm,
    PointMm EndMm,
    double TickHeightMm,
    double NominalLengthMm)
{
    /// <summary>Length actually drawn in the PDF, correction included.</summary>
    public double DrawnLengthMm => EndMm.XMm - StartMm.XMm;
}
