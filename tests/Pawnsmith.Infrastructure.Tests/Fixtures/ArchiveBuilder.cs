using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

using Pawnsmith.Domain.Projects;
using Pawnsmith.Infrastructure.Projects;

namespace Pawnsmith.Infrastructure.Tests.Fixtures;

/// <summary>
/// Builds archives by hand, including the ones no honest producer would write.
/// </summary>
/// <remarks>
/// <para>
/// The import tests need archives that <see cref="ProjectExporter"/> refuses to
/// produce — a <c>../../evil.txt</c>, two entries of the same name, an entry
/// claiming to be encrypted. Building them through the exporter is impossible by
/// design, so they are built here.
/// </para>
/// <para>
/// <b>No ZIP is written by hand.</b> <c>System.IO.Compression</c> stores an
/// entry name exactly as it is given — it normalises nothing on the way in, and
/// <c>FullName</c> reads back <c>../../evil.txt</c> unchanged — so hostile names
/// cost nothing but a string. The one thing it will not do is set the encryption
/// bit, and that is the only place these fixtures touch raw bytes.
/// </para>
/// </remarks>
internal sealed class ArchiveBuilder
{
    private readonly List<Entry> entries = [];

    private sealed record Entry(string Name, byte[] Content, int ExternalAttributes, bool Stored);

    /// <summary>Unix mode of a symbolic link, in the half of the attributes a ZIP keeps it in.</summary>
    public const int SymbolicLinkAttributes = 0xA1FF << 16;

    /// <summary>Adds an entry, name included, whatever that name looks like.</summary>
    /// <param name="stored">
    /// Write the bytes uncompressed. Needed by exactly one test, whose whole
    /// point is that a chosen byte sequence really appears in the file: deflate
    /// would encode it away, and the decoy would decoy nothing.
    /// </param>
    public ArchiveBuilder With(string name, byte[] content, int externalAttributes = 0, bool stored = false)
    {
        entries.Add(new Entry(name, content, externalAttributes, stored));
        return this;
    }

    /// <summary>Adds a text entry.</summary>
    public ArchiveBuilder With(string name, string content) =>
        With(name, Encoding.UTF8.GetBytes(content));

    /// <summary>Adds an entry whose bytes are written to the file as they are.</summary>
    public ArchiveBuilder WithStored(string name, byte[] content) =>
        With(name, content, externalAttributes: 0, stored: true);

    /// <summary>Removes every entry whose name matches, so a valid archive can be spoilt.</summary>
    public ArchiveBuilder Without(Func<string, bool> match)
    {
        entries.RemoveAll(entry => match(entry.Name));
        return this;
    }

    /// <summary>Writes the archive and returns its path.</summary>
    public string WriteTo(string path)
    {
        using FileStream file = File.Create(path);
        using ZipArchive zip = new(file, ZipArchiveMode.Create);

        foreach (Entry entry in entries)
        {
            ZipArchiveEntry created = zip.CreateEntry(
                entry.Name,
                entry.Stored ? CompressionLevel.NoCompression : CompressionLevel.Optimal);
            created.ExternalAttributes = entry.ExternalAttributes;

            using Stream stream = created.Open();
            stream.Write(entry.Content);
        }

        return path;
    }

    // ---- Les archives de départ -------------------------------------------

    /// <summary>An archive that passes every check, to be spoilt one rule at a time.</summary>
    /// <remarks>
    /// Starting from a sound archive and breaking exactly one thing is what makes
    /// a refusal mean something: a test whose archive is wrong in three ways
    /// proves only that it was refused, not what refused it.
    /// </remarks>
    public static ArchiveBuilder Valid(Project project, ArchiveProfile profile = ArchiveProfile.Backup)
    {
        ProjectDocument document = project.ToDocument();
        ArchiveBuilder builder = new ArchiveBuilder()
            .With(ArchiveManifestFile.EntryName, Manifest(profile))
            .With(ProjectFileWriter.FileName, ProjectJson.Serialize(document));

        foreach (string relative in ShareFilter.ReferencedImages(document))
        {
            builder.With(relative, Png());
        }

        return builder;
    }

    /// <summary>The bytes of an <c>archive.json</c>, with a version that can be wrong on purpose.</summary>
    public static byte[] Manifest(ArchiveProfile profile = ArchiveProfile.Backup, int? archiveVersion = null)
    {
        ArchiveManifest manifest = ArchiveManifestFile.For(profile, DateTimeOffset.UnixEpoch);

        return ArchiveManifestFile.Serialize(
            archiveVersion is { } version ? manifest with { ArchiveVersion = version } : manifest);
    }

    /// <summary>Four bytes that are enough to stand in for a PNG here.</summary>
    /// <remarks>
    /// Nothing in the import decodes an image — MEN-005 bounds that, in the
    /// slice that reads pixels. What matters here is that the file exists, is
    /// named, and does not compress suspiciously well.
    /// </remarks>
    public static byte[] Png() => [0x89, 0x50, 0x4E, 0x47];

    // ---- Le seul endroit qui touche aux octets ----------------------------

    /// <summary>
    /// Sets the "this entry is encrypted" bit on every entry of an existing
    /// archive.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The content is left in clear, because what is being tested is the
    /// <b>refusal</b>, and the refusal is meant to happen without decompressing
    /// anything. An archive that were genuinely encrypted would test the same
    /// rule and be far harder to build.
    /// </para>
    /// <para>
    /// The central directory is walked properly rather than scanned for the
    /// signature: four bytes that look like a header can perfectly well occur
    /// inside compressed data, and a fixture that corrupted the archive would
    /// make the test pass for the wrong reason.
    /// </para>
    /// </remarks>
    public static void MarkEveryEntryEncrypted(string archivePath)
    {
        byte[] bytes = File.ReadAllBytes(archivePath);

        // No archive comment is written, so the end-of-central-directory record
        // is exactly the last 22 bytes.
        const int endLength = 22;
        int end = bytes.Length - endLength;

        int count = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(end + 10));
        int offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(end + 16));

        for (int index = 0; index < count; index++)
        {
            // Offset 8 of a central directory record is the general purpose bit
            // flag; bit 0 of it means encrypted.
            bytes[offset + 8] |= 0x01;

            int nameLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 28));
            int extraLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 30));
            int commentLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 32));

            offset += 46 + nameLength + extraLength + commentLength;
        }

        File.WriteAllBytes(archivePath, bytes);
    }
    /// <summary>
    /// Damages the compressed bytes of one entry, so that reading it fails
    /// partway through.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is what test 51 needs: an archive that passes every check made
    /// before extraction and then fails during it, with real files already
    /// written beside the broken one. Nothing else available produces that.
    /// Understating a declared size does not — the framework simply truncates
    /// the stream at whatever was declared, without a word — and an
    /// already-cancelled token fails before the first file rather than among
    /// them.
    /// </para>
    /// <para>
    /// The entry has to be one the inspection does not read, or the archive
    /// would be refused before extraction ever started, and it has to be one
    /// that really was deflated, since damaging a stored block changes bytes
    /// without breaking anything. An image built by
    /// <see cref="Compressible"/> is both.
    /// </para>
    /// </remarks>
    public static void CorruptEntryData(string archivePath, string entryName)
    {
        byte[] bytes = File.ReadAllBytes(archivePath);

        const int endLength = 22;
        int end = bytes.Length - endLength;

        int count = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(end + 10));
        int offset = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(end + 16));

        for (int index = 0; index < count; index++)
        {
            int nameLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 28));
            int extraLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 30));
            int commentLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(offset + 32));

            if (string.Equals(Encoding.UTF8.GetString(bytes, offset + 46, nameLength), entryName, StringComparison.Ordinal))
            {
                // Offset 42 of a central directory record points at the entry's
                // local header; the data begins after that header's 30 fixed
                // bytes and its own name and extra fields, whose lengths sit at
                // offsets 26 and 28 of it.
                int local = (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(offset + 42));
                int localName = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(local + 26));
                int localExtra = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(local + 28));
                int data = local + 30 + localName + localExtra;

                // Well past the start of the deflate stream, so that a first
                // block decodes and the failure lands in the middle rather than
                // on the first byte read.
                for (int position = data + 24; position < data + 40; position++)
                {
                    bytes[position] ^= 0xFF;
                }

                File.WriteAllBytes(archivePath, bytes);
                return;
            }

            offset += 46 + nameLength + extraLength + commentLength;
        }

        throw new InvalidOperationException($"No entry called '{entryName}' in '{archivePath}'.");
    }

    /// <summary>
    /// Content that deflate really compresses, but nowhere near the bound of
    /// C.9.3.
    /// </summary>
    /// <remarks>
    /// The two requirements pull against each other, which is why this is a
    /// method and not a literal. It has to compress, or there is no deflate
    /// stream to damage; and it has to stay under 100:1, or the bomb check
    /// refuses the archive before any of that matters. Repeated prose lands at
    /// roughly 20:1.
    /// </remarks>
    public static byte[] Compressible()
    {
        const string sentence =
            "a goblin skirmisher wielding a short spear held vertically against the body, " +
            "wearing leather scraps, one ear torn, a bone fetish tied to the belt; ";

        return Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat(sentence, 40)));
    }
}
