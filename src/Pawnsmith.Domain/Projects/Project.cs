using Pawnsmith.Domain.Primitives;

namespace Pawnsmith.Domain.Projects;

/// <summary>
/// The aggregate root: a unit of persistent work that holds a style, a
/// geometry, a universe, a set of blueprints, and produces one or more sheets.
/// </summary>
/// <remarks>
/// <para>
/// <b>No field here is locked after creation</b> (DEC-030, confirmed by
/// DEC-055). Changing <see cref="Universe"/>, <see cref="Style"/>,
/// <see cref="Geometry"/> or <see cref="PaperFormatName"/> is allowed;
/// candidates produced under the previous values become <i>misaligned</i>,
/// which is a computed state and never a stored one. A lock would have been a
/// consent mechanism, and DEC-030 rejected it: a warning arrives when the user
/// is already motivated, whereas the problem only surfaces at export.
/// </para>
/// <para>
/// The consequence for the repository contract is what that card was written
/// to protect: since there is no transition to arbitrate, <c>SaveAsync</c>
/// never receives the previously loaded state. A transition rule, if a later
/// slice wants one, belongs to an Application use case that holds both states.
/// </para>
/// <para>
/// <c>versionSchema</c> is deliberately absent. It is a property of the
/// <i>file</i>, not of the project: the reader checks it, the writer emits the
/// one version it knows, and carrying it on the entity would let a project
/// claim a schema its writer cannot produce.
/// </para>
/// </remarks>
/// <param name="ProjectId">Opaque identity — see the note below.</param>
/// <param name="Name">Display label. Neither unique nor stable.</param>
/// <param name="Universe">Aesthetic register. One value in v1.</param>
/// <param name="Style">The project's style. A value object with no identity of its own.</param>
/// <param name="Geometry">How every pawn of this project is built (DEC-001, DEC-039).</param>
/// <param name="PaperFormatName">Key of an entry in the calibration's paper formats. Not resolved here.</param>
/// <param name="CalibrationOverrides">The two overridable dimensions. Always present.</param>
/// <param name="Blueprints">The blueprints, <b>in a significant order</b> — see the note below.</param>
/// <param name="CreatedAt">Creation instant, in UTC.</param>
/// <param name="ModifiedAt">Instant of the last save, in UTC.</param>
public sealed record Project(
    Guid ProjectId,
    string Name,
    Universe Universe,
    Style Style,
    Geometry Geometry,
    string PaperFormatName,
    CalibrationOverrides CalibrationOverrides,
    IReadOnlyList<Blueprint> Blueprints,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

// `ProjectId` is not a feature, it is the absence of a constraint (DEC-047).
// DEC-011 justifies the plain-folder persistence in three words — versionable,
// backupable, diffable — and the first two assume a folder gets renamed,
// duplicated, restored elsewhere under another name. Making the folder name the
// identity would have forbidden all three without the constraint being written
// anywhere, and the symptom would have been a restored backup silently treated
// as a different project.
//
// It has no consumer in T2, and that is accepted: identity is the one thing that
// cannot be added afterwards, since projects written without an identifier do
// not gain one retroactively.
//
// The order of `Blueprints` is functional, not cosmetic. B.5.1 of T1 paginates
// size groups "in the order the sizes appear in the manifest", and the manifest
// will be produced from the project — so this order decides the order of the
// pages in the PDF. Never sort it on read, never reorder it on write.
//
// `PaperFormatName` is a key, not a `PaperFormat`. Resolving it against the
// calibration is Application's job, and a key the local calibration does not
// know is a diagnostic, not a refusal: a project is a user's data and a
// calibration is a machine's, and one never rejects the other (DEC-056).

/// <summary>
/// The style of a project: the only structural guarantee of visual coherence.
/// </summary>
/// <remarks>
/// A value object with no identifier — a project has exactly one style, and a
/// style does not exist outside its project.
/// <para>
/// It remains a property of the <b>project</b> and is never overridable per
/// blueprint. That half of DEC-006 still stands; only the freeze at creation
/// fell, with DEC-030.
/// </para>
/// </remarks>
/// <param name="Name">Display label. May be empty.</param>
/// <param name="StyleClause">Literal string injected into every resolved prompt. In English (DEC-037).</param>
/// <param name="NegativeClause">Negative prompt shared by the whole project.</param>
/// <param name="Palette">Free descriptor, folded into the style clause.</param>
public sealed record Style(
    string Name,
    string StyleClause,
    string NegativeClause,
    string Palette);

/// <summary>
/// The physical values a project may override, and no others.
/// </summary>
/// <remarks>
/// <para>
/// <b>A closed list of two members, not a dictionary.</b> A free-key
/// <c>overrides</c> object would have been shorter to write and strictly worse:
/// it would make everything the calibration holds overridable, including
/// <c>scaleCorrectionFactor</c>, which DEC-040 explains at length must stay
/// locked. A convention that opens by default is the opposite of what section 0
/// of the T1 specification asks for (DEC-053).
/// </para>
/// <para>
/// Why these two and not others: DEC-040 sorts physical values by <i>who
/// determines them</i>, and makes overridable only what is determined by an
/// object the user owns and replaces — the slot of their commercial bases.
/// <c>gutterMm</c> and <c>silhouetteMarginMm</c> are determined by the user's
/// own hand at the scissors, and Pawnsmith is single-user, so a property of the
/// user is a global property here, not a project one (DEC-052).
/// </para>
/// <para>
/// <b>The domain never sees this type in a calculation.</b> The merge
/// <c>calibration ∪ overrides</c> happens once, in Application, before any call
/// to the layout engine, which receives an already-effective calibration. Not
/// one line of T1 changes as a result — which is the outcome B.2 was written to
/// obtain when it required the engine to read physical values without knowing
/// them.
/// </para>
/// </remarks>
/// <param name="TabWidthMm">Tab width, or null to use the calibration's value.</param>
/// <param name="TabHeightMm">Tab height, or null to use the calibration's value.</param>
public sealed record CalibrationOverrides(
    double? TabWidthMm,
    double? TabHeightMm)
{
    /// <summary>A project that overrides nothing.</summary>
    /// <remarks>
    /// Both members are always written to the file, as explicit nulls: an absent
    /// member and a null member would otherwise be two spellings of the same
    /// thing, and the writer has to pick one.
    /// </remarks>
    public static CalibrationOverrides None { get; } = new(null, null);
}

// A consequence to make visible rather than discover, already announced by
// DEC-040: `tabHeightMm` feeds the cell height of B.5.2, so **the capacity of a
// page now depends on the project**. Two otherwise identical projects with
// different bases do not fit the same number of pawns per page, and the capacity
// indicator of T6 must therefore be computed on the effective calibration, never
// on the calibration file.
