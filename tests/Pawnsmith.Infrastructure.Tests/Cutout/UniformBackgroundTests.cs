using Pawnsmith.Application.Ports;
using Pawnsmith.Infrastructure.Cutout;
using Pawnsmith.Infrastructure.Imaging;

namespace Pawnsmith.Infrastructure.Tests.Cutout;

/// <summary>
/// The cut-out algorithm, on scenes built in code. Covers tests 7 to 16 of F.9.
/// </summary>
/// <remarks>
/// The scene is one half of a paired image as the framing clause asks for it:
/// a pale grey background with a little noise, and a standing figure — head,
/// body, two legs whose feet touch the bottom edge, with a gap between them.
/// </remarks>
public class UniformBackgroundTests
{
    private static readonly Rgb Grey = new(200, 201, 199);
    private static readonly Rgb Skin = new(90, 130, 60);
    private static readonly Rgb Tunic = new(100, 70, 50);
    private static readonly Rgb Boots = new(40, 35, 30);
    private static readonly Rgb Ground = new(120, 90, 60);

    private static readonly CutoutOptions Options = new();

    private const int Width = 60;
    private const int Height = 200;

    /// <summary>The figure's bounding box: x 20 to 39, y 10 to 199.</summary>
    private static RgbaImage Scene(int groundRows = 0)
    {
        var image = RgbaImage.Blank(Width, Height);
        var noise = new Random(7);

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                // A generated "uniform" grey is never perfectly flat.
                Set(image, x, y, new Rgb((byte)(Grey.R + noise.Next(-4, 5)), (byte)(Grey.G + noise.Next(-4, 5)), (byte)(Grey.B + noise.Next(-4, 5))));
            }
        }

        Fill(image, 24, 10, 12, 12, Skin);    // head
        Fill(image, 20, 22, 20, 80, Tunic);   // body, y 22 to 101
        Fill(image, 22, 102, 6, 98, Boots);   // left leg, down to the bottom edge
        Fill(image, 32, 102, 6, 98, Boots);   // right leg; the gap is x 28 to 31

        if (groundRows > 0)
        {
            Fill(image, 0, Height - groundRows, Width, groundRows, Ground);
        }

        return image;
    }

    private static void Set(RgbaImage image, int x, int y, Rgb colour)
    {
        int at = image.Offset(x, y);
        image.Pixels[at] = colour.R;
        image.Pixels[at + 1] = colour.G;
        image.Pixels[at + 2] = colour.B;
        image.Pixels[at + 3] = 255;
    }

    private static void Fill(RgbaImage image, int x, int y, int width, int height, Rgb colour)
    {
        for (int row = y; row < y + height; row++)
        {
            for (int column = x; column < x + width; column++)
            {
                Set(image, column, row, colour);
            }
        }
    }

    private static byte Alpha(RgbaImage image, int x, int y) => image.Pixels[image.Offset(x, y) + 3];

    private static Rgb Colour(RgbaImage image, int x, int y)
    {
        int at = image.Offset(x, y);
        return new Rgb(image.Pixels[at], image.Pixels[at + 1], image.Pixels[at + 2]);
    }

    private static RgbaImage CutOut(RgbaImage scene) => UniformBackground.CutOut(scene, Options, CancellationToken.None);

    // --- F.9 n° 7 : la couleur du fond -------------------------------------------------------------

    [Fact]
    public void TheBackgroundIsTheMedianOfTheTopLeftAndRightBordersNotTheBottom()
    {
        RgbaImage scene = Scene(groundRows: 8);

        // A sword tip touching the top edge does not move a median.
        Fill(scene, 29, 0, 2, 10, new Rgb(150, 150, 160));

        Rgb background = UniformBackground.BackgroundColour(scene);

        Math.Abs(background.R - Grey.R).ShouldBeLessThanOrEqualTo(4);
        Math.Abs(background.G - Grey.G).ShouldBeLessThanOrEqualTo(4);
        Math.Abs(background.B - Grey.B).ShouldBeLessThanOrEqualTo(4);
    }

    // --- F.9 n° 8 : un fond trop peu uni -----------------------------------------------------------

    [Fact]
    public void ABorderThatIsNotAUniformBackgroundIsRefused()
    {
        var scenery = RgbaImage.Blank(Width, Height);
        new Random(3).NextBytes(scenery.Pixels);

        CutoutException error = Should.Throw<CutoutException>(() => CutOut(scenery));

        error.WireCode.ShouldBe("CUTOUT_BACKGROUND_NOT_UNIFORM");
    }

    // --- F.9 n° 9 et 15 : le fond part, le sujet reste, l'image est recadrée ------------------------

    [Fact]
    public void TheBackgroundBecomesTransparentAndTheImageIsCroppedToTheFigure()
    {
        RgbaImage result = CutOut(Scene());

        // The figure's bounding box, x 20 to 39 and y 10 to 199.
        result.WidthPx.ShouldBe(20);
        result.HeightPx.ShouldBe(190);

        // Inside the body, opaque and untouched.
        Alpha(result, 10, 50).ShouldBe((byte)255);
        Colour(result, 10, 50).ShouldBe(Tunic);

        // Beside the head, inside the box: background, transparent.
        Alpha(result, 0, 0).ShouldBe((byte)0);
    }

    // --- F.9 n° 10 : la bande de sol est retirée ---------------------------------------------------------

    [Fact]
    public void TheGroundBandIsRemovedAndTheFeetBecomeTheBottomOfTheImage()
    {
        RgbaImage result = CutOut(Scene(groundRows: 8));

        // The legs now end 8 rows higher, and nothing of the band is left.
        result.HeightPx.ShouldBe(190 - 8);

        for (int x = 0; x < result.WidthPx; x++)
        {
            if (Alpha(result, x, result.HeightPx - 1) > 0)
            {
                Colour(result, x, result.HeightPx - 1).ShouldBe(Boots);
            }
        }
    }

    // --- F.9 n° 11 : jamais au-delà de 5 % ----------------------------------------------------------------

    [Fact]
    public void TheGroundBandIsNeverRemovedBeyondFivePercentOfTheHeight()
    {
        // Twenty rows of ground; 5 % of 200 is 10.
        RgbaImage scene = Scene(groundRows: 20);

        UniformBackground.RowsAboveGroundBand(scene, UniformBackground.BackgroundColour(scene), Options).ShouldBe(Height - 10);
    }

    // --- F.9 n° 12 : l'espace entre les jambes ----------------------------------------------------------

    [Fact]
    public void TheGapBetweenTheLegsClosedByTheGroundBandBecomesTransparent()
    {
        RgbaImage result = CutOut(Scene(groundRows: 8));

        // x 30 in the scene is x 10 in the result; the last row, just above
        // where the band was, would have stayed grey had the band been kept.
        Alpha(result, 10, result.HeightPx - 1).ShouldBe((byte)0);
        Alpha(result, 10, 150).ShouldBe((byte)0);
    }

    // --- F.9 n° 13 : trou de fond et vêtement gris --------------------------------------------------------

    [Fact]
    public void AnEnclosedHoleOfBackgroundGoesButAGreyGarmentStays()
    {
        RgbaImage scene = Scene();

        // Background enclosed by the body, as between an arm and the torso.
        Fill(scene, 24, 40, 5, 5, Grey);

        // A grey patch 18 away from the background: above the strict
        // tolerance of 12, under the flood tolerance of 24.
        Fill(scene, 31, 40, 5, 5, new Rgb(218, 219, 217));

        RgbaImage result = CutOut(scene);

        // Scene x 26 and 33, y 42 are result x 6 and 13, y 32.
        Alpha(result, 6, 32).ShouldBe((byte)0);
        Alpha(result, 13, 32).ShouldBe((byte)255);
    }

    // --- F.9 n° 14 : le bord adouci -------------------------------------------------------------------------

    [Fact]
    public void AnEdgePixelGetsAnOpacityProportionalToItsDistanceFromTheBackground()
    {
        RgbaImage scene = Scene();

        // The left column of the body, 36 away from the exact grey: halfway
        // between the tolerance (24) and twice the tolerance (48).
        Fill(scene, 20, 30, 1, 40, new Rgb(236, 237, 235));

        RgbaImage result = CutOut(scene);

        // The exact value depends on the median the noise gives; it sits
        // around 127, well clear of both ends.
        byte alpha = Alpha(result, 0, 40);
        alpha.ShouldBeGreaterThan((byte)60);
        alpha.ShouldBeLessThan((byte)200);
    }

    // --- F.9 n° 16 : pas de sujet --------------------------------------------------------------------------

    [Fact]
    public void AnImageWithNoSubjectIsRefused()
    {
        var empty = RgbaImage.Blank(Width, Height);
        Fill(empty, 0, 0, Width, Height, Grey);

        Should.Throw<CutoutException>(() => CutOut(empty)).WireCode.ShouldBe("CUTOUT_SUBJECT_NOT_FOUND");
    }

    [Fact]
    public void ASpeckIsNotACharacter()
    {
        var almostEmpty = RgbaImage.Blank(Width, Height);
        Fill(almostEmpty, 0, 0, Width, Height, Grey);

        // Nine pixels of 12 000: under the 1 % a character needs.
        Fill(almostEmpty, 30, 100, 3, 3, Tunic);

        Should.Throw<CutoutException>(() => CutOut(almostEmpty)).WireCode.ShouldBe("CUTOUT_SUBJECT_NOT_FOUND");
    }
}
