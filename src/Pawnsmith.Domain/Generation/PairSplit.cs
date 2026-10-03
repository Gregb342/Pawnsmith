namespace Pawnsmith.Domain.Generation;

/// <summary>A rectangle of pixels in an image, origin at the top-left corner.</summary>
/// <remarks>
/// Pixels, not millimetres: this is the one domain type that speaks in pixels,
/// because it describes a region of a source image, not anything that will be
/// printed. Convention 4 of DEC-038 ("millimetres everywhere") is about the
/// sheet, and nothing here reaches the sheet.
/// </remarks>
/// <param name="X">Left edge, in pixels from the left of the image.</param>
/// <param name="Y">Top edge, in pixels from the top of the image.</param>
/// <param name="Width">Width in pixels. At least one.</param>
/// <param name="Height">Height in pixels. At least one.</param>
public sealed record PixelRect(int X, int Y, int Width, int Height);

/// <summary>The two views of a paired image: the front on the left, the back on the right.</summary>
public sealed record PairHalves(PixelRect Front, PixelRect Back);

/// <summary>
/// Where the paired image is cut in two (§E.8, DEC-079).
/// </summary>
/// <remarks>
/// <para>
/// <b>Decided here, executed in T5.</b> T4 uses the rule on reception, to refuse
/// an image that cannot be cut; T5 calls it to cut the pixels just before the
/// background removal. The pixels are not cut in T4 because the schema has no
/// place for non-transparent halves, because their only reader is the
/// background remover, and because cutting pixels means decoding a PNG — a
/// library choice reserved, with T5's model, to the project owner.
/// </para>
/// <para>
/// <b>The front is assumed to be on the left, not detected.</b> The framing
/// clause asks for "front view on the left and back view on the right", and T0a
/// obtained it on three subjects out of three (DEC-043). A detection that got it
/// wrong would swap the front and back of a pawn without anything saying so; an
/// assumption the framing clause guarantees fails visibly, on the paired image
/// the user is looking at.
/// </para>
/// <para>
/// <b>The cut is vertical, at the exact middle.</b> No search for the "true"
/// gap between the two figures: T0a measured 0 to 1.4 % of misalignment between
/// the views, and the background removal clears both sides of the cut anyway.
/// </para>
/// </remarks>
public static class PairSplit
{
    /// <summary>Narrowest image that still has a pixel for each view.</summary>
    public const int MinimumWidthPx = 2;

    /// <summary>Whether an image of this size can be cut in two.</summary>
    public static bool IsSplittable(int widthPx, int heightPx) =>
        widthPx >= MinimumWidthPx && heightPx >= 1;

    /// <summary>The two halves of an image of the given size.</summary>
    /// <remarks>
    /// <b>An odd width loses its middle column</b>, and both halves keep the
    /// same width. Giving the extra pixel to one side would make the two views
    /// of one character differ by a pixel in width; DEC-041 has just
    /// established that the pair shares one scale, and two sources of
    /// different widths would enter it with a bias no image justifies.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">The image is narrower than two pixels, or has no height.</exception>
    public static PairHalves Of(int widthPx, int heightPx)
    {
        if (!IsSplittable(widthPx, heightPx))
        {
            throw new ArgumentOutOfRangeException(
                nameof(widthPx),
                $"An image of {widthPx} × {heightPx} pixels cannot be cut into two views: " +
                $"it needs at least {MinimumWidthPx} pixels of width and one of height.");
        }

        // Integer division floors, which is the rule: on 1217 pixels, both
        // halves are 608 wide and column 608 belongs to neither.
        int half = widthPx / 2;

        return new PairHalves(
            Front: new PixelRect(X: 0, Y: 0, Width: half, Height: heightPx),
            Back: new PixelRect(X: widthPx - half, Y: 0, Width: half, Height: heightPx));
    }
}
