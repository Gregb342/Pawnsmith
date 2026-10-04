using System.Text;

using Pawnsmith.Application.Ports;
using Pawnsmith.Infrastructure.Imaging;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Imaging;

/// <summary>
/// The PNG decoder and encoder of T5. Covers tests 1 to 6 of F.9.
/// </summary>
public class PngCodecTests
{
    private const int Bound = 8192;

    /// <summary>An image whose every pixel differs, so that a wrong predictor shows.</summary>
    private static RgbaImage Noise(int width, int height, bool opaque)
    {
        var random = new Random(42);
        var image = RgbaImage.Blank(width, height);
        random.NextBytes(image.Pixels);

        if (opaque)
        {
            for (int i = 3; i < image.Pixels.Length; i += 4)
            {
                image.Pixels[i] = 255;
            }
        }

        return image;
    }

    private static CutoutException Refused(byte[] png) =>
        Should.Throw<CutoutException>(() => PngDecoder.Decode(png, Bound));

    // --- F.9 n° 1 : aller-retour, les cinq filtres ---------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void AnRgbaImageComesBackIdenticalWhateverTheFilter(byte filter)
    {
        RgbaImage original = Noise(17, 9, opaque: false);

        RgbaImage decoded = PngDecoder.Decode(new PngWriter { Filter = filter }.Write(original), Bound);

        decoded.WidthPx.ShouldBe(17);
        decoded.HeightPx.ShouldBe(9);
        decoded.Pixels.ShouldBe(original.Pixels);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void AnRgbImageComesBackOpaque(byte filter)
    {
        RgbaImage original = Noise(13, 7, opaque: true);

        RgbaImage decoded = PngDecoder.Decode(new PngWriter { ColourType = 2, Filter = filter }.Write(original), Bound);

        decoded.Pixels.ShouldBe(original.Pixels);
    }

    [Fact]
    public void TheEncoderWritesWhatTheDecoderReads()
    {
        RgbaImage original = Noise(31, 5, opaque: false);

        PngDecoder.Decode(PngEncoder.Encode(original), Bound).Pixels.ShouldBe(original.Pixels);
    }

    // --- F.9 n° 2 : hors du sous-ensemble ------------------------------------------------------------

    [Theory]
    [InlineData(8, 3, 0, "palette")]
    [InlineData(8, 0, 0, "greyscale")]
    [InlineData(8, 4, 0, "greyscale with alpha")]
    [InlineData(16, 6, 0, "16 bits")]
    [InlineData(8, 6, 1, "interlaced")]
    public void APngOutsideTheSubsetIsRefusedByWhatItLacks(byte bitDepth, byte colourType, byte interlace, string named)
    {
        byte[] png = new PngWriter { BitDepth = bitDepth, ColourType = colourType, Interlace = interlace }.Write(Noise(4, 4, opaque: true));

        CutoutException error = Refused(png);

        error.WireCode.ShouldBe("CUTOUT_IMAGE_INVALID");
        error.Message.ShouldContain(named);
    }

    [Fact]
    public void SomethingThatIsNotAPngIsRefused()
    {
        Refused("definitely not a picture"u8.ToArray()).WireCode.ShouldBe("CUTOUT_IMAGE_INVALID");
        Refused([]).WireCode.ShouldBe("CUTOUT_IMAGE_INVALID");
    }

    // --- F.9 n° 3 : la borne, sur l'en-tête ------------------------------------------------------------

    [Theory]
    [InlineData(8193, 10)]
    [InlineData(10, 8193)]
    [InlineData(60000, 60000)]
    public void ASideBeyondTheBoundIsRefusedOnTheHeaderAlone(int width, int height)
    {
        // Twenty-four bytes: there is nothing after the header to decompress.
        CutoutException error = Refused(TestPng.HeaderOnly(width, height));

        error.WireCode.ShouldBe("CUTOUT_IMAGE_TOO_LARGE");
    }

    // --- F.9 n° 4 et 5 : intégrité ---------------------------------------------------------------------

    [Fact]
    public void AWrongCrcIsRefused()
    {
        byte[] png = new PngWriter { CorruptCrc = true }.Write(Noise(4, 4, opaque: false));

        Refused(png).Message.ShouldContain("CRC");
    }

    [Theory]
    [InlineData(1, "more")]
    [InlineData(1000, "more")]
    [InlineData(-1, "less")]
    public void DataLongerOrShorterThanTheHeaderAnnouncesIsRefused(int delta, string named)
    {
        byte[] png = new PngWriter { RawLengthDelta = delta }.Write(Noise(6, 6, opaque: false));

        CutoutException error = Refused(png);

        error.WireCode.ShouldBe("CUTOUT_IMAGE_INVALID");
        error.Message.ShouldContain(named);
    }

    [Fact]
    public void AFileCutShortIsRefused()
    {
        byte[] png = new PngWriter().Write(Noise(6, 6, opaque: false));

        Refused(png[..^20]).WireCode.ShouldBe("CUTOUT_IMAGE_INVALID");
    }

    [Fact]
    public void AnUnknownFilterTypeIsRefused()
    {
        byte[] png = new PngWriter { Filter = 5 }.Write(Noise(3, 3, opaque: false));

        Refused(png).Message.ShouldContain("filter type 5");
    }

    // --- F.9 n° 6 : aucune métadonnée ne survit ---------------------------------------------------------

    [Fact]
    public void ATextChunkOfTheSourceDoesNotSurviveAReencoding()
    {
        // What ComfyUI writes in every image: its graph and the prompt.
        byte[] source = new PngWriter
        {
            ExtraChunks = [("tEXt", Encoding.ASCII.GetBytes("prompt\0{\"6\":{\"inputs\":{\"text\":\"a goblin\"}}}"))],
        }.Write(Noise(5, 5, opaque: false));

        PngWriter.ChunkTypes(source).ShouldContain("tEXt");

        byte[] reencoded = PngEncoder.Encode(PngDecoder.Decode(source, Bound));

        PngWriter.ChunkTypes(reencoded).ShouldBe(["IHDR", "IDAT", "IEND"]);
        Encoding.ASCII.GetString(reencoded).ShouldNotContain("goblin");
    }
}
