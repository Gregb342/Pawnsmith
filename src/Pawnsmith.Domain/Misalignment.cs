namespace Pawnsmith.Domain;

/// <summary>
/// Works out which of a candidate's frozen clauses no longer match the
/// project's current ones.
/// </summary>
/// <remarks>
/// <para>
/// This is the safety mechanism DEC-030 put in place of a lock. Rather than
/// forbidding the user from changing a style, the system lets them change it
/// and then says <b>which candidates</b> were produced under the old one — and
/// since DEC-049, <b>which clause</b> moved. A lock would have been a consent
/// mechanism: it fires when the user is already set on the change, whereas the
/// problem only surfaces at export.
/// </para>
/// <para>
/// <b>Nothing here is ever stored.</b> Misalignment is computed from the
/// project and the framing clause, on demand, and never serialised — a
/// computed value that gets persisted becomes a value that lies from the first
/// missed update (section 3.2 of the bible).
/// </para>
/// </remarks>
public static class Misalignment
{
    /// <summary>
    /// The set of clauses that have moved since this candidate was produced.
    /// Empty means aligned.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The comparison is <b>ordinal and case-sensitive, character by
    /// character</b>, on normalised clauses. No leniency: no case folding, no
    /// collapsing of repeated spaces, no Unicode normalisation. The one
    /// concession is the <c>Trim</c> inside
    /// <see cref="ResolvedPrompt.Normalize"/>, and it is sound — a trailing
    /// space changes nothing in the prompt that gets sent, since the assembly
    /// removes it too. A difference in case, or a space <i>inside</i> the
    /// clause, does misalign: a diffusion model is not indifferent to either.
    /// </para>
    /// <para>
    /// Ordinal matters as much as case here. A culture-sensitive comparison
    /// varies with the culture of the process and with the installed ICU
    /// version, so the same project could be aligned on one machine and
    /// misaligned on another.
    /// </para>
    /// </remarks>
    /// <param name="candidate">The candidate, carrying the three clauses frozen at generation time.</param>
    /// <param name="blueprint">The blueprint it belongs to, carrying the current subject clause.</param>
    /// <param name="style">The project's style, carrying the current style clause.</param>
    /// <param name="framingClause">The framing clause in force today. T2 receives it, it does not fetch it.</param>
    /// <returns>The set of drifted clauses, possibly empty. Never null.</returns>
    public static IReadOnlySet<ClauseKind> Of(
        Candidate candidate,
        Blueprint blueprint,
        Style style,
        string framingClause)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(style);
        ArgumentNullException.ThrowIfNull(framingClause);

        HashSet<ClauseKind> drifted = [];

        if (Differs(candidate.FramingClauseUsed, framingClause))
        {
            drifted.Add(ClauseKind.Framing);
        }

        if (Differs(candidate.SubjectClauseUsed, blueprint.SubjectClause))
        {
            drifted.Add(ClauseKind.Subject);
        }

        if (Differs(candidate.StyleClauseUsed, style.StyleClause))
        {
            drifted.Add(ClauseKind.Style);
        }

        return drifted;
    }

    /// <summary>
    /// Whether this candidate is misaligned at all.
    /// </summary>
    /// <remarks>
    /// <b>Misalignment is not a status, and this method changes nothing.</b> It
    /// does not rewrite <see cref="Candidate.Status"/>, does not un-elect a
    /// candidate, does not touch a file. A <c>Valid</c> candidate that becomes
    /// misaligned stays <c>Valid</c>: the two are independent axes, and merging
    /// them is the natural mistake at this spot (DEC-030).
    /// <para>
    /// There is also <b>no partial realignment</b>. Knowing which clause
    /// drifted serves to explain, never to copy a current clause back onto a
    /// candidate: a candidate is an image produced under a given prompt, and
    /// rewriting its prompt without regenerating the image is precisely the lie
    /// DEC-030 exists to prevent.
    /// </para>
    /// </remarks>
    public static bool IsMisaligned(
        Candidate candidate,
        Blueprint blueprint,
        Style style,
        string framingClause)
    {
        return Of(candidate, blueprint, style, framingClause).Count > 0;
    }

    private static bool Differs(string frozen, string current)
    {
        return !string.Equals(
            ResolvedPrompt.Normalize(frozen),
            ResolvedPrompt.Normalize(current),
            StringComparison.Ordinal);
    }
}
