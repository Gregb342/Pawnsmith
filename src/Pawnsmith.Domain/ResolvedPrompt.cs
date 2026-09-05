namespace Pawnsmith.Domain;

/// <summary>
/// Assembles the three clauses into the prompt that is actually sent, and
/// normalises a clause.
/// </summary>
/// <remarks>
/// <para>
/// The resolved prompt is a <b>function, not a field</b>: it is never stored on
/// a blueprint, never stored on a candidate, and never serialised. A candidate
/// freezes its three clauses and the prompt is rebuilt from them (DEC-049).
/// </para>
/// <para>
/// <b>The assembly rule is a compatibility surface, on a par with a file
/// schema.</b> Changing the order of the clauses or the separator changes every
/// prompt, and therefore misaligns every candidate of every project, at once
/// and immediately. It takes a decision card, not a convenience commit
/// (C.5.5).
/// </para>
/// </remarks>
public static class ResolvedPrompt
{
    /// <summary>
    /// The one separator between two clauses. A single line feed, never
    /// <c>Environment.NewLine</c>.
    /// </summary>
    /// <remarks>
    /// This is the trap the constant exists to close. A platform-dependent
    /// separator would make a project saved on Windows and reopened on Linux
    /// produce a different resolved prompt for the same clauses — so a
    /// <b>global, phantom misalignment</b>, appearing on every candidate at
    /// once, for a reason nothing on screen could explain. It shows up neither
    /// in a test nor in review, and it destroys trust in the only safety
    /// mechanism the product has.
    /// </remarks>
    private const string Separator = "\n";

    /// <summary>
    /// Builds the prompt from its three clauses, in the order framing →
    /// subject → style.
    /// </summary>
    /// <remarks>
    /// The order is that of section 4.1 of the bible and of DEC-028, and it is
    /// fixed. Empty clauses are <b>omitted rather than joined</b>, so an empty
    /// style does not leave a prompt ending in a line feed.
    /// <para>
    /// A consequence worth stating, because it looks like a defect and is not:
    /// the assembly is <b>not injective</b>. An empty style and an absent style
    /// give the same prompt. That is correct — they mean the same thing.
    /// </para>
    /// </remarks>
    /// <param name="framingClause">What makes the image cuttable and easy to cut out.</param>
    /// <param name="subjectClause">What the user asked for, as stored on the blueprint.</param>
    /// <param name="styleClause">The project's style clause.</param>
    /// <returns>The prompt to send, with its clauses normalised.</returns>
    public static string From(string framingClause, string subjectClause, string styleClause)
    {
        ArgumentNullException.ThrowIfNull(framingClause);
        ArgumentNullException.ThrowIfNull(subjectClause);
        ArgumentNullException.ThrowIfNull(styleClause);

        string[] clauses =
        [
            Normalize(framingClause),
            Normalize(subjectClause),
            Normalize(styleClause),
        ];

        return string.Join(Separator, clauses.Where(clause => clause.Length > 0));
    }

    /// <summary>
    /// Puts a clause in its canonical form: line endings reduced to
    /// <c>\n</c>, then leading and trailing whitespace removed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Normalising once, early and visibly, is what lets the comparison be
    /// stupid.</b> A misalignment check that tried to be clever — ignoring
    /// case, collapsing runs of spaces — would declare "aligned" a candidate
    /// that is not, which is failure in the dangerous direction. A check with
    /// no tolerance at all would cry wolf, and a warning that fires wrongly
    /// ends up ignored, which amounts to not having it. The way out of that
    /// tension is here rather than in the comparison (C.5.4).
    /// </para>
    /// <para>
    /// The function is idempotent, so applying it at assembly time as well as
    /// at write time costs nothing and removes the question of where it
    /// happens.
    /// </para>
    /// </remarks>
    /// <param name="clause">The clause to normalise.</param>
    /// <returns>The normalised clause. Never null.</returns>
    public static string Normalize(string clause)
    {
        ArgumentNullException.ThrowIfNull(clause);

        // "\r\n" first, then any lone "\r". The other order would turn every
        // Windows line ending into two line feeds.
        return clause
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal)
            .Trim();
    }
}
