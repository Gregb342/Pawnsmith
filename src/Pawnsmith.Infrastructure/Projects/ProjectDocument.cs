using System.Text.Json.Serialization;

namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// The shape of <c>project.json</c>, version 1 — one record per object of the
/// schema of C.3.4.
/// </summary>
/// <remarks>
/// <para>
/// <b>These types exist so that the domain never learns about JSON.</b> A.3
/// forbids <c>Pawnsmith.Domain</c> from referencing anything, which rules out a
/// serialisation attribute on an entity, and DEC-021 rules out automatic
/// mapping. So Infrastructure declares its own document types and maps by hand,
/// in both directions. It is the motif T1 already uses for the calibration and
/// manifest readers, repeated deliberately.
/// </para>
/// <para>
/// <b>The order in which members are declared is the order they are written to
/// the file</b>, and C.3.3 requires that order to be fixed: two saves of an
/// unchanged project must produce the same bytes. Reordering these properties
/// changes every project file, so do not do it for tidiness.
/// </para>
/// <para>
/// <b>Where the "no derived value is serialised" guarantee lives.</b> Not in a
/// rule anyone has to remember: <see cref="CandidateDocument"/> simply has no
/// <c>resolvedPrompt</c> and no <c>misaligned</c> member, so there is nothing to
/// forget to leave out — it is not expressible. The textual test on the produced
/// file is a net, not the mechanism (C.3.6).
/// </para>
/// <para>
/// <b>Every key is pinned by its own attribute</b> rather than produced by a
/// camel-case naming policy. The policy would be one line instead of thirty-seven
/// attributes, and it would be a hidden convention: renaming a C# property would
/// silently change the format of every project file already on a disk. The schema
/// of C.3.4 is a contract, so the contract is written down. It is also the only
/// way <c>characterClass</c> reads unambiguously.
/// </para>
/// <para>
/// Identifiers, timestamps, enumerations and the seed are all <b>strings</b>
/// here, even though the domain has real types for them. That is the point of a
/// document type: it owns the wire format, and the mapping owns the conversion.
/// </para>
/// </remarks>
/// <param name="VersionSchema">Always 1 for now. Any field added to this schema increments it (DEC-048).</param>
/// <param name="ProjectId">UUID v4, lower case, canonical hyphenated form.</param>
/// <param name="Name">Display label, 1 to 120 characters once trimmed.</param>
/// <param name="Universe">Name of a <c>Universe</c> member.</param>
/// <param name="Geometry">Name of a <c>Geometry</c> member — three of them since DEC-039.</param>
/// <param name="PaperFormat">A key of the calibration's paper formats. Not resolved here (DEC-056).</param>
/// <param name="Style">The project's style. Always present.</param>
/// <param name="CalibrationOverrides">Always written, with explicit nulls (DEC-053).</param>
/// <param name="Blueprints">May be empty. <b>The order is significant</b> — see the note below.</param>
/// <param name="CreatedAt">ISO 8601, UTC, <c>Z</c> suffix.</param>
/// <param name="ModifiedAt">Same, rewritten at every save.</param>
public sealed record ProjectDocument(
    [property: JsonPropertyName("versionSchema")]
    int VersionSchema,
    [property: JsonPropertyName("projectId")]
    string ProjectId,
    [property: JsonPropertyName("name")]
    string Name,
    [property: JsonPropertyName("universe")]
    string Universe,
    [property: JsonPropertyName("geometry")]
    string Geometry,
    [property: JsonPropertyName("paperFormat")]
    string PaperFormat,
    [property: JsonPropertyName("style")]
    StyleDocument Style,
    [property: JsonPropertyName("calibrationOverrides")]
    CalibrationOverridesDocument CalibrationOverrides,
    [property: JsonPropertyName("blueprints")]
    IReadOnlyList<BlueprintDocument> Blueprints,
    [property: JsonPropertyName("createdAt")]
    string CreatedAt,
    [property: JsonPropertyName("modifiedAt")]
    string ModifiedAt);

// The order of Blueprints is functional, not cosmetic. B.5.1 of T1 paginates
// size groups "in the order the sizes appear in the manifest", and the manifest
// will be produced from the project - so this order decides the order of the
// pages in the PDF. Never sort it on read, never reorder it on write.

/// <summary>The <c>style</c> object: a value object with no identifier.</summary>
/// <remarks>
/// Every member may be empty, and none may be absent. An absent member and an
/// empty one would otherwise be two spellings of the same thing, and the writer
/// has to pick one.
/// </remarks>
/// <param name="Name">Display label.</param>
/// <param name="StyleClause">Injected into every resolved prompt. In English (DEC-037).</param>
/// <param name="NegativeClause">Negative prompt shared by the project.</param>
/// <param name="Palette">Free descriptor.</param>
public sealed record StyleDocument(
    [property: JsonPropertyName("name")]
    string Name,
    [property: JsonPropertyName("styleClause")]
    string StyleClause,
    [property: JsonPropertyName("negativeClause")]
    string NegativeClause,
    [property: JsonPropertyName("palette")]
    string Palette);

/// <summary>The <c>calibrationOverrides</c> object: a closed list of two.</summary>
/// <remarks>
/// Not a dictionary, and that is the whole decision (DEC-053). A free-key object
/// would make everything the calibration holds overridable, including
/// <c>scaleCorrectionFactor</c>, which DEC-040 explains at length must stay
/// locked. Both members are always written, as explicit nulls when unset.
/// </remarks>
/// <param name="TabWidthMm">Null means "use the calibration's value".</param>
/// <param name="TabHeightMm">Same.</param>
public sealed record CalibrationOverridesDocument(
    [property: JsonPropertyName("tabWidthMm")]
    double? TabWidthMm,
    [property: JsonPropertyName("tabHeightMm")]
    double? TabHeightMm);

/// <summary>One entry of the <c>blueprints</c> array.</summary>
/// <param name="Id">UUID v4.</param>
/// <param name="Race">Not empty once trimmed.</param>
/// <param name="CharacterClass">Not empty once trimmed. Named this way because <c>class</c> is a C# keyword.</param>
/// <param name="Size">Name of a <c>Size</c> member.</param>
/// <param name="OptionalParameters">Catalogue keys and values. Keys are validated against nothing in T2.</param>
/// <param name="Details">Free text. May be empty.</param>
/// <param name="SubjectClause">Stored and editable (DEC-028). May be empty.</param>
/// <param name="Quantity">At least one.</param>
/// <param name="Candidates">May be empty. Order significant, for display.</param>
/// <param name="ElectedCandidateId">Must name a candidate <b>of this blueprint</b>, or be null.</param>
public sealed record BlueprintDocument(
    [property: JsonPropertyName("id")]
    string Id,
    [property: JsonPropertyName("race")]
    string Race,
    [property: JsonPropertyName("characterClass")]
    string CharacterClass,
    [property: JsonPropertyName("size")]
    string Size,
    [property: JsonPropertyName("optionalParameters")]
    IReadOnlyDictionary<string, string> OptionalParameters,
    [property: JsonPropertyName("details")]
    string Details,
    [property: JsonPropertyName("subjectClause")]
    string SubjectClause,
    [property: JsonPropertyName("quantity")]
    int Quantity,
    [property: JsonPropertyName("candidates")]
    IReadOnlyList<CandidateDocument> Candidates,
    [property: JsonPropertyName("electedCandidateId")]
    string? ElectedCandidateId);

/// <summary>One entry of a blueprint's <c>candidates</c> array.</summary>
/// <remarks>
/// <b>The seed is a decimal string, not a number.</b> ComfyUI seeds run to
/// 2^64-1, and a JSON number is read by <c>JSON.parse</c> as an IEEE-754 double:
/// past 2^53 the value is silently rounded. The front end is React (DEC-018), so
/// the seed will pass through <c>JSON.parse</c> in T6, and a rounded seed is a
/// generation that cannot be replayed — a defect that only shows the day someone
/// replays one, which is to say never during a test.
/// </remarks>
/// <param name="Id">UUID v4. Names the image files on disk.</param>
/// <param name="Seed">Unsigned 64-bit integer, written in decimal.</param>
/// <param name="FramingClauseUsed">Frozen at generation time (DEC-049).</param>
/// <param name="SubjectClauseUsed">Same.</param>
/// <param name="StyleClauseUsed">Same.</param>
/// <param name="Status">Name of a <c>CandidateStatus</c> member.</param>
/// <param name="PairedImageFile">Relative path under <c>images/</c>, or null.</param>
/// <param name="FrontImageFile">Same. Null before the cut-out of T5 has run.</param>
/// <param name="BackImageFile">Same.</param>
/// <param name="GeneratedAt">ISO 8601, UTC, <c>Z</c> suffix.</param>
public sealed record CandidateDocument(
    [property: JsonPropertyName("id")]
    string Id,
    [property: JsonPropertyName("seed")]
    string Seed,
    [property: JsonPropertyName("framingClauseUsed")]
    string FramingClauseUsed,
    [property: JsonPropertyName("subjectClauseUsed")]
    string SubjectClauseUsed,
    [property: JsonPropertyName("styleClauseUsed")]
    string StyleClauseUsed,
    [property: JsonPropertyName("status")]
    string Status,
    [property: JsonPropertyName("pairedImageFile")]
    string? PairedImageFile,
    [property: JsonPropertyName("frontImageFile")]
    string? FrontImageFile,
    [property: JsonPropertyName("backImageFile")]
    string? BackImageFile,
    [property: JsonPropertyName("generatedAt")]
    string GeneratedAt);
