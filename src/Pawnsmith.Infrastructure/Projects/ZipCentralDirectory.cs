using System.Text;

namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// Reads the one field of the ZIP format that <c>System.IO.Compression</c> does
/// not expose: whether an entry declares itself encrypted.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this file exists at all.</b> C.9.1 step 4 requires an encrypted entry
/// to fail the archive globally, before anything is extracted. The framework
/// offers no way to ask: <see cref="System.IO.Compression.ZipArchiveEntry"/> has
/// no flag property, and — this is the part that decides the matter —
/// <c>ZipArchive</c> <b>never looks at the bit either</b>. Setting it on every
/// header of a real archive and reopening it produces no error of any kind: the
/// entries open, and their bytes are handed over as if nothing were wrong. So
/// without this reader an encrypted archive would not be refused, it would be
/// extracted into a project folder full of ciphertext, silently. That is a worse
/// outcome than any refusal.
/// </para>
/// <para>
/// <b>Why the central directory and not the local headers.</b> Each local file
/// header carries the same flag, and they sit in a row from the start of the
/// file, which sounds easier to walk. It is not: when bit 3 of the flag is set
/// the header's size fields are zero and the real sizes follow the data, so
/// there is no way to know where the next header begins. The central directory
/// is the index the format provides precisely so this is not necessary.
/// </para>
/// <para>
/// <b>What this is not.</b> It is not a ZIP reader, and nothing else may grow
/// here. It reads one 2-byte field at a documented offset in a fixed-size
/// record, and a name to put in a log message. Everything else about the archive
/// keeps coming from the framework.
/// </para>
/// </remarks>
internal static class ZipCentralDirectory
{
    private const uint EndOfCentralDirectorySignature = 0x06054b50;
    private const uint Zip64LocatorSignature = 0x07064b50;
    private const uint Zip64EndOfCentralDirectorySignature = 0x06064b50;
    private const uint CentralDirectoryHeaderSignature = 0x02014b50;

    /// <summary>Bytes of a central directory record before the file name.</summary>
    private const int CentralDirectoryHeaderLength = 46;

    /// <summary>Bit 0 of the general purpose bit flag: the entry is encrypted.</summary>
    private const ushort EncryptedFlag = 0x0001;

    /// <summary>
    /// Largest end-of-directory record there can be: 22 fixed bytes plus a
    /// comment whose length is held in two bytes.
    /// </summary>
    private const int MaxEndOfCentralDirectoryLength = 22 + ushort.MaxValue;

    /// <summary>The names of the entries that declare themselves encrypted.</summary>
    /// <param name="archivePath">The archive to look at. Opened read-only and closed again.</param>
    /// <param name="maxEntries">
    /// How many records to walk at most. The resource bound of C.9.3 has already
    /// been applied to the entry count by the time this runs, and passing it in
    /// rather than inventing a second ceiling keeps DEC-057 true: no bound is
    /// written as a literal in the code that applies it.
    /// </param>
    /// <exception cref="InvalidDataException">The directory cannot be read as one.</exception>
    public static IReadOnlyList<string> EncryptedEntryNames(string archivePath, int maxEntries)
    {
        ArgumentException.ThrowIfNullOrEmpty(archivePath);

        using FileStream stream = new(
            archivePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read);

        using BinaryReader reader = new(stream, Encoding.UTF8, leaveOpen: true);

        (long offset, long count) = FindCentralDirectory(stream, reader);

        List<string> encrypted = [];

        stream.Position = offset;

        for (long index = 0; index < count && index < maxEntries; index++)
        {
            if (reader.ReadUInt32() != CentralDirectoryHeaderSignature)
            {
                throw new InvalidDataException(
                    $"Central directory record {index} does not start with the expected signature.");
            }

            // Offsets 4 and 6 are the two version fields; the flag is at 8.
            stream.Position += 4;
            ushort flag = reader.ReadUInt16();

            // From the flag to the name length is offset 10 to offset 28.
            stream.Position += 18;
            ushort nameLength = reader.ReadUInt16();
            ushort extraLength = reader.ReadUInt16();
            ushort commentLength = reader.ReadUInt16();

            // Offsets 34 to 45: disk number, the two attribute fields and the
            // local header offset. None of them is read here.
            stream.Position += CentralDirectoryHeaderLength - 34;

            byte[] nameBytes = reader.ReadBytes(nameLength);

            if (nameBytes.Length != nameLength)
            {
                throw new InvalidDataException("The central directory ends in the middle of a name.");
            }

            stream.Position += extraLength + commentLength;

            if ((flag & EncryptedFlag) != 0)
            {
                // Decoded as UTF-8 because that is what our own writer produces
                // and what bit 11 of the flag declares in any modern archive. A
                // name this fails to decode still reaches the log as replacement
                // characters, which is enough: this string is only ever a log
                // message, never a path.
                encrypted.Add(Encoding.UTF8.GetString(nameBytes));
            }
        }

        return encrypted;
    }

    /// <summary>Where the central directory starts, and how many records it holds.</summary>
    /// <remarks>
    /// The end-of-directory record sits at the very end of the file, after a
    /// comment of up to 64 KiB, so it is found by scanning backwards rather than
    /// by seeking to a known place. When it carries the two saturated values that
    /// mean "this archive is too big for 32-bit fields", the real numbers come
    /// from the Zip64 record the locator points at.
    /// </remarks>
    private static (long Offset, long Count) FindCentralDirectory(Stream stream, BinaryReader reader)
    {
        long tailLength = Math.Min(stream.Length, MaxEndOfCentralDirectoryLength);
        stream.Position = stream.Length - tailLength;

        byte[] tail = reader.ReadBytes((int)tailLength);
        int end = LastIndexOfSignature(tail, EndOfCentralDirectorySignature);

        if (end < 0)
        {
            throw new InvalidDataException("No end-of-central-directory record: this is not a ZIP file.");
        }

        long endPosition = stream.Length - tailLength + end;

        stream.Position = endPosition + 10;
        ushort count = reader.ReadUInt16();

        stream.Position = endPosition + 16;
        uint offset = reader.ReadUInt32();

        if (count != ushort.MaxValue && offset != uint.MaxValue)
        {
            return (offset, count);
        }

        // Zip64. The locator sits immediately before the record just found, and
        // points at the record holding the real 64-bit values. Refusing instead
        // would refuse a legitimate large Backup, which the 4 GiB bound of C.9.3
        // explicitly allows.
        int locator = LastIndexOfSignature(tail, Zip64LocatorSignature);

        if (locator < 0)
        {
            throw new InvalidDataException("A Zip64 archive with no Zip64 locator.");
        }

        stream.Position = stream.Length - tailLength + locator + 8;
        long zip64Position = reader.ReadInt64();

        if (zip64Position < 0 || zip64Position + 56 > stream.Length)
        {
            throw new InvalidDataException("The Zip64 locator points outside the file.");
        }

        stream.Position = zip64Position;

        if (reader.ReadUInt32() != Zip64EndOfCentralDirectorySignature)
        {
            throw new InvalidDataException("The Zip64 locator does not point at a Zip64 record.");
        }

        stream.Position = zip64Position + 32;
        long zip64Count = reader.ReadInt64();

        stream.Position = zip64Position + 48;
        long zip64Offset = reader.ReadInt64();

        return (zip64Offset, zip64Count);
    }

    /// <summary>The last place <paramref name="signature"/> appears in <paramref name="buffer"/>.</summary>
    /// <remarks>
    /// Backwards, because the four bytes of a signature can perfectly well occur
    /// inside compressed data or inside the archive comment; the one that counts
    /// is the last.
    /// </remarks>
    private static int LastIndexOfSignature(byte[] buffer, uint signature)
    {
        byte[] pattern =
        [
            (byte)(signature & 0xFF),
            (byte)((signature >> 8) & 0xFF),
            (byte)((signature >> 16) & 0xFF),
            (byte)((signature >> 24) & 0xFF),
        ];

        for (int index = buffer.Length - pattern.Length; index >= 0; index--)
        {
            if (buffer[index] == pattern[0]
                && buffer[index + 1] == pattern[1]
                && buffer[index + 2] == pattern[2]
                && buffer[index + 3] == pattern[3])
            {
                return index;
            }
        }

        return -1;
    }
}
