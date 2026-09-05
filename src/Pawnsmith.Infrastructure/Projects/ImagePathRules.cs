namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// The shape an image path stored in <c>project.json</c> is allowed to have.
/// </summary>
/// <remarks>
/// <para>
/// Three reasons the paths are relative and constrained, and the second is the
/// one people forget (C.3.5). <b>Portability</b>: a <c>C:\Users\...</c> makes an
/// archive useless anywhere else. <b>Information leak</b>: chapter 8 keeps logs
/// out of an archive and names "absolute paths" among what they contain — an
/// absolute path in <c>project.json</c> would let in by the door what MEN-006
/// pushes out of the window, namely the user's account name and the shape of
/// their disk. <b>Attack surface</b>: a path read from a file and joined to a
/// root is the definition of MEN-002, and a project can arrive from a
/// third-party archive.
/// </para>
/// <para>
/// Everything here is <b>syntax</b>, checked before any file is opened. The
/// remaining rule of C.3.5 — that the resolved absolute path is prefixed by the
/// project folder, after following links — needs the file system and belongs to
/// the reader. Both are needed: this one stops the obvious, that one stops a
/// symbolic link.
/// </para>
/// </remarks>
public static class ImagePathRules
{
    /// <summary>The only folder image files may live in.</summary>
    public const string ImagesFolder = "images";

    /// <summary>The only separator a stored path may use.</summary>
    /// <remarks>
    /// A backslash is refused outright. It separates on one platform and is a
    /// valid filename character on the other, and that ambiguity is settled
    /// rather than guessed: a file genuinely called <c>a\b.png</c> on Linux would
    /// otherwise become a folder on Windows.
    /// </remarks>
    public const char Separator = '/';

    /// <summary>Checks one stored path, without touching the disk.</summary>
    /// <param name="path">The path as read from the file.</param>
    /// <param name="field">Where it came from, for the message — for example <c>blueprints[0].candidates[1].frontImageFile</c>.</param>
    /// <exception cref="ProjectException">The path breaks any rule of C.3.5.</exception>
    public static void Validate(string path, string field)
    {
        ArgumentNullException.ThrowIfNull(field);

        if (string.IsNullOrWhiteSpace(path))
        {
            throw Refuse(field, path, "it is empty");
        }

        if (path.Contains('\\', StringComparison.Ordinal))
        {
            throw Refuse(field, path, "it contains a backslash");
        }

        // Rooted covers "/images/a.png" on both platforms and "C:/..." on
        // Windows; the colon test covers a drive letter on Linux too, where
        // Path.IsPathRooted would not see one. A UNC prefix always starts with
        // two backslashes and is already gone.
        if (Path.IsPathRooted(path) || path.Contains(':', StringComparison.Ordinal))
        {
            throw Refuse(field, path, "it is not relative");
        }

        string[] segments = path.Split(Separator);

        if (Array.Exists(segments, segment => segment is "." or ".." or ""))
        {
            throw Refuse(field, path, "it contains an empty, '.' or '..' segment");
        }

        if (segments.Length < 2 || !string.Equals(segments[0], ImagesFolder, StringComparison.Ordinal))
        {
            throw Refuse(field, path, $"it does not start with '{ImagesFolder}/'");
        }
    }

    private static ProjectException Refuse(string field, string? path, string why) =>
        new(ProjectErrorCode.PathEscape,
            $"The image path '{path}' at {field} is refused because {why}. " +
            $"Stored paths are relative to the project folder, use '{Separator}' as their only " +
            $"separator, and live under '{ImagesFolder}/'.");
}
