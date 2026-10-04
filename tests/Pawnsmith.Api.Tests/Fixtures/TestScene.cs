using Pawnsmith.Infrastructure.Imaging;

namespace Pawnsmith.Api.Tests.Fixtures;

/// <summary>
/// A paired image built in code, as the framing clause asks the generator for
/// it: two standing figures on a pale grey background, front left, back right.
/// </summary>
/// <remarks>
/// A copy of the fixture of the infrastructure tests, reduced to what the API
/// tests need: the two test projects do not reference each other, like
/// <see cref="TestPng"/>.
/// </remarks>
internal static class TestScene
{
    /// <summary>A 120 x 200 paired image whose figures are 20 x 190 once cut out.</summary>
    public static byte[] PairPng()
    {
        var image = RgbaImage.Blank(120, 200);
        Fill(image, 0, 0, 120, 200, 200, 201, 199);

        foreach (int left in new[] { 0, 60 })
        {
            Fill(image, left + 24, 10, 12, 12, 90, 130, 60);
            Fill(image, left + 20, 22, 20, 80, 100, 70, 50);
            Fill(image, left + 22, 102, 6, 98, 40, 35, 30);
            Fill(image, left + 32, 102, 6, 98, 40, 35, 30);
        }

        return PngEncoder.Encode(image);
    }

    private static void Fill(RgbaImage image, int x, int y, int width, int height, byte r, byte g, byte b)
    {
        for (int row = y; row < y + height; row++)
        {
            for (int column = x; column < x + width; column++)
            {
                int at = image.Offset(column, row);
                image.Pixels[at] = r;
                image.Pixels[at + 1] = g;
                image.Pixels[at + 2] = b;
                image.Pixels[at + 3] = 255;
            }
        }
    }
}
