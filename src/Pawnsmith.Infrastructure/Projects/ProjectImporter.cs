using System.IO.Compression;

using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Infrastructure.Projects;

/// <summary>A project that has just arrived from an archive.</summary>
/// <param name="Project">The project, exactly as the archive carried it.</param>
/// <param name="Directory">Where it now lives.</param>
/// <param name="Diagnostics">What does not match this machine. Never a reason to have refused.</param>
public sealed record ImportedProject(
    Project Project,
    string Directory,
    IReadOnlyList<ProjectDiagnostic> Diagnostics);

/// <summary>
/// Steps 7 and 8 of C.9.1: the only part of an import that writes anything.
/// </summary>
/// <remarks>
/// <para>
/// Everything before this has already happened, in <see cref="ArchiveInspector"/>,
/// and none of it could have written a byte. What is left is to put the files
/// somewhere — and the shape of that is what makes an import <b>atomic from the
/// user's point of view: either the project is there and complete, or there is
/// nothing.</b> Extraction goes to a temporary folder, and the destination comes
/// into existence in one operation, at the very end.
/// </para>
/// <para>
/// <b>The temporary folder is a sibling of the destination, and that is not a
/// matter of tidiness.</b> A rename is atomic only within one volume; across two
/// it degrades into a copy, which is exactly the half-written state being
/// avoided. Putting it beside the destination is what guarantees the two share a
/// volume, whatever the deployment mounts where.
/// </para>
/// <para>
/// <b>Never a merge</b> (DEC-051). A destination that already exists is refused,
/// even empty. Merging two project folders would mean answering questions nobody
/// has written down — what becomes of two blueprints with the same identifier and
/// different clauses? of two elected candidates? — and answering them here would
/// be deciding business rules in silence. The refusal is plain, and the caller
/// offers another name.
/// </para>
/// <para>
/// <b>The <c>projectId</c> is preserved, never regenerated.</b> The dominant case
/// is restoring one's own backup, where changing the identity would simply be
/// wrong. Importing the same archive twice does produce two folders with one
/// identifier, and that is a copy rather than a corruption (DEC-047); choosing
/// between "replace" and "keep both" means asking somebody, so it is T6.
/// </para>
/// </remarks>
public sealed class ProjectImporter
{
    /// <summary>Prefix of the folder an import extracts into before it commits.</summary>
    /// <remarks>
    /// The leading dot keeps it out of the way of the folder names
    /// <see cref="ProjectFolderName"/> can produce — its whitelist is
    /// <c>a-z</c>, <c>0-9</c> and <c>-</c>, so no project can ever be called
    /// this. A crash mid-extraction therefore leaves something recognisable
    /// rather than something that looks like a project.
    /// </remarks>
    public const string StagingPrefix = ".import-";

    private readonly ProjectRepositoryOptions options;
    private readonly ArchiveInspector inspector;

    public ProjectImporter(ProjectRepositoryOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
        inspector = new ArchiveInspector(options);
    }

    /// <summary>Imports <paramref name="archivePath"/> under the projects root.</summary>
    /// <param name="archivePath">The archive to read.</param>
    /// <param name="name">
    /// The display name the folder is derived from, through
    /// <see cref="ProjectFolderName"/>. Asked for rather than taken from the
    /// archive, because the name inside an archive is chosen by whoever sent it
    /// and the folder it lands in is the recipient's business (MEN-009).
    /// </param>
    /// <param name="calibration">This machine's calibration, for the diagnostics only.</param>
    /// <exception cref="ProjectException">Anything C.9.1 or C.9.2 refuses.</exception>
    public async Task<ImportedProject> ImportAsync(
        string archivePath,
        string name,
        Calibration calibration,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(archivePath);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(calibration);

        string root = Path.GetFullPath(options.ProjectsRoot);
        string destination = ResolveDestination(root, name);

        // C.9.2, and before the archive is even opened: refusing early costs
        // nothing and means a doomed import never touches the file at all.
        RequireDestinationIsFree(destination);

        // Steps 1 to 6. Nothing here can write, by construction.
        InspectedArchive inspected = inspector.Inspect(archivePath);

        Directory.CreateDirectory(root);
        string staging = Path.Combine(root, StagingPrefix + Guid.NewGuid().ToString("N"));

        try
        {
            await ExtractAsync(archivePath, inspected, staging, cancellationToken).ConfigureAwait(false);

            // Step 8, and the one instant at which the destination appears.
            // Checked again immediately before, because the inspection and the
            // extraction took time and this is the last moment at which refusing
            // still costs nothing.
            RequireDestinationIsFree(destination);
            Directory.Move(staging, destination);
        }
        catch
        {
            TryDelete(staging);
            throw;
        }

        Project project = inspected.Project.ToDomain();

        return new ImportedProject(
            project,
            destination,
            RelationalDiagnostics.For(project, calibration));
    }

    // ---- C.9.2 : la destination -------------------------------------------

    /// <summary>The folder this import will create, having checked it is a legal one.</summary>
    /// <remarks>
    /// <see cref="ProjectFolderName"/> cannot produce an escaping name — its
    /// whitelist has no separator, no dot and no colon — so the check below can
    /// never fire today. It is written anyway, and deliberately: C.3.2 asks that
    /// the resolved path be verified to sit under the root <b>before any
    /// write</b>, and a guarantee that depends on the current contents of another
    /// type's whitelist is a guarantee that disappears the day that whitelist is
    /// edited. This is the cheap half of MEN-009.
    /// </remarks>
    private static string ResolveDestination(string root, string name)
    {
        string folder = ProjectFolderName.From(name);
        string destination = Path.GetFullPath(Path.Combine(root, folder));

        if (!IsUnder(destination, root))
        {
            throw new ProjectException(
                ProjectErrorCode.PathEscape,
                $"The name '{name}' resolves to '{destination}', which is outside the projects " +
                $"root '{root}'. Nothing is ever written outside it.");
        }

        return destination;
    }

    /// <summary>Refuses a destination that exists, however empty it is.</summary>
    /// <remarks>
    /// An empty folder counts, and it has to: an import that filled it would be a
    /// merge in all but name, and the user who made that folder meant something
    /// by it. The folder is not touched.
    /// </remarks>
    private static void RequireDestinationIsFree(string destination)
    {
        if (Directory.Exists(destination) || File.Exists(destination))
        {
            throw new ProjectException(
                ProjectErrorCode.ImportDestinationExists,
                $"'{destination}' already exists. An import never merges into an existing " +
                "folder and never writes over one, even an empty one: choose another name.");
        }
    }

    // ---- Étape 7 : l'extraction, dans un dossier temporaire ---------------

    private async Task ExtractAsync(
        string archivePath,
        InspectedArchive inspected,
        string staging,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(staging);

        string stagingRoot = Path.GetFullPath(staging);
        HashSet<string> expected = new(inspected.EntryNames, StringComparer.Ordinal);

        using ZipArchive zip = ZipFile.OpenRead(archivePath);

        foreach (ZipArchiveEntry entry in zip.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // The archive is opened a second time, so this is the second time
            // these names are seen. DEC-051 asks for exactly that: the inventory
            // says what the archive claims, and what is written is checked again
            // as it is written. A file that changed in between would show up
            // here as a name nobody approved.
            // archive.json has done its work at inspection and stops there. C.3.1
            // closes a project folder to project.json, images/ and exports/, so
            // leaving it behind would put a file in the folder that the format
            // does not recognise - and that the next export would not carry,
            // since the whitelist is built from the project. A one-way
            // stowaway, and the reason a Backup round trip would not come back
            // byte for byte.
            if (string.Equals(entry.FullName, ArchiveManifestFile.EntryName, StringComparison.Ordinal))
            {
                continue;
            }

            if (!expected.Contains(entry.FullName))
            {
                throw new ProjectException(
                    ProjectErrorCode.ArchiveRejected,
                    "This archive is refused because it changed between being checked and being " +
                    "read. Nothing is imported in part.");
            }

            string target = Path.GetFullPath(Path.Combine(stagingRoot, entry.FullName));

            // The doubled check DEC-051 insists on, on the resolved path this
            // time. The inventory check is lexical and cannot see what a resolved
            // path becomes; this one cannot see a duplicate. Neither replaces the
            // other.
            if (!IsUnder(target, stagingRoot))
            {
                throw new ProjectException(
                    ProjectErrorCode.ArchiveRejected,
                    "This archive is refused because one of its entries resolves outside the " +
                    "folder being written. Nothing is imported in part.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);

            await WriteEntryAsync(entry, target, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Writes one entry into the staging folder.</summary>
    /// <remarks>
    /// <para>
    /// <b>There is deliberately no guard here against an entry that produces
    /// more bytes than it announced</b>, and the reason is a measurement rather
    /// than a hope. Every bound of C.9.3 is applied to the size the archive
    /// <i>declares</i>, which is what lets a bomb be refused before anything
    /// decompresses — but a declaration is written by the sender, so the obvious
    /// worry is an entry claiming a hundred bytes and expanding to four
    /// gigabytes.
    /// </para>
    /// <para>
    /// It cannot. The stream <see cref="ZipArchiveEntry.Open"/> returns in read
    /// mode stops at the declared length: an entry patched to announce four
    /// bytes of a five-thousand-byte payload yields exactly four and closes
    /// without complaint. A guard here would therefore have been code no test
    /// could ever reach, which is worse than no guard at all — it would read as
    /// a defence and be a decoration.
    /// </para>
    /// <para>
    /// What that same measurement does show is the opposite hazard, and it is
    /// noted rather than handled here: a <b>understated</b> size makes the file
    /// arrive truncated, silently, because the framework does not verify the
    /// CRC in that case either. It breaks no invariant of C.8 — the archive
    /// still names exactly the files it holds — and a damaged image fails
    /// loudly in the slice that reads it. Closing it would mean checking each
    /// entry's CRC32, which C.9 does not ask for.
    /// </para>
    /// </remarks>
    private static async Task WriteEntryAsync(
        ZipArchiveEntry entry,
        string target,
        CancellationToken cancellationToken)
    {
        await using Stream source = entry.Open();
        await using FileStream file = File.Create(target);

        await source.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
    }

    // ---- Outils ------------------------------------------------------------

    /// <summary>Whether <paramref name="path"/> is the root itself or sits inside it.</summary>
    /// <remarks>
    /// The trailing separator is what stops <c>/data/projects-evil</c> passing
    /// for a child of <c>/data/projects</c>. The comparison follows the platform,
    /// because case matters on Linux and does not on Windows, and pretending
    /// otherwise would refuse legitimate folders on one side or accept escapes on
    /// the other.
    /// </remarks>
    private static bool IsUnder(string path, string root)
    {
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        string trimmedRoot = root.TrimEnd(Path.DirectorySeparatorChar);
        string rootWithSeparator = trimmedRoot + Path.DirectorySeparatorChar;

        return string.Equals(path.TrimEnd(Path.DirectorySeparatorChar), trimmedRoot, comparison)
            || path.StartsWith(rootWithSeparator, comparison);
    }

    /// <summary>Removes the staging folder, without hiding the failure that led here.</summary>
    private static void TryDelete(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
            // The caller is already being handed the failure that matters, and a
            // leftover staging folder is recognisable by its name.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
