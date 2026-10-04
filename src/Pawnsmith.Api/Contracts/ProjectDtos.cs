using Pawnsmith.Application.Blueprints;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Projects;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Api.Contracts;

/// <summary>A project as the API shows it, with what it says about itself and its derived values.</summary>
/// <param name="Folder">How the API addresses it (DEC-083).</param>
/// <param name="Diagnostics">What does not match this machine — kind and field, never a message (DEC-084).</param>
/// <param name="MisalignmentKnown">False when no workflow is configured: misalignment is then unknown, not absent (§G.4).</param>
public sealed record ProjectDto(
    string Folder,
    Guid ProjectId,
    string Name,
    Universe Universe,
    Geometry Geometry,
    string PaperFormat,
    StyleDto Style,
    OverridesDto CalibrationOverrides,
    IReadOnlyList<BlueprintDto> Blueprints,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    IReadOnlyList<DiagnosticDto> Diagnostics,
    bool MisalignmentKnown,
    bool UniverseAndStyleFrozen);

public sealed record StyleDto(string Name, string StyleClause, string NegativeClause, string Palette);

/// <summary>The closed list of DEC-053: two members, null meaning "the calibration's value".</summary>
public sealed record OverridesDto(double? TabWidthMm, double? TabHeightMm);

/// <param name="OptionalParameters">Sorted by key, ordinal: a dictionary has no order of its own (§G.4).</param>
/// <param name="ResolvedPrompt">Derived, read-only (DEC-028); null when the framing clause is unknown.</param>
public sealed record BlueprintDto(
    Guid Id,
    string Race,
    string CharacterClass,
    Size Size,
    IReadOnlyList<ParameterDto> OptionalParameters,
    string Details,
    string SubjectClause,
    bool SubjectClauseEdited,
    string? ResolvedPrompt,
    int Quantity,
    Guid? ElectedCandidateId,
    IReadOnlyList<CandidateDto> Candidates);

public sealed record ParameterDto(string Key, string Value);

/// <param name="Seed">A decimal string, never a JSON number: past 2^53 <c>JSON.parse</c> rounds it (§C.3.4).</param>
/// <param name="MisalignedClauses">The clauses that moved; empty when aligned; null when unknown (§G.4).</param>
/// <param name="PairedImage">The stored path, relative to the project; served under <c>/api/projects/{folder}/</c>.</param>
public sealed record CandidateDto(
    Guid Id,
    string Seed,
    CandidateStatus Status,
    IReadOnlyList<ClauseKind>? MisalignedClauses,
    string SubjectClauseUsed,
    string StyleClauseUsed,
    string? PairedImage,
    string? FrontImage,
    string? BackImage,
    DateTimeOffset GeneratedAt);

public sealed record DiagnosticDto(string Kind, string Field);

/// <summary>One line of the project list: the project, or the code that stops it loading.</summary>
public sealed record ProjectListingDto(
    string Folder,
    Guid? ProjectId,
    string? Name,
    DateTimeOffset? ModifiedAt,
    string? ErrorCode);

public sealed record CreateProjectRequest(string Name, Universe Universe, Geometry Geometry, string PaperFormat);

/// <summary>Everything of a project that is not its blueprints. Nothing is locked (DEC-055).</summary>
public sealed record DuplicateProjectRequest(string Name, StyleDto? Style);

public sealed record ProjectSettingsRequest(
    string Name,
    Universe Universe,
    Geometry Geometry,
    string PaperFormat,
    StyleDto Style,
    OverridesDto CalibrationOverrides);

/// <summary>The manual mappings of the project DTOs (DEC-021).</summary>
/// <summary>
/// What a project's DTO needs beyond the project: the framing clause, for the
/// misalignment (DEC-082), and the composer, for <c>subjectClauseEdited</c>
/// (DEC-109).
/// </summary>
/// <param name="FramingClause">The configured workflow's clause, or <c>null</c> when no workflow is read.</param>
/// <param name="Composer">The composer of the application.</param>
public sealed record MappingContext(string? FramingClause, IPromptComposer Composer);

public static class ProjectMapping
{
    /// <param name="folder">The folder the project was read from.</param>
    /// <param name="loaded">The project and its diagnostics.</param>
    /// <param name="context">The framing clause in force, or null when unknown, and the composer.</param>
    public static ProjectDto ToDto(this LoadedProjectResult loaded, string folder, MappingContext context)
    {
        string? framingClause = context.FramingClause;

        Project project = loaded.Project;

        return new ProjectDto(
            Folder: folder,
            ProjectId: project.ProjectId,
            Name: project.Name,
            Universe: project.Universe,
            Geometry: project.Geometry,
            PaperFormat: project.PaperFormatName,
            Style: project.Style.ToDto(),
            CalibrationOverrides: new OverridesDto(project.CalibrationOverrides.TabWidthMm, project.CalibrationOverrides.TabHeightMm),
            Blueprints: [.. project.Blueprints.Select(blueprint => blueprint.ToDto(project.Universe, project.Style, context))],
            CreatedAt: project.CreatedAt,
            ModifiedAt: project.ModifiedAt,
            Diagnostics: [.. loaded.Diagnostics.Select(diagnostic => new DiagnosticDto(diagnostic.Kind, diagnostic.Field))],
            MisalignmentKnown: framingClause is not null,
            UniverseAndStyleFrozen: ProjectSettingsEditor.IsFrozen(project));
    }

    public static StyleDto ToDto(this Style style) => new(style.Name, style.StyleClause, style.NegativeClause, style.Palette);

    public static Style ToDomain(this StyleDto style) => new(style.Name, style.StyleClause, style.NegativeClause, style.Palette);

    public static BlueprintDto ToDto(this Blueprint blueprint, Universe universe, Style style, MappingContext context) => new(
        Id: blueprint.Id,
        Race: blueprint.Race,
        CharacterClass: blueprint.CharacterClass,
        Size: blueprint.Size,
        OptionalParameters: [.. blueprint.OptionalParameters
            .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
            .Select(parameter => new ParameterDto(parameter.Key, parameter.Value))],
        Details: blueprint.Details,
        SubjectClause: blueprint.SubjectClause,
        SubjectClauseEdited: BlueprintEditor.IsSubjectClauseEdited(blueprint, universe, context.Composer),
        ResolvedPrompt: context.FramingClause is null ? null : ResolvedPrompt.From(context.FramingClause, blueprint.SubjectClause, style.StyleClause),
        Quantity: blueprint.Quantity,
        ElectedCandidateId: blueprint.ElectedCandidateId,
        Candidates: [.. blueprint.Candidates.Select(candidate => candidate.ToDto(blueprint, style, context.FramingClause))]);

    public static CandidateDto ToDto(this Candidate candidate, Blueprint blueprint, Style style, string? framingClause) => new(
        Id: candidate.Id,
        Seed: candidate.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture),
        Status: candidate.Status,

        // In the enumeration's order, so that two answers list the clauses
        // the same way; null when the framing clause is unknown.
        MisalignedClauses: framingClause is null
            ? null
            : [.. Enum.GetValues<ClauseKind>().Where(Misalignment.Of(candidate, blueprint, style, framingClause).Contains)],

        // The subject and style clauses frozen at generation, for "what
        // changed since this image". Not the framing clause: it never appears
        // in the interface (DEC-029); a misaligned framing is named, not shown.
        SubjectClauseUsed: candidate.SubjectClauseUsed,
        StyleClauseUsed: candidate.StyleClauseUsed,
        PairedImage: candidate.PairedImageFile,
        FrontImage: candidate.FrontImageFile,
        BackImage: candidate.BackImageFile,
        GeneratedAt: candidate.GeneratedAt);

    public static ProjectListingDto ToDto(this ProjectListing listing) => new(
        listing.Folder,
        listing.Project?.ProjectId,
        listing.Project?.Name,
        listing.Project?.ModifiedAt,
        listing.ErrorCode);

    /// <summary>The project with its settings replaced. No rule: nothing is locked after creation (DEC-055).</summary>
    public static ProjectSettings ToDomain(this ProjectSettingsRequest settings) => new(
        Name: settings.Name,
        Universe: settings.Universe,
        Geometry: settings.Geometry,
        PaperFormatName: settings.PaperFormat,
        Style: settings.Style.ToDomain(),
        CalibrationOverrides: new CalibrationOverrides(settings.CalibrationOverrides.TabWidthMm, settings.CalibrationOverrides.TabHeightMm));
}
