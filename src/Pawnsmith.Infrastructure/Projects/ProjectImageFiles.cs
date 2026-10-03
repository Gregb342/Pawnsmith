namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// Writes and deletes image files of a project folder, and nothing outside it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Deletion</b> is the disk half of DEC-070. It receives the exact list
/// <c>BlueprintRemoval</c> produced and deletes those files, one by one. It
/// never lists <c>images/</c> to find orphans — see that class for why.
/// </para>
/// <para>
/// Every path goes through <see cref="ImagePathRules"/> and then through the
/// resolved-prefix check of C.3.5 before <c>File.Delete</c> is called. The
/// paths come from a <c>project.json</c> that the reader already validated,
/// but a deletion is the one operation where a path that escaped would do
/// irreversible harm, and two comparisons are cheap (MEN-002).
/// </para>
/// <para>
/// A file that is already gone is not an error: the model no longer
/// references it, which is the state being aimed for.
/// </para>
/// </remarks>
public static class ProjectImageFiles
{
    /// <summary>Writes the paired image of a new candidate, and returns the path to store on it.</summary>
    /// <remarks>
    /// <para>
    /// <b>The name is Pawnsmith's</b>: <c>images/{candidateId}-pair.png</c>, as
    /// C.3.1 lays out. Nothing the generator said about its own file reaches
    /// the disk (§E.7.2).
    /// </para>
    /// <para>
    /// <b>Written to a temporary name, then moved.</b> A file that is half
    /// written when the process stops must not carry the name a candidate is
    /// about to reference. The move never overwrites: a file already at that
    /// name means something is wrong, and replacing it would hide what.
    /// </para>
    /// <para>
    /// <b>An <c>images</c> folder that is a symbolic link is refused</b>, for
    /// the reason MEN-008 gives at export: the prefix check on the path is made
    /// on the path as written, and a link would make that path land elsewhere.
    /// </para>
    /// </remarks>
    /// <returns>The path relative to the project folder, with <c>/</c> as separator.</returns>
    /// <exception cref="ProjectException">
    /// <c>PROJECT_NOT_FOUND</c> when the folder does not exist;
    /// <c>PROJECT_PATH_ESCAPE</c> when <c>images</c> is a link or the path would leave the folder.
    /// </exception>
    /// <exception cref="IOException">A file already sits at that name.</exception>
    public static async Task<string> WritePairedAsync(
        string projectDirectory,
        Guid candidateId,
        byte[] png,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectDirectory);
        ArgumentNullException.ThrowIfNull(png);

        string root = Path.GetFullPath(projectDirectory);

        if (!Directory.Exists(root))
        {
            throw new ProjectException(
                ProjectErrorCode.NotFound,
                $"The project folder '{projectDirectory}' does not exist; the image has nowhere to go.");
        }

        string relative = $"{ImagePathRules.ImagesFolder}{ImagePathRules.Separator}{candidateId:D}-pair.png";
        ImagePathRules.Validate(relative, "paired image");

        string images = Path.Combine(root, ImagePathRules.ImagesFolder);

        if (new DirectoryInfo(images).LinkTarget is not null)
        {
            throw new ProjectException(
                ProjectErrorCode.PathEscape,
                $"The '{ImagePathRules.ImagesFolder}' folder of '{projectDirectory}' is a symbolic link; " +
                "nothing is written through it (MEN-008).");
        }

        string full = Resolve(root, relative);
        Directory.CreateDirectory(images);

        string temporary = Path.Combine(images, $".{candidateId:D}-pair.png.tmp");

        try
        {
            await File.WriteAllBytesAsync(temporary, png, cancellationToken).ConfigureAwait(false);
            File.Move(temporary, full, overwrite: false);
        }
        finally
        {
            // Gone after a successful move; left behind by a failed write or a
            // refused move, and removed here so no ".tmp" outlives the call.
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }

        return relative;
    }

    /// <summary>Opens an image of the project for reading, or returns null when it is not on the disk.</summary>
    /// <remarks>
    /// The same three checks as the other operations: the stored-path rules of
    /// C.3.5, the resolved prefix, and no <c>images</c> folder that is a link.
    /// Whether the project references the file is the caller's question
    /// (DEC-088); this only guarantees that the path cannot leave the folder.
    /// </remarks>
    /// <exception cref="ProjectException"><c>PROJECT_PATH_ESCAPE</c>.</exception>
    public static Stream? OpenForReading(string projectDirectory, string relativePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectDirectory);

        ImagePathRules.Validate(relativePath, "image to read");

        string root = Path.GetFullPath(projectDirectory);
        string images = Path.Combine(root, ImagePathRules.ImagesFolder);

        if (new DirectoryInfo(images).LinkTarget is not null)
        {
            throw new ProjectException(
                ProjectErrorCode.PathEscape,
                $"The '{ImagePathRules.ImagesFolder}' folder of '{projectDirectory}' is a symbolic link; " +
                "nothing is read through it (MEN-008).");
        }

        string full = Resolve(root, relativePath);

        return File.Exists(full)
            ? new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read)
            : null;
    }

    /// <summary>Deletes the given files, relative to the project folder.</summary>
    /// <returns>The number of files actually removed.</returns>
    /// <exception cref="ProjectException"><c>PROJECT_PATH_ESCAPE</c> for a path that leaves the folder.</exception>
    public static int Delete(string projectDirectory, IReadOnlyList<string> relativePaths)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectDirectory);
        ArgumentNullException.ThrowIfNull(relativePaths);

        string root = Path.GetFullPath(projectDirectory);

        // Every path is checked before the first file is deleted, never one at
        // a time as we go. It is the rule MEN-001 states for the archive -
        // everything is validated before a single byte is written - applied to
        // a deletion, where it matters more: a check inside the loop would let
        // a list of [legitimate, escaping] destroy the first file before
        // refusing the second, and a deletion does not roll back.
        List<string> resolved = [];

        foreach (string relative in relativePaths)
        {
            ImagePathRules.Validate(relative, "file to delete");
            resolved.Add(Resolve(root, relative));
        }

        int removed = 0;

        foreach (string path in resolved)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                removed++;
            }
        }

        return removed;
    }

    /// <summary>The full path of a stored path, refused if it would leave the project folder (C.3.5).</summary>
    private static string Resolve(string root, string relative)
    {
        string rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        string full = Path.GetFullPath(Path.Combine(
            root,
            relative.Replace(ImagePathRules.Separator, Path.DirectorySeparatorChar)));

        if (!full.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new ProjectException(
                ProjectErrorCode.PathEscape,
                $"The file '{relative}' resolves outside the project folder; nothing is done with it.");
        }

        return full;
    }
}
