namespace Pawnsmith.Domain;

/// <summary>
/// The complete cut-out of one figure before folding (B.4.1), in millimetres,
/// relative to the unit itself.
/// </summary>
/// <remarks>
/// <para>
/// Five bands, top to bottom: back appendix, back image, <b>fold line</b>,
/// front image, front appendix. The unit is symmetrical about the fold line,
/// so the fold falls exactly at half the total height.
/// </para>
/// <para>
/// The fold line sits at the <b>top of the front image</b> — above the
/// character's head, not below its feet. Once folded, the back panel drops
/// behind the front one and ends up the right way up. Which is why the back
/// image is drawn rotated by 180°: omitting that rotation produces a character
/// standing on its head, and is the most likely mistake of this slice.
/// </para>
/// <para>
/// This type carries geometry only. Where the artwork goes inside each image
/// band, and what the cut outline looks like, are separate concerns.
/// </para>
/// </remarks>
/// <param name="WidthMm">Width of the unit, in millimetres.</param>
/// <param name="TotalHeightMm">Unfolded height: <c>2 × (pawnHeight + appendixHeight)</c>.</param>
/// <param name="FoldLineYMm">Position of the main fold, at half the total height.</param>
/// <param name="BackAppendix">Topmost band, mirror of the front appendix.</param>
/// <param name="BackImage">Back artwork, drawn rotated by 180°.</param>
/// <param name="FrontImage">Front artwork, drawn upright.</param>
/// <param name="FrontAppendix">Bottom band: flaps or tab, depending on geometry.</param>
/// <param name="FoldLinesYMm">Every fold to be drawn, top to bottom. See <see cref="Create"/>.</param>
public sealed record UnfoldedUnit(
    double WidthMm,
    double TotalHeightMm,
    double FoldLineYMm,
    UnitBand BackAppendix,
    UnitBand BackImage,
    UnitBand FrontImage,
    UnitBand FrontAppendix,
    IReadOnlyList<double> FoldLinesYMm)
{
    /// <summary>
    /// Builds the unfolded unit for one size and geometry.
    /// </summary>
    /// <param name="pawn">Pawn dimensions for the size, read from the calibration file.</param>
    /// <param name="geometry">Geometry of the whole sheet (DEC-001).</param>
    /// <param name="geometrySettings">Appendix dimensions, read from the calibration file.</param>
    public static UnfoldedUnit Create(
        PawnDimensions pawn,
        Geometry geometry,
        GeometrySettings geometrySettings)
    {
        ArgumentNullException.ThrowIfNull(pawn);
        ArgumentNullException.ThrowIfNull(geometrySettings);

        double appendixHeightMm = AppendixHeightMm(geometry, geometrySettings);
        double totalHeightMm = 2 * (pawn.PawnHeightMm + appendixHeightMm);

        // Bands are laid out top to bottom, each starting where the previous ended.
        UnitBand backAppendix = new(TopMm: 0, HeightMm: appendixHeightMm);
        UnitBand backImage = new(backAppendix.BottomMm, pawn.PawnHeightMm);
        UnitBand frontImage = new(backImage.BottomMm, pawn.PawnHeightMm);
        UnitBand frontAppendix = new(frontImage.BottomMm, appendixHeightMm);

        // The fold is the boundary between the two images, which is also the
        // midpoint of a unit symmetrical about it.
        double foldLineYMm = frontImage.TopMm;

        return new UnfoldedUnit(
            WidthMm: pawn.PawnWidthMm,
            TotalHeightMm: totalHeightMm,
            FoldLineYMm: foldLineYMm,
            BackAppendix: backAppendix,
            BackImage: backImage,
            FrontImage: frontImage,
            FrontAppendix: frontAppendix,
            FoldLinesYMm: FoldLines(geometry, foldLineYMm, backImage, frontImage));
    }

    private static double AppendixHeightMm(Geometry geometry, GeometrySettings settings)
    {
        return geometry switch
        {
            Geometry.FoldedTent => settings.FoldedTent.FlapHeightMm,
            Geometry.TabAndSocket => settings.TabAndSocket.TabHeightMm,
            _ => throw new ArgumentOutOfRangeException(nameof(geometry), geometry, null),
        };
    }

    /// <summary>
    /// Every fold to be drawn, top to bottom (B.4.2, B.5.4).
    /// </summary>
    /// <remarks>
    /// <c>FoldedTent</c> gets three: the main fold, plus one at each
    /// image/appendix boundary, where the flaps fold outwards to form the base.
    /// <c>TabAndSocket</c> gets one: the tab is rigid with the figure and
    /// slides into the socket, so it is never folded.
    /// </remarks>
    private static IReadOnlyList<double> FoldLines(
        Geometry geometry,
        double foldLineYMm,
        UnitBand backImage,
        UnitBand frontImage)
    {
        return geometry switch
        {
            Geometry.FoldedTent => [backImage.TopMm, foldLineYMm, frontImage.BottomMm],
            Geometry.TabAndSocket => [foldLineYMm],
            _ => throw new ArgumentOutOfRangeException(nameof(geometry), geometry, null),
        };
    }
}
