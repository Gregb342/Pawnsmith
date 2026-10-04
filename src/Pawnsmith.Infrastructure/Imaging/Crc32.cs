namespace Pawnsmith.Infrastructure.Imaging;

/// <summary>
/// The CRC-32 of the PNG specification: polynomial 0xEDB88320, initial value
/// and final XOR 0xFFFFFFFF, over the type and the data of a chunk.
/// </summary>
/// <remarks>
/// Written by hand rather than taken from <c>System.IO.Hashing</c>, which is a
/// separate package: twenty lines against a dependency, the trade §3 of
/// CLAUDE.md asks for (DEC-099).
/// </remarks>
public static class Crc32
{
    private static readonly uint[] Table = BuildTable();

    /// <summary>The CRC of the concatenation of <paramref name="first"/> and <paramref name="second"/>.</summary>
    public static uint Compute(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
    {
        uint crc = 0xFFFFFFFF;
        crc = Update(crc, first);
        crc = Update(crc, second);

        return crc ^ 0xFFFFFFFF;
    }

    private static uint Update(uint crc, ReadOnlySpan<byte> bytes)
    {
        foreach (byte b in bytes)
        {
            crc = Table[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc;
    }

    private static uint[] BuildTable()
    {
        uint[] table = new uint[256];

        for (uint n = 0; n < 256; n++)
        {
            uint c = n;

            for (int k = 0; k < 8; k++)
            {
                c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
            }

            table[n] = c;
        }

        return table;
    }
}
