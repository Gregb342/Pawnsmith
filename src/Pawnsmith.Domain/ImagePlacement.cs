namespace Pawnsmith.Domain;

/// <summary>
/// Where one image is drawn inside its band (B.4.4), in millimetres relative to
/// the unit.
/// </summary>
/// <remarks>
/// <para>
/// The rectangle is the image's <b>bounding box</b>. A half turn is performed
/// about the centre of that box, so a rotated image occupies exactly the same
/// rectangle as an unrotated one — the rotation changes what is drawn, never
/// where.
/// </para>
/// <para>
/// <b>The alignment rule is the point of this type</b>: the image is centred
/// horizontally and sits on its feet line. It is never centred vertically. A
/// short character stands on the ground with empty space above its head; it
/// does not float in the middle of its band.
/// </para>
/// </remarks>
/// <param name="XMm">Left edge of the bounding box, from the left edge of the unit.</param>
/// <param name="YMm">Top edge of the bounding box, from the top of the unit.</param>
/// <param name="WidthMm">Drawn width.</param>
/// <param name="HeightMm">Drawn height.</param>
/// <param name="Rotation">Half turn for the back panel, none for the front.</param>
public sealed record ImagePlacement(
    double XMm,
    double YMm,
    double WidthMm,
    double HeightMm,
    ImageRotation Rotation)
{
    /// <summary>Bottom edge of the bounding box, from the top of the unit.</summary>
    public double BottomMm => YMm + HeightMm;

    /// <summary>
    /// Places both images of one pawn, at a single shared scale (DEC-041).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The two faces are scaled together, never separately.</b> Two views of
    /// the same character rarely have exactly the same pixel dimensions — a
    /// raised weapon on one side is enough — so scaling each to fit its own box
    /// magnifies them differently. Measured on real artwork, that produced up to
    /// 4.5 mm of difference between the front and back of a single troll: once
    /// folded, the back panel sticks out past the front.
    /// </para>
    /// <para>
    /// The shared factor is the smaller of the two, the only one that lets both
    /// fit. The character then appears at the same magnification on both faces,
    /// which is what the eye and the scissors care about.
    /// </para>
    /// </remarks>
    /// <param name="unit">The unfolded unit the images go into.</param>
    /// <param name="front">Pixel dimensions of the front artwork.</param>
    /// <param name="back">Pixel dimensions of the back artwork.</param>
    /// <param name="layout">Layout values, read from the calibration file.</param>
    public static ImagePair ForPair(
        UnfoldedUnit unit,
        SourceImageSize front,
        SourceImageSize back,
        LayoutSettings layout)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(front);
        ArgumentNullException.ThrowIfNull(back);
        ArgumentNullException.ThrowIfNull(layout);

        (double boxWidthMm, double boxHeightMm) = Box(unit, layout);

        double scale = Math.Min(
            FitScale(boxWidthMm, boxHeightMm, front),
            FitScale(boxWidthMm, boxHeightMm, back));

        return new ImagePair(
            Front: Place(unit, front, scale, ImageRotation.None),
            Back: Place(unit, back, scale, ImageRotation.HalfTurn),
            BoxHeightMm: boxHeightMm,
            IsWidthLimited: IsWidthLimited(boxWidthMm, boxHeightMm, front, back));
    }

    /// <summary>
    /// Places one image at an already-decided scale.
    /// </summary>
    /// <remarks>
    /// The front sits with its feet on the bottom edge of its band, so its box
    /// grows upwards from there. The back sits with its feet on the <i>top</i>
    /// edge of its own band, so its box grows downwards. The two feet lines face
    /// each other across the fold, which is what makes the back land the right
    /// way up once folded — and omitting the half turn is what produces a
    /// character standing on its head.
    /// </remarks>
    private static ImagePlacement Place(
        UnfoldedUnit unit,
        SourceImageSize source,
        double scale,
        ImageRotation rotation)
    {
        double widthMm = source.WidthPx * scale;
        double heightMm = source.HeightPx * scale;

        double yMm = rotation == ImageRotation.None
            ? unit.FrontImage.BottomMm - heightMm
            : unit.BackImage.TopMm;

        return new ImagePlacement(
            XMm: (unit.WidthMm - widthMm) / 2,
            YMm: yMm,
            WidthMm: widthMm,
            HeightMm: heightMm,
            Rotation: rotation);
    }

    /// <summary>
    /// The box an image may occupy inside its band.
    /// </summary>
    /// <remarks>
    /// Narrower than the band by a silhouette margin on <i>each side</i>, and
    /// shorter by one margin at the <i>top only</i> (B.4.4). No margin below the
    /// feet: that edge is where the fold or the tab starts, and the drawing has
    /// to reach it.
    /// </remarks>
    private static (double WidthMm, double HeightMm) Box(UnfoldedUnit unit, LayoutSettings layout)
    {
        double widthMm = unit.WidthMm - (2 * layout.SilhouetteMarginMm);
        double heightMm = unit.FrontImage.HeightMm - layout.SilhouetteMarginMm;

        if (widthMm <= 0 || heightMm <= 0)
        {
            // DEC-038, convention 5: a silhouette margin wider than the pawn
            // leaves nothing to draw in. Fail here rather than emit an empty
            // outline and find out on paper.
            throw new ArgumentOutOfRangeException(
                nameof(layout),
                layout.SilhouetteMarginMm,
                $"Silhouette margin leaves no room in a {unit.WidthMm} x " +
                $"{unit.FrontImage.HeightMm} mm band.");
        }

        return (widthMm, heightMm);
    }

    /// <summary>
    /// The largest factor at which a source fits its box, aspect preserved.
    /// </summary>
    /// <remarks>
    /// The smaller of the two ratios is the one that makes the image fit in both
    /// directions; the other would overflow.
    /// <para>
    /// The image is scaled up as readily as down. A source smaller than its box
    /// would otherwise print a pawn shorter than its size demands, which would
    /// defeat the whole point of calibrated sizes.
    /// </para>
    /// </remarks>
    private static double FitScale(double boxWidthMm, double boxHeightMm, SourceImageSize source)
    {
        if (source.WidthPx <= 0 || source.HeightPx <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Source image dimensions must both be positive.");
        }

        return Math.Min(boxWidthMm / source.WidthPx, boxHeightMm / source.HeightPx);
    }

    /// <summary>
    /// Whether the pair is held back by its width rather than its height
    /// (DEC-042).
    /// </summary>
    /// <remarks>
    /// A width-limited pawn prints shorter than its size demands, and nothing on
    /// the sheet reveals it. The framing clause is meant to make the case rare,
    /// but it only governs what the generator produces: artwork brought in by
    /// hand, and later the external import of EVO-010, escape it entirely. So
    /// the engine reports the case instead of hiding it.
    /// </remarks>
    private static bool IsWidthLimited(
        double boxWidthMm,
        double boxHeightMm,
        SourceImageSize front,
        SourceImageSize back)
    {
        foreach (SourceImageSize source in new[] { front, back })
        {
            if (boxWidthMm / source.WidthPx < boxHeightMm / source.HeightPx)
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// The two faces of one pawn, scaled together (DEC-041).
/// </summary>
/// <param name="Front">Front artwork, upright.</param>
/// <param name="Back">Back artwork, turned by a half turn.</param>
/// <param name="BoxHeightMm">Height the artwork could have reached.</param>
/// <param name="IsWidthLimited">
/// True when the width, not the height, decided the scale — meaning the pawn
/// prints shorter than its size allows.
/// </param>
public sealed record ImagePair(
    ImagePlacement Front,
    ImagePlacement Back,
    double BoxHeightMm,
    bool IsWidthLimited)
{
    /// <summary>
    /// How much of the available height the artwork actually uses, from 0 to 1.
    /// </summary>
    /// <remarks>
    /// The figure a diagnostic message quotes: 0.42 means the pawn prints at
    /// 42% of the height its size calls for.
    /// </remarks>
    public double HeightUsage => Math.Max(Front.HeightMm, Back.HeightMm) / BoxHeightMm;
}
