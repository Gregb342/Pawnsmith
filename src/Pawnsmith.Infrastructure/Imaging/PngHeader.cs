namespace Pawnsmith.Infrastructure.Imaging;

/// <summary>
/// Reads the width and height a PNG declares, from its first 24 bytes, without
/// decoding anything.
/// </summary>
/// <remarks>
/// <para>
/// A PNG starts with an eight-byte signature, then its first chunk, which the
/// format requires to be <c>IHDR</c>: four bytes of length, four bytes of type,
/// then the width and the height as big-endian 32-bit integers. Reading those is
/// what MEN-005 asks for first — the bounds are checked on what the file
/// <i>declares</i>, before anything trusts its contents.
/// </para>
/// <para>
/// <see cref="FileImageSizeReader"/> of T1 reads the same two numbers from a
/// file, with its own messages. It was left untouched rather than rewired onto
/// this type in T4: the duplication is ten lines, and T1 is written and tested
/// but not yet validated on paper (DEC-044).
/// </para>
/// </remarks>
public static class PngHeader
{
    /// <summary>Signature, chunk length, chunk type, width, height.</summary>
    public const int Length = 24;

    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly byte[] HeaderChunkType = "IHDR"u8.ToArray();

    /// <summary>The declared dimensions, or false when the bytes are not the start of a PNG.</summary>
    public static bool TryRead(ReadOnlySpan<byte> bytes, out int widthPx, out int heightPx)
    {
        widthPx = 0;
        heightPx = 0;

        if (bytes.Length < Length
            || !bytes[..Signature.Length].SequenceEqual(Signature)
            || !bytes.Slice(12, 4).SequenceEqual(HeaderChunkType))
        {
            return false;
        }

        // Big-endian, whatever the machine: the byte order the PNG format specifies.
        widthPx = ReadBigEndian(bytes.Slice(16, 4));
        heightPx = ReadBigEndian(bytes.Slice(20, 4));

        // The format caps both at 2^31 - 1; a value with the top bit set reads
        // as negative here and is refused with zero.
        return widthPx > 0 && heightPx > 0;
    }

    private static int ReadBigEndian(ReadOnlySpan<byte> four) =>
        (four[0] << 24) | (four[1] << 16) | (four[2] << 8) | four[3];
}
