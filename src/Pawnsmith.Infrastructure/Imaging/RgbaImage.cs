namespace Pawnsmith.Infrastructure.Imaging;

/// <summary>
/// An image in memory: four bytes per pixel — red, green, blue, alpha — row
/// after row, from the top left corner.
/// </summary>
/// <remarks>
/// The one pixel layout of the cut-out (§F.2). A decoded RGB image gets an
/// alpha of 255 everywhere, so that the algorithm never has two cases.
/// </remarks>
public sealed class RgbaImage
{
    /// <summary>Bytes per pixel.</summary>
    public const int Channels = 4;

    public RgbaImage(int widthPx, int heightPx, byte[] pixels)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(widthPx, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(heightPx, 1);
        ArgumentNullException.ThrowIfNull(pixels);

        if (pixels.LongLength != (long)widthPx * heightPx * Channels)
        {
            throw new ArgumentException(
                $"A {widthPx} x {heightPx} image holds {(long)widthPx * heightPx * Channels} bytes, not {pixels.LongLength}.",
                nameof(pixels));
        }

        WidthPx = widthPx;
        HeightPx = heightPx;
        Pixels = pixels;
    }

    public int WidthPx { get; }

    public int HeightPx { get; }

    /// <summary>The pixels; changing them changes the image.</summary>
    public byte[] Pixels { get; }

    /// <summary>A transparent black image of the given size.</summary>
    public static RgbaImage Blank(int widthPx, int heightPx) =>
        new(widthPx, heightPx, new byte[(long)widthPx * heightPx * Channels]);

    /// <summary>The index of the red byte of the pixel at (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public int Offset(int x, int y) => ((y * WidthPx) + x) * Channels;

    /// <summary>A copy of a rectangle of this image.</summary>
    public RgbaImage Crop(int x, int y, int widthPx, int heightPx)
    {
        if (x < 0 || y < 0 || widthPx < 1 || heightPx < 1 || x + widthPx > WidthPx || y + heightPx > HeightPx)
        {
            throw new ArgumentOutOfRangeException(
                nameof(x), $"The rectangle ({x}, {y}, {widthPx} x {heightPx}) is not inside a {WidthPx} x {HeightPx} image.");
        }

        RgbaImage copy = Blank(widthPx, heightPx);
        int rowBytes = widthPx * Channels;

        for (int row = 0; row < heightPx; row++)
        {
            Array.Copy(Pixels, Offset(x, y + row), copy.Pixels, row * rowBytes, rowBytes);
        }

        return copy;
    }
}
