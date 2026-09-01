namespace Pawnsmith.Domain;

/// <summary>
/// Thrown when a cell does not fit on a page at all, so the page would hold
/// nothing (B.5.2).
/// </summary>
/// <remarks>
/// <para>
/// <b>A zero capacity is a normal case, not an improbable anomaly.</b> It is
/// enough for one pawn height to exceed the ceiling of §B.5.6 — the
/// calibration shipped in v1.1 did exactly that, with a Gargantuan height of
/// 125 mm producing a 270 mm cell against 263 mm of usable A4.
/// </para>
/// <para>
/// The engine bounds nothing of its own accord: it computes the capacity and
/// reports zero. This exception is that report, and it names all three
/// ingredients, because knowing that "something does not fit" is useless
/// without knowing which size, on which paper, in which geometry.
/// </para>
/// </remarks>
public sealed class PageCapacityException : Exception
{
    public PageCapacityException(
        Size size,
        Geometry geometry,
        string paperFormatName,
        double cellWidthMm,
        double cellHeightMm,
        double usableWidthMm,
        double usableHeightMm)
        : base(
            $"Size {size} in geometry {geometry} needs a cell of " +
            $"{cellWidthMm} × {cellHeightMm} mm, which does not fit in the " +
            $"{usableWidthMm} × {usableHeightMm} mm usable area of paper format " +
            $"{paperFormatName}. Page capacity would be zero.")
    {
        Size = size;
        Geometry = geometry;
        PaperFormatName = paperFormatName;
        CellWidthMm = cellWidthMm;
        CellHeightMm = cellHeightMm;
        UsableWidthMm = usableWidthMm;
        UsableHeightMm = usableHeightMm;
    }

    public Size Size { get; }

    public Geometry Geometry { get; }

    public string PaperFormatName { get; }

    public double CellWidthMm { get; }

    public double CellHeightMm { get; }

    public double UsableWidthMm { get; }

    public double UsableHeightMm { get; }
}
