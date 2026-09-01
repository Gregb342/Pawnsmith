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
    /// Places the front image: upright, feet on the boundary with the front
    /// appendix, which is the <i>bottom</i> of its band.
    /// </summary>
    public static ImagePlacement ForFront(
        UnfoldedUnit unit,
        SourceImageSize source,
        LayoutSettings layout)
    {
        ArgumentNullException.ThrowIfNull(unit);

        (double widthMm, double heightMm) = ScaleToFit(unit, unit.FrontImage, source, layout);

        // Feet sit on the band's bottom edge, so the box grows upwards from it.
        return new ImagePlacement(
            XMm: CentredXMm(unit.WidthMm, widthMm),
            YMm: unit.FrontImage.BottomMm - heightMm,
            WidthMm: widthMm,
            HeightMm: heightMm,
            Rotation: ImageRotation.None);
    }

    /// <summary>
    /// Places the back image: turned by 180°, feet on the boundary with the
    /// back appendix, which is the <i>top</i> of its band.
    /// </summary>
    /// <remarks>
    /// The two feet lines face each other across the fold, one at the bottom of
    /// the front band and one at the top of the back band. That is what makes
    /// the back panel land the right way up once folded — and omitting the half
    /// turn is what produces a character standing on its head.
    /// </remarks>
    public static ImagePlacement ForBack(
        UnfoldedUnit unit,
        SourceImageSize source,
        LayoutSettings layout)
    {
        ArgumentNullException.ThrowIfNull(unit);

        (double widthMm, double heightMm) = ScaleToFit(unit, unit.BackImage, source, layout);

        // Feet sit on the band's top edge, so the box grows downwards from it.
        return new ImagePlacement(
            XMm: CentredXMm(unit.WidthMm, widthMm),
            YMm: unit.BackImage.TopMm,
            WidthMm: widthMm,
            HeightMm: heightMm,
            Rotation: ImageRotation.HalfTurn);
    }

    /// <summary>
    /// Scales the source to the largest size fitting its box, preserving the
    /// aspect ratio.
    /// </summary>
    /// <remarks>
    /// The box is narrower than the band by a silhouette margin on <i>each
    /// side</i>, and shorter by one margin at the <i>top only</i> (B.4.4). No
    /// margin below the feet: that edge is where the fold or the tab starts,
    /// and the drawing has to reach it.
    /// <para>
    /// The image is scaled up as readily as down. A source smaller than its box
    /// would otherwise print a pawn shorter than its size demands, which would
    /// defeat the whole point of calibrated sizes.
    /// </para>
    /// </remarks>
    private static (double WidthMm, double HeightMm) ScaleToFit(
        UnfoldedUnit unit,
        UnitBand band,
        SourceImageSize source,
        LayoutSettings layout)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(layout);

        if (source.WidthPx <= 0 || source.HeightPx <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(source),
                source,
                "Source image dimensions must both be positive.");
        }

        double boxWidthMm = unit.WidthMm - (2 * layout.SilhouetteMarginMm);
        double boxHeightMm = band.HeightMm - layout.SilhouetteMarginMm;

        if (boxWidthMm <= 0 || boxHeightMm <= 0)
        {
            // DEC-038, convention 5: a silhouette margin wider than the pawn
            // leaves nothing to draw in. Fail here rather than emit an empty
            // outline and find out on paper.
            throw new ArgumentOutOfRangeException(
                nameof(layout),
                layout.SilhouetteMarginMm,
                $"Silhouette margin leaves no room in a {unit.WidthMm} × {band.HeightMm} mm band.");
        }

        // The smaller of the two ratios is the one that makes the image fit in
        // both directions; the other would overflow the box.
        double scale = Math.Min(boxWidthMm / source.WidthPx, boxHeightMm / source.HeightPx);

        return (source.WidthPx * scale, source.HeightPx * scale);
    }

    private static double CentredXMm(double unitWidthMm, double drawnWidthMm)
    {
        return (unitWidthMm - drawnWidthMm) / 2;
    }
}
