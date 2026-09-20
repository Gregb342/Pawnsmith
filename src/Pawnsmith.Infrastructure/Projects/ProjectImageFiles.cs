namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// Deletes image files of a project folder, and nothing outside it.
/// </summary>
/// <remarks>
/// <para>
/// The disk half of DEC-070. It receives the exact list
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
    /// <summary>Deletes the given files, relative to the project folder.</summary>
    /// <returns>The number of files actually removed.</returns>
    /// <exception cref="ProjectException"><c>PROJECT_PATH_ESCAPE</c> for a path that leaves the folder.</exception>
    public static int Delete(string projectDirectory, IReadOnlyList<string> relativePaths)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectDirectory);
        ArgumentNullException.ThrowIfNull(relativePaths);

        string root = Path.GetFullPath(projectDirectory);
        string rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

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

            string full = Path.GetFullPath(Path.Combine(
                root,
                relative.Replace(ImagePathRules.Separator, Path.DirectorySeparatorChar)));

            if (!full.StartsWith(rootWithSeparator, StringComparison.Ordinal))
            {
                throw new ProjectException(
                    ProjectErrorCode.PathEscape,
                    $"The file '{relative}' resolves outside the project folder and will not be deleted.");
            }

            resolved.Add(full);
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
}
