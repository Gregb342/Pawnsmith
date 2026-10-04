using System.Buffers.Binary;
using System.IO.Compression;

namespace Pawnsmith.Infrastructure.Imaging;

/// <summary>
/// Writes an RGBA image as a PNG holding <c>IHDR</c>, <c>IDAT</c> and <c>IEND</c>,
/// and nothing else (§F.2.2, DEC-099).
/// </summary>
/// <remarks>
/// <b>No metadata, ever.</b> ComfyUI writes its graph and the prompt in text
/// chunks of every image. A cut-out is always re-encoded from its pixels by
/// this type, never copied from the source chunks, so that nothing of the
/// source travels into a <c>Share</c> archive (DEC-079).
/// <para>
/// Every row uses filter type 0 (no prediction). The other filters make a
/// smaller file; a cut-out is mostly transparent and compresses well anyway,
/// and one filter is one thing less to review.
/// </para>
/// </remarks>
public static class PngEncoder
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static byte[] Encode(RgbaImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        byte[] header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), image.WidthPx);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), image.HeightPx);
        header[8] = 8;   // bits per channel
        header[9] = 6;   // colour type: RGBA
        header[10] = 0;  // compression method: deflate
        header[11] = 0;  // filter method: the five adaptive filters
        header[12] = 0;  // not interlaced

        int stride = image.WidthPx * RgbaImage.Channels;
        using var compressed = new MemoryStream();

        using (var zlib = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true))
        {
            for (int y = 0; y < image.HeightPx; y++)
            {
                zlib.WriteByte(0);
                zlib.Write(image.Pixels, y * stride, stride);
            }
        }

        using var file = new MemoryStream();
        file.Write(Signature);
        WriteChunk(file, "IHDR"u8, header);
        WriteChunk(file, "IDAT"u8, compressed.ToArray());
        WriteChunk(file, "IEND"u8, []);

        return file.ToArray();
    }

    private static void WriteChunk(Stream file, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> four = stackalloc byte[4];

        BinaryPrimitives.WriteInt32BigEndian(four, data.Length);
        file.Write(four);
        file.Write(type);
        file.Write(data);
        BinaryPrimitives.WriteUInt32BigEndian(four, Crc32.Compute(type, data));
        file.Write(four);
    }
}
