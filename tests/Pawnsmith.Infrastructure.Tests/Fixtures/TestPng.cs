using System.IO.Compression;
using System.Text;

namespace Pawnsmith.Infrastructure.Tests.Fixtures;

/// <summary>
/// Makes real, valid PNG files in code, so that no binary is committed.
/// </summary>
/// <remarks>
/// <para>
/// A complete PNG — signature, <c>IHDR</c>, one <c>IDAT</c>, <c>IEND</c>, every
/// chunk with its CRC — of a uniform grey, 8-bit RGB, not interlaced. Any image
/// viewer opens it. Nothing in T4 decodes pixels, so a header would have been
/// enough for the client; a real file costs forty lines and keeps the fixture
/// honest for whoever reuses it in T5.
/// </para>
/// <para>
/// The compression is <see cref="ZLibStream"/>, which is in the framework: the
/// PNG format's data stream is exactly a zlib stream. The CRC is written by hand,
/// since the framework only exposes it through a separate package.
/// </para>
/// </remarks>
internal static class TestPng
{
    private static readonly uint[] CrcTable = BuildCrcTable();

    /// <summary>A grey image of the given size.</summary>
    /// <param name="grey">Value of every channel of every pixel.</param>
    public static byte[] Create(int widthPx, int heightPx, byte grey = 0xC8)
    {
        using var file = new MemoryStream();
        file.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

        byte[] header =
        [
            .. BigEndian(widthPx),
            .. BigEndian(heightPx),
            8,  // bit depth
            2,  // colour type: RGB
            0,  // compression method
            0,  // filter method
            0,  // no interlace
        ];

        WriteChunk(file, "IHDR", header);
        WriteChunk(file, "IDAT", Compress(Scanlines(widthPx, heightPx, grey)));
        WriteChunk(file, "IEND", []);

        return file.ToArray();
    }

    /// <summary>A file that starts like a PNG and declares the given size, and holds nothing else.</summary>
    /// <remarks>For the bound tests: a header that lies about an enormous image costs 24 bytes.</remarks>
    public static byte[] HeaderOnly(int widthPx, int heightPx)
    {
        return
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D,
            (byte)'I', (byte)'H', (byte)'D', (byte)'R',
            .. BigEndian(widthPx),
            .. BigEndian(heightPx),
        ];
    }

    private static byte[] Scanlines(int widthPx, int heightPx, byte grey)
    {
        // Each row starts with its filter type (0, none), then three bytes per pixel.
        int rowLength = 1 + (3 * widthPx);
        byte[] raw = new byte[rowLength * heightPx];

        for (int row = 0; row < heightPx; row++)
        {
            Array.Fill(raw, grey, (row * rowLength) + 1, rowLength - 1);
        }

        return raw;
    }

    private static byte[] Compress(byte[] raw)
    {
        using var output = new MemoryStream();

        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(raw);
        }

        return output.ToArray();
    }

    private static void WriteChunk(Stream file, string type, byte[] data)
    {
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);

        file.Write(BigEndian(data.Length));
        file.Write(typeBytes);
        file.Write(data);
        file.Write(BigEndian((int)Crc([.. typeBytes, .. data])));
    }

    private static uint Crc(byte[] bytes)
    {
        uint crc = 0xFFFFFFFF;

        foreach (byte b in bytes)
        {
            crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFF;
    }

    private static uint[] BuildCrcTable()
    {
        // The CRC-32 of the PNG specification, polynomial 0xEDB88320.
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

    private static byte[] BigEndian(int value) =>
    [
        (byte)((value >> 24) & 0xFF),
        (byte)((value >> 16) & 0xFF),
        (byte)((value >> 8) & 0xFF),
        (byte)(value & 0xFF),
    ];
}
