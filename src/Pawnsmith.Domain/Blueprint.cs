namespace Pawnsmith.Domain;

/// <summary>
/// <i>What the user wants</i>: a creature described by its parameters, its
/// quantity, and its subject clause. Persistent and stable, unlike the
/// candidates hanging off it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SubjectClause"/> is the <b>only</b> segment of the prompt the
/// user may edit (DEC-028). It is produced once by the composer of T3 from the
/// fields above it, then stored; it never regenerates itself after an edit.
/// T2 reads and writes it without knowing how to build it.
/// </para>
/// <para>
/// The assembled prompt is <b>not</b> a field here. It is derived from the
/// three clauses and computed on demand, never persisted.
/// </para>
/// </remarks>
/// <param name="Id">Stable identifier, referenced by <see cref="ElectedCandidateId"/>.</param>
/// <param name="Race">Mandatory.</param>
/// <param name="CharacterClass">Mandatory. Named this way because <c>class</c> is a C# keyword — see the note below.</param>
/// <param name="Size">Mandatory. The grouping key that decides which page this pawn lands on (DEC-005).</param>
/// <param name="OptionalParameters">Catalogue keys and their values. Not validated against anything in T2.</param>
/// <param name="Details">Free text, folded into the subject clause when it is composed.</param>
/// <param name="SubjectClause">The stored, editable clause. May be empty.</param>
/// <param name="Quantity">Number of copies on the sheet. At least one.</param>
/// <param name="Candidates">Attempts made for this blueprint, in display order.</param>
/// <param name="ElectedCandidateId">The elected candidate, which must belong to <b>this</b> blueprint.</param>
public sealed record Blueprint(
    Guid Id,
    string Race,
    string CharacterClass,
    Size Size,
    IReadOnlyDictionary<string, string> OptionalParameters,
    string Details,
    string SubjectClause,
    int Quantity,
    IReadOnlyList<Candidate> Candidates,
    Guid? ElectedCandidateId);

// Two naming points, written down rather than suffered:
//
// `class` is a reserved word in C#, so the "classe" of the glossary is
// `characterClass`, in the type and in the JSON alike. Writing `@class` to save
// the symmetry would be worse: an escaped identifier reads badly and, more to
// the point, is hard to grep for.
//
// An absent key in `OptionalParameters` means "not constrained", never "absent
// from the illustration" (DEC-024). The model cannot express the difference and
// deliberately does not try: the distinction is carried by the wording of the
// interface, not by a third value.

/// <summary>
/// <i>What the model produced</i>: one concrete attempt for a blueprint,
/// identified by its seed.
/// </summary>
/// <remarks>
/// <para>
/// A candidate freezes <b>its three clauses separately</b>, not the assembled
/// prompt (DEC-049). Two reasons. Knowing <i>which</i> clause moved is what
/// makes the misalignment warning actionable — with a single string the
/// interface can only say "something changed". And the framing clause is
/// editable through the ComfyUI workflow template (DEC-029), so adjusting it
/// misaligns the user's whole library at once; that has to be explainable.
/// </para>
/// <para>
/// The price is real and is named here so it is not forgotten: the prompt that
/// actually went over the wire is <b>reconstructed</b>, not replayed. It stays
/// faithful only for as long as T4 sends exactly the join of these three
/// clauses. If T4 ever transforms the prompt after assembly, DEC-049 must be
/// reopened.
/// </para>
/// </remarks>
/// <param name="Id">Stable identifier. Names the image files on disk.</param>
/// <param name="Seed">Generation seed. Unsigned 64-bit — see the note below.</param>
/// <param name="FramingClauseUsed">Framing clause frozen at generation time.</param>
/// <param name="SubjectClauseUsed">Subject clause frozen at generation time.</param>
/// <param name="StyleClauseUsed">Style clause frozen at generation time.</param>
/// <param name="Status">The user's arbitration. Independent of misalignment.</param>
/// <param name="PairedImageFile">Raw two-view image, kept for diagnosis. Null once a <c>Share</c> archive has dropped it.</param>
/// <param name="FrontImageFile">Cut-out front view, or null before T5 has run.</param>
/// <param name="BackImageFile">Cut-out back view, or null before T5 has run.</param>
/// <param name="GeneratedAt">When the pair was produced, in UTC.</param>
public sealed record Candidate(
    Guid Id,
    ulong Seed,
    string FramingClauseUsed,
    string SubjectClauseUsed,
    string StyleClauseUsed,
    CandidateStatus Status,
    string? PairedImageFile,
    string? FrontImageFile,
    string? BackImageFile,
    DateTimeOffset GeneratedAt);

// The seed is a `ulong` because ComfyUI seeds run to 2^64 - 1. It is written to
// JSON as a decimal string, not as a number: a JSON number is read by
// `JSON.parse` as an IEEE-754 double, and past 2^53 the value is silently
// rounded. The front end is React (DEC-018), so the seed will pass through
// `JSON.parse` in T6. A rounded seed is a generation that cannot be replayed,
// and the defect only shows the day someone replays one — never during a test.
//
// The three image paths are nullable because the life cycle demands it: between
// generation (T4) and cut-out (T5) a candidate has a paired image and no
// cut-outs. Refusing null would force T4 to write paths to files that do not
// exist, which is worse. Whether a given `Status` implies a given set of files
// is a business rule, so question C of chapter 16 of the bible, so T3.
