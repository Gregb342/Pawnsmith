using System.Globalization;

using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// The manual mapping between the domain entities and the document types,
/// written out by hand in both directions (DEC-021).
/// </summary>
/// <remarks>
/// <para>
/// It is verbose on purpose. Automatic mapping breaks at run time and is
/// invisible at review, which contradicts the way this project is built
/// (DEC-027) — and here a silent mismatch would not throw, it would write a
/// project file missing a field nobody noticed.
/// </para>
/// <para>
/// <b><see cref="ToDomain(ProjectDocument)"/> recalculates nothing.</b> It
/// builds entities from a document and stops there. The resolved prompt and the
/// misalignment are computed when someone asks for them, from the current
/// clauses, and never at load time (C.3.6).
/// </para>
/// <para>
/// <b>What this mapping assumes, and what it therefore does not check.</b> A
/// document reaching <c>ToDomain</c> has already passed the validation of
/// C.7.1 — step 5 checks UUID forms, known enumeration members and parseable
/// timestamps, and step 8 only then builds the entities. The parsing below is
/// consequently strict and throws: a failure here is a <b>programming error</b>,
/// a caller that skipped the validation, not a bad file. A bad file is refused
/// earlier with a code the user can act on.
/// </para>
/// </remarks>
public static class ProjectMapping
{
    /// <summary>The schema version this code reads and writes.</summary>
    /// <remarks>
    /// Lives here rather than on the entity because it is a property of the
    /// <b>file</b>, not of the project: carrying it on <see cref="Project"/>
    /// would let an in-memory project claim a schema its own writer cannot
    /// produce. Any field added to the schema increments it — there is no such
    /// thing as a compatible addition (DEC-048).
    /// </remarks>
    public const int SupportedVersionSchema = 1;

    /// <summary>Format of every timestamp in the file: ISO 8601, UTC, second precision.</summary>
    /// <remarks>
    /// A project has no time zone (C.3.3), so the instant is converted to UTC
    /// before being written and the <c>Z</c> is literal. Reading uses the same
    /// format, which is what makes the round trip exact whatever the process
    /// time zone.
    /// </remarks>
    private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    // ---- Domaine vers document -------------------------------------------

    public static ProjectDocument ToDocument(this Project project)
    {
        ArgumentNullException.ThrowIfNull(project);

        return new ProjectDocument(
            VersionSchema: SupportedVersionSchema,
            ProjectId: FormatId(project.ProjectId),
            Name: project.Name,
            Universe: project.Universe.ToString(),
            Geometry: project.Geometry.ToString(),
            PaperFormat: project.PaperFormatName,
            Style: project.Style.ToDocument(),
            CalibrationOverrides: project.CalibrationOverrides.ToDocument(),
            Blueprints: [.. project.Blueprints.Select(blueprint => blueprint.ToDocument())],
            CreatedAt: FormatInstant(project.CreatedAt),
            ModifiedAt: FormatInstant(project.ModifiedAt));
    }

    public static StyleDocument ToDocument(this Style style)
    {
        ArgumentNullException.ThrowIfNull(style);

        return new StyleDocument(
            Name: style.Name,
            StyleClause: style.StyleClause,
            NegativeClause: style.NegativeClause,
            Palette: style.Palette);
    }

    public static CalibrationOverridesDocument ToDocument(this CalibrationOverrides overrides)
    {
        ArgumentNullException.ThrowIfNull(overrides);

        return new CalibrationOverridesDocument(overrides.TabWidthMm, overrides.TabHeightMm);
    }

    public static BlueprintDocument ToDocument(this Blueprint blueprint)
    {
        ArgumentNullException.ThrowIfNull(blueprint);

        return new BlueprintDocument(
            Id: FormatId(blueprint.Id),
            Race: blueprint.Race,
            CharacterClass: blueprint.CharacterClass,
            Size: blueprint.Size.ToString(),
            OptionalParameters: blueprint.OptionalParameters,
            Details: blueprint.Details,
            SubjectClause: blueprint.SubjectClause,
            Quantity: blueprint.Quantity,
            Candidates: [.. blueprint.Candidates.Select(candidate => candidate.ToDocument())],
            ElectedCandidateId: blueprint.ElectedCandidateId is { } elected ? FormatId(elected) : null);
    }

    public static CandidateDocument ToDocument(this Candidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        return new CandidateDocument(
            Id: FormatId(candidate.Id),
            // Invariant culture on a ulong is belt and braces rather than a real
            // risk, and it is written anyway: every number in this file is
            // formatted the same way, and an exception to that rule is the kind
            // of thing that gets copied into a decimal one day.
            Seed: candidate.Seed.ToString(CultureInfo.InvariantCulture),
            FramingClauseUsed: candidate.FramingClauseUsed,
            SubjectClauseUsed: candidate.SubjectClauseUsed,
            StyleClauseUsed: candidate.StyleClauseUsed,
            Status: candidate.Status.ToString(),
            PairedImageFile: candidate.PairedImageFile,
            FrontImageFile: candidate.FrontImageFile,
            BackImageFile: candidate.BackImageFile,
            GeneratedAt: FormatInstant(candidate.GeneratedAt));
    }

    // ---- Document vers domaine -------------------------------------------

    public static Project ToDomain(this ProjectDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new Project(
            ProjectId: ParseId(document.ProjectId),
            Name: document.Name,
            Universe: ParseEnum<Universe>(document.Universe),
            Style: document.Style.ToDomain(),
            Geometry: ParseEnum<Geometry>(document.Geometry),
            PaperFormatName: document.PaperFormat,
            CalibrationOverrides: document.CalibrationOverrides.ToDomain(),
            Blueprints: [.. document.Blueprints.Select(blueprint => blueprint.ToDomain())],
            CreatedAt: ParseInstant(document.CreatedAt),
            ModifiedAt: ParseInstant(document.ModifiedAt));
    }

    public static Style ToDomain(this StyleDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new Style(
            Name: document.Name,
            StyleClause: document.StyleClause,
            NegativeClause: document.NegativeClause,
            Palette: document.Palette);
    }

    public static CalibrationOverrides ToDomain(this CalibrationOverridesDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new CalibrationOverrides(document.TabWidthMm, document.TabHeightMm);
    }

    public static Blueprint ToDomain(this BlueprintDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new Blueprint(
            Id: ParseId(document.Id),
            Race: document.Race,
            CharacterClass: document.CharacterClass,
            Size: ParseEnum<Size>(document.Size),
            OptionalParameters: document.OptionalParameters,
            Details: document.Details,
            SubjectClause: document.SubjectClause,
            Quantity: document.Quantity,
            Candidates: [.. document.Candidates.Select(candidate => candidate.ToDomain())],
            ElectedCandidateId: document.ElectedCandidateId is { } elected ? ParseId(elected) : null);
    }

    public static Candidate ToDomain(this CandidateDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new Candidate(
            Id: ParseId(document.Id),
            Seed: ulong.Parse(document.Seed, CultureInfo.InvariantCulture),
            FramingClauseUsed: document.FramingClauseUsed,
            SubjectClauseUsed: document.SubjectClauseUsed,
            StyleClauseUsed: document.StyleClauseUsed,
            Status: ParseEnum<CandidateStatus>(document.Status),
            PairedImageFile: document.PairedImageFile,
            FrontImageFile: document.FrontImageFile,
            BackImageFile: document.BackImageFile,
            GeneratedAt: ParseInstant(document.GeneratedAt));
    }

    // ---- Conversions élémentaires ----------------------------------------

    /// <summary>An identifier in the one form the file accepts: lower case, hyphenated.</summary>
    private static string FormatId(Guid id) => id.ToString("D", CultureInfo.InvariantCulture);

    /// <summary>
    /// Reads an identifier in that exact form, and no other.
    /// </summary>
    /// <remarks>
    /// <c>ParseExact</c> with <c>"D"</c> rather than <c>Guid.Parse</c>, which
    /// also accepts braces, parentheses and the form without hyphens. Accepting
    /// three spellings of one identifier would make two projects differ byte for
    /// byte while meaning the same thing, and C.3.3 asks for the opposite.
    /// </remarks>
    private static Guid ParseId(string value) =>
        Guid.ParseExact(value, "D");

    private static string FormatInstant(DateTimeOffset instant) =>
        instant.ToUniversalTime().ToString(TimestampFormat, CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseInstant(string value) =>
        DateTimeOffset.ParseExact(
            value,
            TimestampFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

    /// <summary>
    /// Reads an enumeration member by its exact name.
    /// </summary>
    /// <remarks>
    /// Case-sensitive on purpose. <c>"medium"</c> is not a value this
    /// application ever writes, so accepting it would mean silently repairing a
    /// hand-edited file — and writing it back in a third spelling.
    /// <para>
    /// <b>The round trip through <c>ToString</c> is what refuses an ordinal</b>,
    /// and it is not decoration. <c>Enum.TryParse</c> happily reads <c>"1"</c>
    /// as the member whose underlying value is 1, and <c>Enum.IsDefined</c> then
    /// says yes — so both of the obvious guards let it through. C.3.3 forbids
    /// ordinals for a precise reason: inserting a member into the enumeration
    /// shifts every one after it, and every project file written with a number
    /// silently starts meaning something else. Requiring that the text be
    /// exactly the member's own name closes it.
    /// </para>
    /// </remarks>
    private static TEnum ParseEnum<TEnum>(string value)
        where TEnum : struct, Enum
    {
        if (!Enum.TryParse(value, ignoreCase: false, out TEnum parsed)
            || !string.Equals(parsed.ToString(), value, StringComparison.Ordinal))
        {
            throw new ManifestException(
                $"'{value}' is not a member of {typeof(TEnum).Name}. " +
                $"Known members are: {string.Join(", ", Enum.GetNames<TEnum>())}.");
        }

        return parsed;
    }
}
