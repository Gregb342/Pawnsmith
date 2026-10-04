using System.Buffers.Binary;
using System.Globalization;
using System.IO.Compression;

using Pawnsmith.Application.Ports;

namespace Pawnsmith.Infrastructure.Imaging;

/// <summary>
/// Decodes the PNG files ComfyUI writes: 8 bits per channel, RGB or RGBA, not
/// interlaced (§F.2.1, DEC-099). Anything else is refused with a code.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing is trusted before it is checked</b> (MEN-005), in this order:
/// the signature; the dimensions, on the first 24 bytes, before a single byte
/// is allocated for pixels; the CRC of every chunk; the format fields; and the
/// size of the decompressed data, which the header fixes exactly —
/// <c>height × (1 + width × bytes per pixel)</c>. The zlib stream is read up to
/// that size and no further, and one byte more or less is a refusal. A
/// decompression bomb therefore never inflates past what an already bounded
/// header announced.
/// </para>
/// <para>
/// <b>A PNG in two sentences.</b> After an eight-byte signature, the file is a
/// list of chunks — length, four-letter type, data, CRC — starting with
/// <c>IHDR</c> (the format) and ending with <c>IEND</c>; the pixels are in one or
/// more <c>IDAT</c> chunks, concatenated into one zlib stream. Each row of
/// pixels starts with a filter byte telling how the row was predicted from its
/// neighbours, which <see cref="Unfilter"/> undoes.
/// </para>
/// </remarks>
public static class PngDecoder
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Decodes a PNG into RGBA pixels.</summary>
    /// <param name="png">The whole file.</param>
    /// <param name="maxDimensionPx">The longest side accepted.</param>
    /// <exception cref="CutoutException"><c>CUTOUT_IMAGE_INVALID</c> or <c>CUTOUT_IMAGE_TOO_LARGE</c>.</exception>
    public static RgbaImage Decode(byte[] png, int maxDimensionPx)
    {
        ArgumentNullException.ThrowIfNull(png);

        // 1. Signature and declared size, on the first 24 bytes (MEN-005).
        if (!PngHeader.TryRead(png, out int width, out int height))
        {
            throw Invalid("it is not a PNG file, or its header is incomplete");
        }

        if (width > maxDimensionPx || height > maxDimensionPx)
        {
            throw new CutoutException(
                CutoutErrorCode.ImageTooLarge,
                string.Create(CultureInfo.InvariantCulture,
                    $"The image declares {width} x {height} pixels, more than the {maxDimensionPx} allowed on a side; it is refused before being decoded (MEN-005)."));
        }

        // 2. Chunks, each with its CRC checked.
        int position = Signature.Length;
        bool first = true, ended = false;
        int bytesPerPixel = 0;
        using var compressed = new MemoryStream();

        while (!ended)
        {
            if (position + 8 > png.Length)
            {
                throw Invalid("it ends before its IEND chunk");
            }

            uint declared = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(position, 4));

            if (declared > int.MaxValue || position + 12L + declared > png.Length)
            {
                throw Invalid("a chunk declares more data than the file holds");
            }

            int length = (int)declared;
            ReadOnlySpan<byte> type = png.AsSpan(position + 4, 4);
            ReadOnlySpan<byte> data = png.AsSpan(position + 8, length);
            uint crc = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(position + 8 + length, 4));
            string name = System.Text.Encoding.ASCII.GetString(type);

            if (Crc32.Compute(type, data) != crc)
            {
                throw Invalid($"the CRC of its {name} chunk is wrong: the file is damaged");
            }

            if (first)
            {
                if (name != "IHDR" || length != 13)
                {
                    throw Invalid("its first chunk is not a valid IHDR");
                }

                bytesPerPixel = ReadFormat(data);
                first = false;
            }
            else if (name == "IDAT")
            {
                compressed.Write(data);
            }
            else if (name == "IEND")
            {
                ended = true;
            }
            else if (name != "PLTE" && char.IsUpper(name[0]))
            {
                // A critical chunk this decoder does not know: the format says
                // the image cannot be read correctly without it. Ancillary
                // chunks - lower case first letter, such as ComfyUI's tEXt -
                // are skipped, and never written back (§F.2.2).
                throw Invalid($"it holds a critical {name} chunk this reader does not handle");
            }

            position += 12 + length;
        }

        if (compressed.Length == 0)
        {
            throw Invalid("it holds no image data");
        }

        // 3. Exactly the size the header announces, no more, no less.
        long rowBytes = 1 + ((long)width * bytesPerPixel);
        long expected = rowBytes * height;
        byte[] raw = new byte[expected];
        compressed.Position = 0;

        try
        {
            using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
            zlib.ReadExactly(raw);

            if (zlib.ReadByte() != -1)
            {
                throw Invalid("its compressed data holds more than the header announces");
            }
        }
        catch (EndOfStreamException)
        {
            throw Invalid("its compressed data holds less than the header announces");
        }
        catch (InvalidDataException error)
        {
            throw new CutoutException(CutoutErrorCode.ImageInvalid, "The image is refused because its compressed data is not valid zlib.", error);
        }

        return Unfilter(raw, width, height, bytesPerPixel);
    }

    /// <summary>Checks the IHDR fields against the subset read, and gives the bytes per pixel.</summary>
    private static int ReadFormat(ReadOnlySpan<byte> header)
    {
        byte bitDepth = header[8], colourType = header[9], compression = header[10], filter = header[11], interlace = header[12];

        if (bitDepth != 8)
        {
            throw Invalid($"it uses {bitDepth} bits per channel; only 8 are read");
        }

        if (colourType != 2 && colourType != 6)
        {
            string kind = colourType switch { 0 => "greyscale", 3 => "palette", 4 => "greyscale with alpha", _ => $"colour type {colourType}" };
            throw Invalid($"it is a {kind} image; only RGB and RGBA are read");
        }

        if (compression != 0 || filter != 0)
        {
            throw Invalid("it declares a compression or filter method the format does not define");
        }

        if (interlace != 0)
        {
            throw Invalid("it is interlaced; only non-interlaced images are read");
        }

        return colourType == 6 ? 4 : 3;
    }

    /// <summary>Undoes the row filters and widens RGB to RGBA.</summary>
    private static RgbaImage Unfilter(byte[] raw, int width, int height, int bpp)
    {
        int stride = width * bpp;
        byte[] previous = new byte[stride];
        byte[] current = new byte[stride];
        var image = RgbaImage.Blank(width, height);

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * (stride + 1);
            byte filter = raw[rowStart];
            Array.Copy(raw, rowStart + 1, current, 0, stride);

            for (int i = 0; i < stride; i++)
            {
                // a: the byte to the left, b: above, c: above-left - the three
                // neighbours the five filters of the format predict from.
                int a = i >= bpp ? current[i - bpp] : 0;
                int b = previous[i];
                int c = i >= bpp ? previous[i - bpp] : 0;

                int prediction = filter switch
                {
                    0 => 0,
                    1 => a,
                    2 => b,
                    3 => (a + b) / 2,
                    4 => Paeth(a, b, c),
                    _ => throw Invalid($"row {y} uses filter type {filter}, which the format does not define"),
                };

                current[i] = unchecked((byte)(current[i] + prediction));
            }

            for (int x = 0; x < width; x++)
            {
                int to = image.Offset(x, y), from = x * bpp;
                image.Pixels[to] = current[from];
                image.Pixels[to + 1] = current[from + 1];
                image.Pixels[to + 2] = current[from + 2];
                image.Pixels[to + 3] = bpp == 4 ? current[from + 3] : (byte)255;
            }

            (previous, current) = (current, previous);
        }

        return image;
    }

    /// <summary>The Paeth predictor of the PNG specification: the neighbour closest to a + b - c.</summary>
    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);

        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    private static CutoutException Invalid(string why) =>
        new(CutoutErrorCode.ImageInvalid, $"The image is refused because {why} (DEC-099).");
}
