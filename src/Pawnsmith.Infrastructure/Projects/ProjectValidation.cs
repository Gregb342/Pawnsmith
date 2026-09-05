using System.Globalization;

using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// Everything a <c>project.json</c> can contradict on its own — steps 4 to 7 of
/// C.7.1.
/// </summary>
/// <remarks>
/// <para>
/// This is the <b>intrinsic</b> half of the split DEC-056 draws, and it is
/// blocking everywhere: on load, on save, on import. What it checks is wrong on
/// every machine — a missing field, an unknown enumeration member, a negative
/// override — so no calibration and no disk can make it right.
/// </para>
/// <para>
/// The other half, the <b>relational</b> one, is not here at all. Whether the
/// paper format is known locally, whether an image really sits on this disk,
/// whether a tab fits this user's pawns: those confront a project with an
/// environment, they produce diagnostics, and they block nothing in T2. <b>A
/// project opens to be corrected, not to crash.</b>
/// </para>
/// <para>
/// Every refusal names the offending field, down to its index, because a
/// malformed project is almost always your own — the opposite of the archive,
/// whose single opaque code is deliberate (C.11).
/// </para>
/// <para>
/// Why null is checked on members the type declares non-nullable: the
/// deserialiser fills what the file provides and leaves the rest at null,
/// whatever the annotation says. A missing <c>name</c> arrives here as a null
/// <c>string</c>, and the compiler has no idea.
/// </para>
/// </remarks>
public static class ProjectValidation
{
    /// <summary>Longest project name accepted, in characters (C.3.4).</summary>
    public const int MaxNameLength = 120;

    /// <summary>Runs steps 4 to 7 of C.7.1, in that order.</summary>
    /// <remarks>
    /// The order is not a matter of taste: each step assumes the one before it
    /// succeeded, and inverting two of them opens a hole. The version is checked
    /// first because a document of an unknown schema must not be interpreted at
    /// all; the paths are checked before anything reaches the disk.
    /// </remarks>
    /// <exception cref="ProjectException">Any intrinsic rule is broken.</exception>
    public static void Validate(ProjectDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        ValidateVersion(document.VersionSchema);
        ValidateRoot(document);
        ValidateStyle(document.Style);
        ValidateOverrides(document.CalibrationOverrides);

        for (int index = 0; index < document.Blueprints.Count; index++)
        {
            ValidateBlueprint(document.Blueprints[index], $"blueprints[{index}]");
        }
    }

    // ---- Étape 4 : la version de schéma ----------------------------------

    private static void ValidateVersion(int versionSchema)
    {
        if (versionSchema > ProjectMapping.SupportedVersionSchema)
        {
            // Refused outright, and the motive is not caution - it is data loss
            // (C.6.1). A v1 reader opening a v2 project would ignore the fields
            // it does not know; the v1 writer then writes what its document type
            // holds, which is to say without them. Opening and saving would
            // silently amputate the project of everything v2 added, and the loss
            // only surfaces when it is reopened with the newer build.
            throw new ProjectException(
                ProjectErrorCode.SchemaTooRecent,
                $"This project declares schema version {versionSchema}, and this build " +
                $"supports version {ProjectMapping.SupportedVersionSchema}. It is refused rather " +
                "than partly read: saving it back would drop everything the newer version added.");
        }

        if (versionSchema != ProjectMapping.SupportedVersionSchema)
        {
            // An older version is an empty case in v1 - there is only one
            // version, so there is nothing to migrate and no migration engine is
            // written (C.6.2). Refusing an impossible number is not a migration
            // engine; it is refusing to interpret a file we cannot read.
            throw Invalid(
                "versionSchema",
                $"{versionSchema} is not a schema version this build knows. " +
                "Migration is an explicit, backed-up, step-by-step operation, never a silent " +
                "conversion at load time.");
        }
    }

    // ---- Étape 5 : la structure -------------------------------------------

    private static void ValidateRoot(ProjectDocument document)
    {
        ValidateIdentifier(document.ProjectId, "projectId");
        ValidateRequiredText(document.Name, "name");

        if (document.Name.Trim().Length == 0 || document.Name.Trim().Length > MaxNameLength)
        {
            throw Invalid("name", $"it must hold 1 to {MaxNameLength} characters once trimmed.");
        }

        ValidateEnumMember<Universe>(document.Universe, "universe");
        ValidateEnumMember<Geometry>(document.Geometry, "geometry");
        ValidateRequiredText(document.PaperFormat, "paperFormat");

        // Not checked against the calibration, and that is the point: the
        // calibration is a machine's data and the project is a user's, so one
        // never refuses the other (DEC-056). An unknown format becomes a
        // diagnostic at load and an error when someone asks for a sheet.
        ValidateRequired(document.Style, "style");
        ValidateRequired(document.CalibrationOverrides, "calibrationOverrides");
        ValidateRequired(document.Blueprints, "blueprints");
        ValidateTimestamp(document.CreatedAt, "createdAt");
        ValidateTimestamp(document.ModifiedAt, "modifiedAt");
    }

    private static void ValidateStyle(StyleDocument style)
    {
        // Every member may be empty and none may be absent: an absent member and
        // an empty one would be two spellings of one thing.
        ValidateRequired(style.Name, "style.name");
        ValidateRequired(style.StyleClause, "style.styleClause");
        ValidateRequired(style.NegativeClause, "style.negativeClause");
        ValidateRequired(style.Palette, "style.palette");
    }

    private static void ValidateBlueprint(BlueprintDocument blueprint, string field)
    {
        ValidateRequired(blueprint, field);
        ValidateIdentifier(blueprint.Id, $"{field}.id");
        ValidateRequiredText(blueprint.Race, $"{field}.race");
        ValidateRequiredText(blueprint.CharacterClass, $"{field}.characterClass");
        ValidateEnumMember<Size>(blueprint.Size, $"{field}.size");
        ValidateRequired(blueprint.OptionalParameters, $"{field}.optionalParameters");
        ValidateRequired(blueprint.Details, $"{field}.details");
        ValidateRequired(blueprint.SubjectClause, $"{field}.subjectClause");
        ValidateRequired(blueprint.Candidates, $"{field}.candidates");

        if (blueprint.Quantity < 1)
        {
            throw Invalid($"{field}.quantity", $"{blueprint.Quantity} is not at least 1.");
        }

        for (int index = 0; index < blueprint.Candidates.Count; index++)
        {
            ValidateCandidate(blueprint.Candidates[index], $"{field}.candidates[{index}]");
        }

        ValidateElection(blueprint, field);
    }

    private static void ValidateElection(BlueprintDocument blueprint, string field)
    {
        if (blueprint.ElectedCandidateId is not { } elected)
        {
            return;
        }

        ValidateIdentifier(elected, $"{field}.electedCandidateId");

        // "Of this blueprint", not "of this project". Referential integrity is
        // what T2 owes; what happens to the previous elect when a new one is
        // chosen is a business rule, so question B of chapter 16, so T3.
        bool belongs = blueprint.Candidates.Any(
            candidate => string.Equals(candidate.Id, elected, StringComparison.Ordinal));

        if (!belongs)
        {
            throw Invalid(
                $"{field}.electedCandidateId",
                $"'{elected}' names no candidate of this blueprint.");
        }
    }

    private static void ValidateCandidate(CandidateDocument candidate, string field)
    {
        ValidateRequired(candidate, field);
        ValidateIdentifier(candidate.Id, $"{field}.id");
        ValidateSeed(candidate.Seed, $"{field}.seed");
        ValidateRequired(candidate.FramingClauseUsed, $"{field}.framingClauseUsed");
        ValidateRequired(candidate.SubjectClauseUsed, $"{field}.subjectClauseUsed");
        ValidateRequired(candidate.StyleClauseUsed, $"{field}.styleClauseUsed");
        ValidateEnumMember<CandidateStatus>(candidate.Status, $"{field}.status");
        ValidateTimestamp(candidate.GeneratedAt, $"{field}.generatedAt");

        // ---- Étape 6 : les chemins, avant tout accès disque ----------------
        ValidateOptionalPath(candidate.PairedImageFile, $"{field}.pairedImageFile");
        ValidateOptionalPath(candidate.FrontImageFile, $"{field}.frontImageFile");
        ValidateOptionalPath(candidate.BackImageFile, $"{field}.backImageFile");
    }

    private static void ValidateOptionalPath(string? path, string field)
    {
        if (path is not null)
        {
            ImagePathRules.Validate(path, field);
        }
    }

    // ---- Étape 7 : les surcharges, dans leur seule moitié intrinsèque -----

    private static void ValidateOverrides(CalibrationOverridesDocument overrides)
    {
        ValidateOverride(overrides.TabWidthMm, "calibrationOverrides.tabWidthMm");
        ValidateOverride(overrides.TabHeightMm, "calibrationOverrides.tabHeightMm");
    }

    private static void ValidateOverride(double? value, string field)
    {
        if (value is not { } number)
        {
            return;
        }

        // Only what is wrong in itself, on every machine (DEC-056). Whether the
        // value fits this user's pawns is relational: it makes a diagnostic, and
        // it becomes an error when the sheet is calculated, where DEC-038 put
        // that check. The v1.0 specification refused it here, which made a
        // perfectly coherent archive unopenable just because its author owns
        // different bases - the very scenario the Share profile exists to make
        // pleasant.
        if (!double.IsFinite(number) || number <= 0)
        {
            throw new ProjectException(
                ProjectErrorCode.OverrideInvalid,
                $"The calibration override {field} is {number.ToString(CultureInfo.InvariantCulture)}, " +
                "and an override must be a finite, strictly positive number.");
        }
    }

    // ---- Les vérifications élémentaires -----------------------------------

    private static void ValidateRequired<T>(T? value, string field)
    {
        if (value is null)
        {
            throw Invalid(field, "it is absent.");
        }
    }

    private static void ValidateRequiredText(string? value, string field)
    {
        ValidateRequired(value, field);

        if (value!.Trim().Length == 0)
        {
            throw Invalid(field, "it is empty.");
        }
    }

    private static void ValidateIdentifier(string? value, string field)
    {
        ValidateRequired(value, field);

        if (!Guid.TryParseExact(value, "D", out _))
        {
            throw Invalid(
                field,
                $"'{value}' is not a UUID in the lower-case hyphenated form this schema uses.");
        }
    }

    private static void ValidateSeed(string? value, string field)
    {
        ValidateRequired(value, field);

        if (!ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            throw Invalid(
                field,
                $"'{value}' is not an unsigned 64-bit integer written in decimal. " +
                "The seed is a string precisely so that it can hold one.");
        }
    }

    private static void ValidateTimestamp(string? value, string field)
    {
        ValidateRequired(value, field);

        if (!DateTimeOffset.TryParseExact(
                value,
                "yyyy-MM-dd'T'HH:mm:ss'Z'",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out _))
        {
            throw Invalid(field, $"'{value}' is not an ISO 8601 UTC instant such as 2026-09-01T14:22:07Z.");
        }
    }

    private static void ValidateEnumMember<TEnum>(string? value, string field)
        where TEnum : struct, Enum
    {
        ValidateRequired(value, field);

        // The round trip through ToString is what refuses an ordinal: TryParse
        // reads "1" as the member whose underlying value is 1, and IsDefined
        // then agrees. C.3.3 forbids ordinals because inserting a member shifts
        // every one after it, and every file written with a number silently
        // starts meaning something else.
        if (!Enum.TryParse(value, ignoreCase: false, out TEnum parsed)
            || !string.Equals(parsed.ToString(), value, StringComparison.Ordinal))
        {
            throw Invalid(
                field,
                $"'{value}' is not a member of {typeof(TEnum).Name}. " +
                $"Known members are: {string.Join(", ", Enum.GetNames<TEnum>())}.");
        }
    }

    private static ProjectException Invalid(string field, string why) =>
        new(ProjectErrorCode.Invalid, $"The project field '{field}' is refused: {why}");
}
