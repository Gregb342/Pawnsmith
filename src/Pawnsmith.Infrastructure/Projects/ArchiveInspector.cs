using System.IO.Compression;
using System.Text.Json;

namespace Pawnsmith.Infrastructure.Projects;

/// <summary>What an archive turned out to hold, once it was found acceptable.</summary>
/// <param name="Manifest">The <c>archive.json</c> that opened it.</param>
/// <param name="Project">The <c>project.json</c> it carries, read in memory and already validated.</param>
/// <param name="EntryNames">Every entry, in the order the archive lists them. All of them extractable.</param>
public sealed record InspectedArchive(
    ArchiveManifest Manifest,
    ProjectDocument Project,
    IReadOnlyList<string> EntryNames);

/// <summary>
/// Steps 1 to 6 of C.9.1: everything an import checks <b>before</b> a single
/// byte is written.
/// </summary>
/// <remarks>
/// <para>
/// <b>This type takes no destination and holds no way to write.</b> That is not
/// an accident of the current implementation, it is the point of separating it
/// from the extraction. MEN-001 asks that an archive be validated before
/// anything is written; here that is not a rule to keep, it is a shape — there
/// is no file to create, no folder to make, no path to join. "Nothing was
/// written" needs no assertion when nothing could have been.
/// </para>
/// <para>
/// The order of the checks is fixed by C.9.1 and is not rearrangeable. Bounds
/// first, because everything after them reads something and a bound applied
/// afterwards protects nothing. Then the manifest, which says whether this is
/// even an archive of ours. Then the inventory, which is what MEN-001 is
/// actually about. Then the project file, held in memory and put through exactly
/// the checks a load applies. Then internal consistency, which is the invariant
/// DEC-050 gave the export: <b>no dangling reference, in either direction.</b>
/// </para>
/// <para>
/// <b>Two codes, and the difference is deliberate.</b> A resource bound refuses
/// with <c>ARCHIVE_LIMIT_EXCEEDED</c> so the cause is legible — an archive that
/// is merely too big is not an attack, and telling its owner so costs nothing.
/// Everything else refuses with <c>ARCHIVE_REJECTED</c> and says no more,
/// because naming the rule that caught it is information useful to whoever is
/// building the malicious archive and to nobody else (C.11). The project codes
/// pass through unchanged: a <c>project.json</c> inside an archive is still a
/// project file, and <c>PROJECT_SCHEMA_TOO_RECENT</c> is what C.9.2 asks for by
/// name.
/// </para>
/// </remarks>
public sealed class ArchiveInspector
{
    private readonly ProjectRepositoryOptions options;

    public ArchiveInspector(ProjectRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
    }

    /// <summary>Reads <paramref name="archivePath"/> without extracting any of it.</summary>
    /// <remarks>
    /// The archive is opened, read and closed. Nothing keeps a handle on it, so
    /// the extraction of C.9.1 step 7 opens it again. That second read is what
    /// DEC-051 calls the doubled check: the inventory says what the archive
    /// claims, and the extractor verifies every resolved path as it writes it.
    /// A file that changed between the two would defeat that, which is why T2
    /// says plainly there is no concurrent caller before T6.
    /// </remarks>
    /// <exception cref="ProjectException">Any rule of C.9.1 steps 2 to 6, or C.9.3.</exception>
    public InspectedArchive Inspect(string archivePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(archivePath);

        if (!File.Exists(archivePath))
        {
            throw new ProjectException(
                ProjectErrorCode.NotFound,
                $"There is no archive at '{archivePath}'.");
        }

        // Étape 1 : ouvrir en lecture, sans rien extraire.
        using ZipArchive zip = Open(archivePath);

        IReadOnlyList<ZipArchiveEntry> entries = ReadInventory(zip);

        CheckResourceBounds(entries);
        ArchiveManifest manifest = ReadManifest(zip);
        CheckInventory(entries, archivePath);

        ProjectDocument project = ReadProject(zip);
        CheckInternalConsistency(project, entries);

        return new InspectedArchive(
            manifest,
            project,
            [.. entries.Select(entry => entry.FullName)]);
    }

    private static ZipArchive Open(string archivePath)
    {
        try
        {
            return ZipFile.OpenRead(archivePath);
        }
        catch (InvalidDataException error)
        {
            throw Reject("it cannot be read as a ZIP file", error);
        }
    }

    /// <summary>The entry list, which is where reading a ZIP begins.</summary>
    /// <remarks>
    /// A limitation worth stating rather than hiding: this materialises the whole
    /// central directory, so the entry-count bound of C.9.3 is applied
    /// <i>after</i> the library has already read it. The framework offers no
    /// lazy enumeration, and writing one would mean the ZIP reader this code
    /// deliberately does not contain. What it costs is bounded by the size of
    /// the archive file on disk, never by anything the archive claims about
    /// itself — so a small file cannot make this allocate a large amount.
    /// </remarks>
    private static IReadOnlyList<ZipArchiveEntry> ReadInventory(ZipArchive zip)
    {
        try
        {
            return zip.Entries;
        }
        catch (InvalidDataException error)
        {
            throw Reject("its central directory cannot be read", error);
        }
    }

    // ---- Étape 2 : les bornes de ressources (C.9.3) -----------------------

    private void CheckResourceBounds(IReadOnlyList<ZipArchiveEntry> entries)
    {
        if (entries.Count > options.MaxArchiveEntryCount)
        {
            throw Exceeded(
                $"it holds {entries.Count} entries, past the bound of {options.MaxArchiveEntryCount}");
        }

        long uncompressed = 0;
        long compressed = 0;

        foreach (ZipArchiveEntry entry in entries)
        {
            // Declared sizes, read from the central directory. That is the whole
            // point: a decompression bomb is refused on what it announces, long
            // before anything decompresses it.
            uncompressed += entry.Length;
            compressed += entry.CompressedLength;

            if (uncompressed > options.MaxArchiveUncompressedBytes)
            {
                throw Exceeded(
                    $"it expands to more than {options.MaxArchiveUncompressedBytes} bytes");
            }

            if (ExceedsRatio(entry.Length, entry.CompressedLength))
            {
                throw Exceeded(
                    $"the entry '{entry.FullName}' expands {entry.Length} bytes from " +
                    $"{entry.CompressedLength}, past the {options.MaxArchiveCompressionRatio}:1 bound");
            }

            if (entry.FullName.Length > options.MaxArchivePathLength)
            {
                throw Exceeded(
                    $"an entry name is {entry.FullName.Length} characters, past the bound of " +
                    $"{options.MaxArchivePathLength}");
            }

            if (ArchiveEntryRules.Depth(entry.FullName) > options.MaxArchivePathDepth)
            {
                throw Exceeded(
                    $"the entry '{entry.FullName}' is deeper than {options.MaxArchivePathDepth} segments");
            }
        }

        if (ExceedsRatio(uncompressed, compressed))
        {
            throw Exceeded(
                $"it expands {uncompressed} bytes from {compressed}, past the " +
                $"{options.MaxArchiveCompressionRatio}:1 bound");
        }
    }

    /// <summary>Whether these two sizes are a ratio the bound refuses.</summary>
    /// <remarks>
    /// An entry that compresses to nothing at all is judged only on whether it
    /// expands to anything: dividing by zero would be the alternative, and an
    /// empty entry is not a bomb.
    /// </remarks>
    private bool ExceedsRatio(long uncompressed, long compressed)
    {
        if (uncompressed == 0)
        {
            return false;
        }

        return compressed == 0 || uncompressed > compressed * options.MaxArchiveCompressionRatio;
    }

    // ---- Étape 3 : archive.json -------------------------------------------

    private static ArchiveManifest ReadManifest(ZipArchive zip)
    {
        ZipArchiveEntry entry = zip.GetEntry(ArchiveManifestFile.EntryName)
            ?? throw Reject($"it holds no '{ArchiveManifestFile.EntryName}'");

        ArchiveManifest manifest;

        try
        {
            manifest = ArchiveManifestFile.Deserialize(Read(entry));
        }
        catch (JsonException error)
        {
            throw Reject($"its '{ArchiveManifestFile.EntryName}' cannot be read", error);
        }

        if (manifest.ArchiveVersion != ArchiveManifestFile.SupportedArchiveVersion)
        {
            throw Reject(
                $"it declares archive version {manifest.ArchiveVersion} and this build reads " +
                $"version {ArchiveManifestFile.SupportedArchiveVersion}");
        }

        return manifest;
    }

    // Nothing requires archive.json to be the *first* entry, although C.8.5 makes
    // ours one. Being first is what lets a reader find it without walking the
    // archive; it is a courtesy of the writer, not a property the format
    // guarantees, and refusing an otherwise sound archive over the order of its
    // entries would buy nothing.

    // ---- Étape 4 : l'inventaire, et le rejet global ------------------------

    /// <summary>Walks the entry list, refusing the whole archive on the first thing wrong.</summary>
    /// <remarks>
    /// <b>These rules overlap, and the overlap is kept rather than trimmed.</b>
    /// On today's whitelist the narrow gate catches almost everything the other
    /// rules catch — <c>../../evil.txt</c> is not a whitelisted name any more
    /// than it is a safe relative path — so each rule taken alone looks
    /// redundant. Two reasons not to remove any of them. They differ in
    /// <b>message</b>, and a log that says "not one an archive may hold" about a
    /// path traversal sends its reader looking for the wrong thing. And they
    /// differ in <b>lifespan</b>: the whitelist is a list that will grow, while
    /// "no <c>..</c> segment" is true forever. A defence that only holds because
    /// of the current shape of another one is a defence that disappears the day
    /// that other one is edited.
    /// </remarks>
    private void CheckInventory(IReadOnlyList<ZipArchiveEntry> entries, string archivePath)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        HashSet<string> seenIgnoringCase = new(StringComparer.OrdinalIgnoreCase);

        foreach (ZipArchiveEntry entry in entries)
        {
            string name = entry.FullName;

            // What sort of entry it is comes first, because it is the most
            // specific answer available: a folder entry and a symbolic link both
            // have names that would fail a later rule too, and being told "not a
            // plain relative path" about "images/" would send the reader looking
            // for a path problem that is not the one there is.
            if (!IsOrdinaryFile(entry))
            {
                throw Reject($"the entry '{name}' is not an ordinary file");
            }

            if (!ArchiveEntryRules.IsSafeRelativePath(name))
            {
                throw Reject($"the entry '{name}' is not a plain relative path");
            }

            if (!ArchiveEntryRules.IsWhitelisted(name))
            {
                throw Reject($"the entry '{name}' is not one an archive may hold");
            }

            // Duplicates first, then case. The duplicate is the classic bypass —
            // the validator inspects the first entry and the extractor writes the
            // second. The case collision is harmless on Linux and an overwrite on
            // Windows and macOS, which is reason enough to refuse it everywhere:
            // an archive must not mean two different things on two machines.
            if (!seen.Add(name))
            {
                throw Reject($"the entry '{name}' appears twice");
            }

            if (!seenIgnoringCase.Add(name))
            {
                throw Reject($"the entry '{name}' collides with another that differs only in case");
            }
        }

        CheckNothingIsEncrypted(archivePath);
    }

    /// <summary>
    /// Whether the entry is a plain file rather than a folder, a link or a
    /// device.
    /// </summary>
    /// <remarks>
    /// A name ending in a separator is a folder entry, which carries no content
    /// and which nothing here needs: the extraction creates the folders it needs
    /// from the paths of the files.
    /// <para>
    /// Everything else is read from the Unix mode a ZIP keeps in the high half of
    /// the external attributes. A symbolic link is <c>S_IFLNK</c>, and it matters
    /// here for the same reason MEN-008 matters on the way out: a link in an
    /// archive is a file that points somewhere it was never given permission to
    /// point. Archives written on Windows leave that half at zero and carry DOS
    /// attributes instead, so an absent mode is not treated as a refusal — and
    /// neither is a mode that carries permissions with no type bits, which some
    /// writers produce.
    /// </para>
    /// </remarks>
    private static bool IsOrdinaryFile(ZipArchiveEntry entry)
    {
        if (entry.FullName.EndsWith(ArchiveEntryRules.Separator))
        {
            return false;
        }

        const int fileTypeMask = 0xF000;
        const int regularFile = 0x8000;

        int fileType = ((entry.ExternalAttributes >> 16) & 0xFFFF) & fileTypeMask;

        return fileType is 0 or regularFile;
    }

    /// <summary>Refuses an archive any of whose entries declares itself encrypted.</summary>
    /// <remarks>
    /// The one check the framework cannot answer, and the one it silently gets
    /// wrong — see <see cref="ZipCentralDirectory"/> for why this costs a read of
    /// the central directory by hand.
    /// </remarks>
    private void CheckNothingIsEncrypted(string archivePath)
    {
        IReadOnlyList<string> encrypted;

        try
        {
            encrypted = ZipCentralDirectory.EncryptedEntryNames(
                archivePath,
                options.MaxArchiveEntryCount);
        }
        catch (InvalidDataException error)
        {
            throw Reject("its central directory cannot be read", error);
        }

        if (encrypted.Count > 0)
        {
            throw Reject($"the entry '{encrypted[0]}' is encrypted");
        }
    }

    // ---- Étape 5 : project.json, en mémoire --------------------------------

    private ProjectDocument ReadProject(ZipArchive zip)
    {
        ZipArchiveEntry entry = zip.GetEntry(ProjectFileWriter.FileName)
            ?? throw Reject($"it holds no '{ProjectFileWriter.FileName}'");

        if (entry.Length > options.MaxProjectFileBytes)
        {
            throw new ProjectException(
                ProjectErrorCode.TooLarge,
                $"The '{ProjectFileWriter.FileName}' in this archive announces {entry.Length} bytes, " +
                $"past the {options.MaxProjectFileBytes} byte bound. It is read in memory, so the " +
                "bound comes first.");
        }

        ProjectDocument document;

        try
        {
            document = ProjectJson.Deserialize(Read(entry));
        }
        catch (JsonException error)
        {
            // PROJECT_INVALID rather than ARCHIVE_REJECTED, and the message names
            // the field. The reticence of ARCHIVE_REJECTED is about the shape of
            // the archive, which an attacker controls entirely; a malformed
            // project inside an otherwise well-formed archive is nearly always
            // one's own, and hiding which field broke would help nobody.
            throw new ProjectException(
                ProjectErrorCode.Invalid,
                $"The project file in this archive cannot be read: {error.Message}",
                error);
        }

        // Steps 3 to 7 of C.7.1, exactly as a load applies them: schema version,
        // structure, image paths, intrinsic overrides. Nothing relational, so no
        // calibration is needed and none is asked for - an archive built on
        // another machine's calibration imports (DEC-056).
        ProjectValidation.Validate(document);

        return document;
    }

    // ---- Étape 6 : la cohérence interne ------------------------------------

    /// <summary>
    /// Checks the archive and its project file agree, in both directions.
    /// </summary>
    /// <remarks>
    /// This is the invariant of DEC-050 read from the receiving end: <b>an
    /// archive always holds a <c>project.json</c> consistent with the files it
    /// contains.</b> A referenced file that is absent would produce a project
    /// with holes, which is the half-broken state the <c>Share</c> filter exists
    /// to avoid creating. A present file nobody references is the other half, and
    /// refusing it is what stops an archive being used to drop a chosen file into
    /// a project folder — the whitelist already says it must be a <c>.png</c>
    /// under <c>images/</c>, and this says it must be one somebody asked for.
    /// </remarks>
    private static void CheckInternalConsistency(
        ProjectDocument project,
        IReadOnlyList<ZipArchiveEntry> entries)
    {
        HashSet<string> present = new(
            entries.Select(entry => entry.FullName),
            StringComparer.Ordinal);

        IReadOnlyList<string> referenced = ShareFilter.ReferencedImages(project);

        foreach (string relative in referenced)
        {
            if (!present.Contains(relative))
            {
                throw Reject($"its project refers to '{relative}', which the archive does not hold");
            }
        }

        HashSet<string> wanted = new(referenced, StringComparer.Ordinal);

        foreach (string name in present.Where(ArchiveEntryRules.IsImage))
        {
            if (!wanted.Contains(name))
            {
                throw Reject($"it holds '{name}', which its project does not refer to");
            }
        }
    }

    // ---- Outils ------------------------------------------------------------

    private static byte[] Read(ZipArchiveEntry entry)
    {
        try
        {
            using Stream stream = entry.Open();
            using MemoryStream buffer = new();
            stream.CopyTo(buffer);

            return buffer.ToArray();
        }
        catch (InvalidDataException error)
        {
            throw Reject($"the entry '{entry.FullName}' cannot be decompressed", error);
        }
    }

    /// <summary>
    /// The one refusal an archive gets, whatever it did.
    /// </summary>
    /// <remarks>
    /// The <paramref name="why"/> goes into the message, which is what a log and
    /// a command line show (chapter 8). The <b>code</b> stays
    /// <c>ARCHIVE_REJECTED</c> and says nothing, because that is what an API
    /// would hand back to whoever sent the archive.
    /// </remarks>
    private static ProjectException Reject(string why, Exception? inner = null)
    {
        string message = $"This archive is refused because {why}. " +
            "An archive is accepted whole or refused whole; nothing is ever imported in part.";

        return inner is null
            ? new ProjectException(ProjectErrorCode.ArchiveRejected, message)
            : new ProjectException(ProjectErrorCode.ArchiveRejected, message, inner);
    }

    private ProjectException Exceeded(string why) =>
        new(ProjectErrorCode.ArchiveLimitExceeded,
            $"This archive is refused because {why}. The bounds exist because an archive is a " +
            "second decompression surface, which MEN-005 bounded for images and nothing bounded " +
            "for archives.");
}
