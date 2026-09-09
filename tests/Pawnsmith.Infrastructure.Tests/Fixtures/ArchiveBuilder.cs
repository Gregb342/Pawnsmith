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
}
