using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

using Pawnsmith.Infrastructure.Imaging;

namespace Pawnsmith.Infrastructure.Tests.Fixtures;

/// <summary>
/// Writes PNG files in code for the decoder tests, including the ones a real
/// encoder would never produce: any filter, any format field, an extra chunk,
/// a damaged CRC, too much or too little data.
/// </summary>
/// <remarks>
/// The filters are applied forward here, from the PNG specification, and undone
/// by the decoder under test: a round trip that comes back identical proves
/// both directions agree on all five.
/// </remarks>
internal sealed class PngWriter
{
    public byte BitDepth { get; init; } = 8;

    /// <summary>2 for RGB, 6 for RGBA; anything else for the refusal tests.</summary>
    public byte ColourType { get; init; } = 6;

    public byte Interlace { get; init; }

    /// <summary>The filter type applied to every row, 0 to 4.</summary>
    public byte Filter { get; init; }

    /// <summary>Ancillary chunks written between IHDR and IDAT, as ComfyUI does with tEXt.</summary>
    public IReadOnlyList<(string Type, byte[] Data)> ExtraChunks { get; init; } = [];

    /// <summary>Bytes added to (or, negative, removed from) the raw data before compression.</summary>
    public int RawLengthDelta { get; init; }

    /// <summary>Flips a bit of the IDAT CRC.</summary>
    public bool CorruptCrc { get; init; }

    public byte[] Write(RgbaImage image)
    {
        int bpp = ColourType == 6 ? 4 : 3;
        int stride = image.WidthPx * bpp;
        byte[] previous = new byte[stride];
        using var raw = new MemoryStream();

        for (int y = 0; y < image.HeightPx; y++)
        {
            byte[] row = new byte[stride];

            for (int x = 0; x < image.WidthPx; x++)
            {
                Array.Copy(image.Pixels, image.Offset(x, y), row, x * bpp, bpp);
            }

            raw.WriteByte(Filter);

            for (int i = 0; i < stride; i++)
            {
                int a = i >= bpp ? row[i - bpp] : 0, b = previous[i], c = i >= bpp ? previous[i - bpp] : 0;
                int prediction = Filter switch { 0 => 0, 1 => a, 2 => b, 3 => (a + b) / 2, 4 => Paeth(a, b, c), _ => 0 };
                raw.WriteByte(unchecked((byte)(row[i] - prediction)));
            }

            previous = row;
        }

        byte[] rawBytes = raw.ToArray();
        rawBytes = RawLengthDelta >= 0
            ? [.. rawBytes, .. new byte[RawLengthDelta]]
            : rawBytes[..(rawBytes.Length + RawLengthDelta)];

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.Fastest, leaveOpen: true))
        {
            zlib.Write(rawBytes);
        }

        byte[] header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), image.WidthPx);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), image.HeightPx);
        header[8] = BitDepth;
        header[9] = ColourType;
        header[12] = Interlace;

        using var file = new MemoryStream();
        file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        Chunk(file, "IHDR", header, corrupt: false);

        foreach ((string type, byte[] data) in ExtraChunks)
        {
            Chunk(file, type, data, corrupt: false);
        }

        Chunk(file, "IDAT", compressed.ToArray(), CorruptCrc);
        Chunk(file, "IEND", [], corrupt: false);

        return file.ToArray();
    }

    /// <summary>The four-letter types of the chunks of a PNG, in order.</summary>
    public static IReadOnlyList<string> ChunkTypes(byte[] png)
    {
        List<string> types = [];
        int position = 8;

        while (position + 8 <= png.Length)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(position, 4));
            types.Add(Encoding.ASCII.GetString(png, position + 4, 4));
            position += 12 + length;
        }

        return types;
    }

    private static void Chunk(Stream file, string type, byte[] data, bool corrupt)
    {
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        Span<byte> four = stackalloc byte[4];

        BinaryPrimitives.WriteInt32BigEndian(four, data.Length);
        file.Write(four);
        file.Write(typeBytes);
        file.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(four, Crc32.Compute(typeBytes, data) ^ (corrupt ? 1u : 0u));
        file.Write(four);
    }

    private static int Paeth(int a, int b, int c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);

        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }
}
