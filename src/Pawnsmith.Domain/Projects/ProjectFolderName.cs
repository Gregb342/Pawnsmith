using System.Globalization;
using System.Text;

namespace Pawnsmith.Domain.Projects;

/// <summary>
/// Turns the display name of a project into a folder name that is safe to put
/// on a disk.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is a piece of security, not a convenience</b> (MEN-009).
/// <see cref="Project.Name"/> is free text: it comes from whoever typed it, or
/// from a third-party archive, and <c>../../logs</c> is a perfectly valid value
/// for it. Pasting that onto the projects root would write outside the root.
/// The threat is MEN-002 with a different way in — the rule there is never to
/// build a path by concatenating user input, and it applies here word for word.
/// </para>
/// <para>
/// Nothing is lost by mangling the name, because <b>the folder name carries no
/// meaning</b> (DEC-047). What identifies a project is its <c>projectId</c>;
/// the folder is an ordinary folder that may be renamed, duplicated or restored
/// under another name. The name the user typed is kept, untouched, in
/// <see cref="Project.Name"/>.
/// </para>
/// <para>
/// What this does <b>not</b> do, on purpose: it resolves no collision and
/// touches no disk. Suffixing <c>-2</c> when a folder already exists, and
/// checking that the result really sits under the projects root, both need to
/// look at a file system — so they belong to the repository, in Infrastructure.
/// The two halves are separated so that this one stays a pure function with an
/// exhaustive test.
/// </para>
/// </remarks>
public static class ProjectFolderName
{
    /// <summary>Longest folder name produced, in characters.</summary>
    /// <remarks>
    /// File systems have their own limits, and a 3 000-character name is a
    /// hostile input rather than a long title. Sixty-four leaves room for the
    /// collision suffix the repository may add, and for the archive file name
    /// of C.8.5 which is built from this one.
    /// </remarks>
    public const int MaxLength = 64;

    /// <summary>Used when nothing usable survives the filter.</summary>
    /// <remarks>
    /// A project named entirely in an alphabet with no ASCII transliteration —
    /// or named <c>"..."</c> — still needs a folder. It gets this one, and the
    /// repository turns the second such project into <c>project-2</c>.
    /// </remarks>
    public const string Fallback = "project";

    /// <summary>
    /// Device names Windows still reserves, inherited from MS-DOS.
    /// </summary>
    /// <remarks>
    /// These are not file names on Windows, they are devices: a file called
    /// <c>NUL</c> cannot be created, and writing to it goes nowhere. The
    /// comparison is case-insensitive, because <c>Con</c> and <c>con</c> are the
    /// same device.
    /// </remarks>
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>
    /// The folder name for a project called <paramref name="name"/>.
    /// </summary>
    /// <remarks>
    /// The steps, in order, each of which matters:
    /// <list type="number">
    /// <item>lower-case, using the invariant culture — see the note below;</item>
    /// <item>transliterate, so that <c>é</c> becomes <c>e</c> rather than a dash;</item>
    /// <item>keep <c>a-z</c>, <c>0-9</c> and <c>-</c>; replace everything else with a dash;</item>
    /// <item>collapse runs of dashes, and trim them from both ends;</item>
    /// <item>truncate to <see cref="MaxLength"/>, then trim dashes again;</item>
    /// <item>fall back if the result is empty or names a device.</item>
    /// </list>
    /// <para>
    /// The whitelist is what makes this safe, and it is a whitelist on purpose:
    /// listing what is allowed cannot be outflanked by a character nobody
    /// thought of, whereas a blacklist can. It also settles two rules of C.3.2
    /// for free — a name can end in neither a dot nor a space, because neither
    /// ever survives step 3.
    /// </para>
    /// <para>
    /// <b>Why the invariant culture.</b> In Turkish, <c>"I".ToLower()</c> is
    /// <c>"ı"</c>, a dotless i with no ASCII equivalent, which step 3 would turn
    /// into a dash. The same project would then get two different folder names
    /// on two machines, for no reason anyone could see. Culture-sensitive
    /// casing has no place in a path.
    /// </para>
    /// </remarks>
    /// <param name="name">The display name, as the user typed it.</param>
    /// <returns>A non-empty folder name, at most <see cref="MaxLength"/> characters.</returns>
    public static string From(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        string transliterated = Transliterate(name.ToLowerInvariant());
        string filtered = KeepAllowedCharacters(transliterated);
        string collapsed = CollapseDashes(filtered);
        string truncated = collapsed.Length > MaxLength
            ? collapsed[..MaxLength].Trim('-')
            : collapsed;

        return truncated.Length == 0 || ReservedNames.Contains(truncated)
            ? Fallback
            : truncated;
    }

    /// <summary>
    /// Strips the accents off Latin letters, leaving everything else alone.
    /// </summary>
    /// <remarks>
    /// Decomposing to <see cref="NormalizationForm.FormD"/> splits <c>é</c> into
    /// an <c>e</c> followed by a combining acute accent; dropping every
    /// combining mark leaves the <c>e</c>. Letters with no such decomposition —
    /// Cyrillic, Greek, an ideogram — come through unchanged and are dealt with
    /// by the whitelist, which turns them into dashes. That is the intended
    /// outcome: a folder name is not the place to preserve a script.
    /// </remarks>
    private static string Transliterate(string value)
    {
        string decomposed = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new(decomposed.Length);

        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static string KeepAllowedCharacters(string value)
    {
        StringBuilder builder = new(value.Length);

        foreach (char character in value)
        {
            bool allowed = character is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-';
            builder.Append(allowed ? character : '-');
        }

        return builder.ToString();
    }

    private static string CollapseDashes(string value)
    {
        StringBuilder builder = new(value.Length);

        foreach (char character in value)
        {
            if (character != '-' || builder.Length == 0 || builder[^1] != '-')
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Trim('-');
    }
}
