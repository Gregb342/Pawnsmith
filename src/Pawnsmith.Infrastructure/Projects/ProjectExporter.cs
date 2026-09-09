using System.Globalization;
using System.IO.Compression;

using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// Writes a project into a ZIP archive, listing what is allowed in rather than
/// zipping a folder.
/// </summary>
/// <remarks>
/// <para>
/// <b>An archive is never "the folder zipped".</b> It is built by enumerating
/// what the whitelist of C.8.2 authorises, and that turns MEN-006 from good
/// practice into a structural property. Zipping the folder would carry off
/// whatever the user has dropped in it: a stray <c>.env</c>, a <c>notes.txt</c>,
/// a whole <c>.git/</c> with its history, a <c>logs/</c> made by hand despite
/// DEC-022. The test changes nature with the code — it no longer looks for a
/// named secret, it checks that <b>no entry outside the list</b> is present.
/// </para>
/// <para>
/// Everything is verified before a single byte is written, and the archive is
/// staged in a temporary file: a refused export leaves nothing at all behind,
/// which is what test 37 asks for.
/// </para>
/// </remarks>
public sealed class ProjectExporter
{
    private readonly TimeProvider clock;

    public ProjectExporter(TimeProvider? clock = null)
    {
        this.clock = clock ?? TimeProvider.System;
    }

    /// <summary>Exports <paramref name="projectDirectory"/> into a new archive.</summary>
    /// <param name="projectDirectory">The project folder to export.</param>
    /// <param name="profile">What the archive is for, which decides what goes in.</param>
    /// <param name="destinationDirectory">Where to write the archive. Never inside a project.</param>
    /// <returns>The full path of the archive written.</returns>
    /// <exception cref="ProjectException">Anything C.8.4 refuses.</exception>
    public async Task<string> ExportAsync(
        string projectDirectory,
        ArchiveProfile profile,
        string destinationDirectory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectDirectory);
        ArgumentException.ThrowIfNullOrEmpty(destinationDirectory);

        string source = Path.GetFullPath(projectDirectory);
        string destination = Path.GetFullPath(destinationDirectory);

        RequireDestinationOutsideAnyProject(destination);
        RequireNoSymbolicLink(source);

        ProjectDocument document = ReadDocument(source);
        ProjectDocument archived = profile == ArchiveProfile.Share
            ? ShareFilter.Apply(document)
            : document;

        IReadOnlyList<string> images = RequireEveryReferencedImage(source, archived);
        IReadOnlyList<string> exports = profile == ArchiveProfile.Backup ? ListExports(source) : [];

        Directory.CreateDirectory(destination);

        string archivePath = Path.Combine(
            destination,
            ArchiveName(Path.GetFileName(source), profile));

        // Staged, then moved: a failure halfway through leaves no archive at
        // all rather than a truncated one somebody might later try to import.
        string staging = archivePath + ".tmp";

        try
        {
            await WriteArchiveAsync(staging, profile, archived, source, images, exports, cancellationToken)
                .ConfigureAwait(false);

            File.Move(staging, archivePath, overwrite: true);
        }
        catch
        {
            TryDelete(staging);
            throw;
        }

        return archivePath;
    }

    /// <summary>
    /// The file name of an archive, as C.8.5 proposes it.
    /// </summary>
    /// <remarks>
    /// The name is a convenience and carries nothing the archive does not
    /// already hold — <c>archive.json</c> is what says the profile, and
    /// <c>project.json</c> is what says the project. Renaming an archive
    /// therefore breaks nothing.
    /// </remarks>
    private string ArchiveName(string folderName, ArchiveProfile profile)
    {
        string stamp = clock.GetUtcNow().ToString("yyyyMMddHHmm", CultureInfo.InvariantCulture);

        return $"{folderName}-{profile.ToString().ToLowerInvariant()}-{stamp}.zip";
    }

    // ---- Ce que C.8.4 refuse ----------------------------------------------

    /// <summary>Refuses to write an archive inside a project folder.</summary>
    /// <remarks>
    /// Otherwise the next backup would contain the previous one, then both, and
    /// so on. The whole chain of parents is checked rather than the destination
    /// alone, because <c>mon-projet/exports/sauvegardes</c> is just as much
    /// inside a project as <c>mon-projet</c> is.
    /// </remarks>
    private static void RequireDestinationOutsideAnyProject(string destination)
    {
        for (DirectoryInfo? folder = new(destination); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, ProjectFileWriter.FileName)))
            {
                throw new ProjectException(
                    ProjectErrorCode.ArchiveExportFailed,
                    $"'{destination}' is inside the project folder '{folder.FullName}'. " +
                    "Writing an archive there would make the next backup contain the previous one.");
            }
        }
    }

    /// <summary>
    /// Refuses a symbolic link anywhere in the project folder (MEN-008).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the exact mirror of MEN-001. A zip slip makes a file <i>enter</i>
    /// where it should not, on import; a symbolic link makes a file <i>leave</i>
    /// where it should stay, on export — towards the log volume DEC-022 took
    /// care to isolate, towards a home folder, towards <c>/etc</c>. The vector
    /// is all the more effective because the victim sends the archive herself,
    /// in good faith, since MEN-006 promised her a project is shareable without
    /// a second thought.
    /// </para>
    /// <para>
    /// <b>The export fails, naming the link; it does not skip it.</b> Skipping
    /// would hand back an archive that is quietly incomplete, and the omission
    /// would only be discovered on restoring it.
    /// </para>
    /// <para>
    /// Every entry is checked, not only the whitelisted ones: a link is refused
    /// wherever it sits, because its presence says something about the folder
    /// that the export has no business deciding on its own.
    /// </para>
    /// </remarks>
    private static void RequireNoSymbolicLink(string projectDirectory)
    {
        EnumerationOptions options = new()
        {
            RecurseSubdirectories = true,
            // Left off deliberately: following a link while looking for links
            // would walk out of the folder, which is the thing being prevented.
            AttributesToSkip = FileAttributes.None,
            IgnoreInaccessible = false,
        };

        foreach (FileSystemInfo entry in new DirectoryInfo(projectDirectory)
                     .EnumerateFileSystemInfos("*", options))
        {
            if (entry.LinkTarget is not null)
            {
                throw new ProjectException(
                    ProjectErrorCode.ArchiveExportFailed,
                    $"'{entry.FullName}' is a symbolic link pointing at '{entry.LinkTarget}'. " +
                    "An export never follows a link: it would put a file from outside the project " +
                    "into an archive the user then sends on. Remove the link and export again.");
            }
        }
    }

    /// <summary>Reads and validates the project file of the folder being exported.</summary>
    /// <remarks>
    /// No calibration is needed and none is asked for: an export has no business
    /// with the relational diagnostics of C.7.1 step 9. What matters here is that
    /// the file is intrinsically sound, so that no archive is ever produced from
    /// a project that could not be reopened.
    /// </remarks>
    private static ProjectDocument ReadDocument(string projectDirectory)
    {
        string path = Path.Combine(projectDirectory, ProjectFileWriter.FileName);

        if (!File.Exists(path))
        {
            throw new ProjectException(
                ProjectErrorCode.NotFound,
                $"No '{ProjectFileWriter.FileName}' in '{projectDirectory}'.");
        }

        ProjectDocument document = ProjectJson.Deserialize(File.ReadAllBytes(path));
        ProjectValidation.Validate(document);

        return document;
    }

    /// <summary>
    /// The referenced images, having checked every one of them is really there.
    /// </summary>
    /// <remarks>
    /// This is where the diagnostic the load tolerates (C.7.2) becomes blocking,
    /// and the two are consistent: <b>opening a project with holes in it is
    /// acceptable, handing one out is not.</b>
    /// </remarks>
    private static IReadOnlyList<string> RequireEveryReferencedImage(
        string projectDirectory,
        ProjectDocument archived)
    {
        IReadOnlyList<string> images = ShareFilter.ReferencedImages(archived);

        foreach (string relative in images)
        {
            if (!File.Exists(Path.Combine(projectDirectory, relative)))
            {
                throw new ProjectException(
                    ProjectErrorCode.ArchiveExportFailed,
                    $"'{relative}' is referenced by the project but is not on this disk. " +
                    "An archive never holds a dangling reference.");
            }
        }

        return images;
    }

    /// <summary>The PDFs of <c>exports/</c>, which only a <c>Backup</c> carries.</summary>
    private static IReadOnlyList<string> ListExports(string projectDirectory)
    {
        string exports = Path.Combine(projectDirectory, "exports");

        if (!Directory.Exists(exports))
        {
            return [];
        }

        return
        [
            .. Directory.EnumerateFiles(exports, "*.pdf", SearchOption.TopDirectoryOnly)
                .Select(path => "exports/" + Path.GetFileName(path))
                .Order(StringComparer.Ordinal),
        ];
    }

    // ---- L'écriture --------------------------------------------------------

    private static async Task WriteArchiveAsync(
        string archivePath,
        ArchiveProfile profile,
        ProjectDocument archived,
        string projectDirectory,
        IReadOnlyList<string> images,
        IReadOnlyList<string> exports,
        CancellationToken cancellationToken)
    {
        await using FileStream file = File.Create(archivePath);
        using ZipArchive zip = new(file, ZipArchiveMode.Create);

        // archive.json first, so an import can read it from the stream without
        // walking the whole archive - which is what lets it refuse before
        // extracting anything (MEN-001).
        await AddAsync(
            zip,
            ArchiveManifestFile.EntryName,
            ArchiveManifestFile.Serialize(ArchiveManifestFile.For(profile, DateTimeOffset.UtcNow)),
            cancellationToken).ConfigureAwait(false);

        await AddAsync(
            zip,
            ProjectFileWriter.FileName,
            ProjectJson.Serialize(archived),
            cancellationToken).ConfigureAwait(false);

        foreach (string relative in images.Order(StringComparer.Ordinal))
        {
            await AddFileAsync(zip, relative, Path.Combine(projectDirectory, relative), cancellationToken)
                .ConfigureAwait(false);
        }

        foreach (string relative in exports)
        {
            await AddFileAsync(zip, relative, Path.Combine(projectDirectory, relative), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task AddAsync(
        ZipArchive zip,
        string entryName,
        byte[] content,
        CancellationToken cancellationToken)
    {
        ZipArchiveEntry entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);

        await using Stream stream = entry.Open();
        await stream.WriteAsync(content, cancellationToken).ConfigureAwait(false);
    }

    private static async Task AddFileAsync(
        ZipArchive zip,
        string entryName,
        string sourcePath,
        CancellationToken cancellationToken)
    {
        // The entry name is the stored path, which C.3.5 already guarantees is
        // relative, POSIX and under images/. Nothing is derived from the
        // absolute path, so no machine detail can leak into an entry name.
        ZipArchiveEntry entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);

        await using Stream target = entry.Open();
        await using FileStream source = File.OpenRead(sourcePath);
        await source.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
