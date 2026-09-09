namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// The shape an archive entry is allowed to have, and the whitelist of C.8.2
/// written out rather than left implicit.
/// </summary>
/// <remarks>
/// <para>
/// The export already honours this list, but it honours it <b>by
/// construction</b> — it enumerates what it adds, so it never had to ask "is
/// this entry allowed?". The import has no such luxury: it is handed a list of
/// names by someone else, possibly hostile, and has to answer that question one
/// name at a time. So the list becomes a function here, and the two sides of
/// C.8.2 can be read side by side.
/// </para>
/// <para>
/// Everything here is <b>syntax on a name</b>. Nothing opens a file, nothing
/// resolves a path, nothing touches the archive. That is what lets step 4 of
/// C.9.1 run before a single byte is extracted, which is the whole of MEN-001.
/// </para>
/// <para>
/// Same division of labour as <see cref="ImagePathRules"/>, and for the same
/// reason: the lexical half stops the obvious, and the half that needs a file
/// system — resolving each path under the temporary folder as it is written —
/// stops what the lexical half cannot see. DEC-051 insists on both, because a
/// validator that inspects one name and an extractor that writes another is
/// exactly how a duplicated entry gets through.
/// </para>
/// </remarks>
public static class ArchiveEntryRules
{
    /// <summary>The only separator an archive entry may use.</summary>
    /// <remarks>
    /// ZIP entries are POSIX paths by specification, and C.8.5 requires it of
    /// what we write. A backslash is refused rather than translated: it
    /// separates on one platform and is a valid filename character on the other,
    /// and that ambiguity is settled, never guessed.
    /// </remarks>
    public const char Separator = '/';

    /// <summary>The one folder image files may live in.</summary>
    public const string ImagesFolder = "images";

    /// <summary>The one folder rendered sheets may live in.</summary>
    public const string ExportsFolder = "exports";

    private const string ImageExtension = ".png";
    private const string ExportExtension = ".pdf";

    /// <summary>
    /// Whether <paramref name="entryName"/> is a plain relative POSIX path.
    /// </summary>
    /// <remarks>
    /// Refused: an absolute path, a drive letter, a UNC prefix, a backslash, and
    /// any <c>.</c>, <c>..</c> or empty segment. Refused whether or not the
    /// whitelist would have caught it anyway — a name that cannot even be read
    /// as a relative path has no business being looked at further.
    /// </remarks>
    public static bool IsSafeRelativePath(string entryName)
    {
        ArgumentNullException.ThrowIfNull(entryName);

        if (entryName.Length == 0 || entryName.Contains('\\', StringComparison.Ordinal))
        {
            return false;
        }

        // A colon catches a drive letter on every platform, including Linux,
        // where Path.IsPathRooted would see nothing wrong with "C:/x.png". A UNC
        // prefix is two backslashes and is already gone.
        if (Path.IsPathRooted(entryName) || entryName.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        foreach (string segment in Segments(entryName))
        {
            if (segment is "" or "." or "..")
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Whether <paramref name="entryName"/> is one of the four things C.8.2
    /// allows into an archive.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>archive.json</c>, <c>project.json</c>, a <c>.png</c> directly under
    /// <c>images/</c>, or a <c>.pdf</c> directly under <c>exports/</c>. Nothing
    /// else, at any depth.
    /// </para>
    /// <para>
    /// <b>The list is the union of the two profiles, and is not narrowed by the
    /// profile the archive declares.</b> A <c>Share</c> carrying an
    /// <c>exports/</c> entry is therefore accepted. That looks lax and is not:
    /// <c>profile</c> lives in <c>archive.json</c>, which comes from the same
    /// sender as the entries, so keying the whitelist off it would let the
    /// sender choose their own rules. It buys nothing against an attacker, and
    /// it would refuse a harmless archive from a future build whose profiles
    /// differ from today's two. What the whitelist is really for is the shape of
    /// the thing — no <c>.env</c>, no <c>logs/</c>, no <c>.git/</c> — and that is
    /// the same shape in both profiles.
    /// </para>
    /// <para>
    /// The comparison is <b>ordinal and case-sensitive</b>. An entry called
    /// <c>Images/a.PNG</c> is refused rather than accepted-and-normalised: two
    /// names differing only in case are already a global refusal one rule
    /// further on, and accepting one spelling here would put the two rules in
    /// contradiction.
    /// </para>
    /// </remarks>
    public static bool IsWhitelisted(string entryName)
    {
        ArgumentNullException.ThrowIfNull(entryName);

        if (string.Equals(entryName, ArchiveManifestFile.EntryName, StringComparison.Ordinal)
            || string.Equals(entryName, ProjectFileWriter.FileName, StringComparison.Ordinal))
        {
            return true;
        }

        string[] segments = Segments(entryName);

        if (segments.Length != 2)
        {
            return false;
        }

        return (segments[0], Path.GetExtension(segments[1])) switch
        {
            (ImagesFolder, ImageExtension) => true,
            (ExportsFolder, ExportExtension) => true,
            _ => false,
        };
    }

    /// <summary>Whether the entry is an image, for the consistency check of C.9.1 step 6.</summary>
    public static bool IsImage(string entryName)
    {
        ArgumentNullException.ThrowIfNull(entryName);

        return entryName.StartsWith(ImagesFolder + Separator, StringComparison.Ordinal);
    }

    /// <summary>How many segments <paramref name="entryName"/> holds.</summary>
    /// <remarks>
    /// Counted rather than derived from the whitelist, because the depth bound of
    /// C.9.3 is a resource bound and refuses with its own code — a 400-segment
    /// name is a hostile input, and saying so is more useful than "not in the
    /// whitelist".
    /// </remarks>
    public static int Depth(string entryName)
    {
        ArgumentNullException.ThrowIfNull(entryName);

        return Segments(entryName).Length;
    }

    private static string[] Segments(string entryName) => entryName.Split(Separator);
}
