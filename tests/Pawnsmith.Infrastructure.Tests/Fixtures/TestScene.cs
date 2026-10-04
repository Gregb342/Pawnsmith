using Pawnsmith.Infrastructure.Cutout;
using Pawnsmith.Infrastructure.Imaging;

namespace Pawnsmith.Infrastructure.Tests.Fixtures;

/// <summary>
/// Paired images built in code, as the framing clause asks the generator for
/// them: two standing figures on a pale grey background, front on the left,
/// back on the right (§F.9).
/// </summary>
internal static class TestScene
{
    public static readonly Rgb Grey = new(200, 201, 199);

    /// <summary>
    /// A paired image <c>2 × halfWidth</c> (plus <paramref name="extraMiddleColumn"/>)
    /// wide; each half holds a figure whose bounding box is x 20 to 39, y 10 to
    /// the bottom edge, in the colour given.
    /// </summary>
    public static RgbaImage Pair(Rgb front, Rgb back, int halfWidth = 60, int height = 200, bool extraMiddleColumn = false)
    {
        int width = (2 * halfWidth) + (extraMiddleColumn ? 1 : 0);
        var image = RgbaImage.Blank(width, height);
        Fill(image, 0, 0, width, height, Grey);

        Figure(image, 0, front);
        Figure(image, width - halfWidth, back);

        return image;
    }

    /// <summary>The same, encoded as the generator would return it.</summary>
    public static byte[] PairPng(Rgb front, Rgb back, bool extraMiddleColumn = false) =>
        PngEncoder.Encode(Pair(front, back, extraMiddleColumn: extraMiddleColumn));

    public static void Fill(RgbaImage image, int x, int y, int width, int height, Rgb colour)
    {
        for (int row = y; row < y + height; row++)
        {
            for (int column = x; column < x + width; column++)
            {
                int at = image.Offset(column, row);
                image.Pixels[at] = colour.R;
                image.Pixels[at + 1] = colour.G;
                image.Pixels[at + 2] = colour.B;
                image.Pixels[at + 3] = 255;
            }
        }
    }

    private static void Figure(RgbaImage image, int left, Rgb colour)
    {
        Fill(image, left + 24, 10, 12, 12, colour);                       // head
        Fill(image, left + 20, 22, 20, 80, colour);                       // body
        Fill(image, left + 22, 102, 6, image.HeightPx - 102, colour);     // legs, down to
        Fill(image, left + 32, 102, 6, image.HeightPx - 102, colour);     // the bottom edge
    }
}
