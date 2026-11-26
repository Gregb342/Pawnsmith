using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Application.Blueprints;

/// <summary>
/// The fields of a blueprint the user fills in. Everything except the clause,
/// the candidates and the election.
/// </summary>
/// <param name="Race">Mandatory.</param>
/// <param name="CharacterClass">Mandatory.</param>
/// <param name="Size">The grouping key of the sheet, not a prompt input.</param>
/// <param name="OptionalParameters">Catalogue keys and values. Not validated against the catalogue (DEC-056).</param>
/// <param name="Details">Free text.</param>
/// <param name="Quantity">Copies on the sheet. At least one — checked by the saver, which names the field.</param>
public sealed record BlueprintFields(
    string Race,
    string CharacterClass,
    Size Size,
    IReadOnlyDictionary<string, string> OptionalParameters,
    string Details,
    int Quantity);

/// <summary>
/// A project after one of its blueprints changed, and what the composer said
/// on the way.
/// </summary>
/// <param name="Project">The project, with the blueprint replaced in place. Not yet saved.</param>
/// <param name="Blueprint">The blueprint as it now is.</param>
/// <param name="Diagnostics">Composition diagnostics, when a composition happened. Empty otherwise.</param>
public sealed record EditedProject(
    Project Project,
    Blueprint Blueprint,
    IReadOnlyList<CompositionDiagnostic> Diagnostics);

/// <summary>
/// The use case that owns the subject clause of a blueprint: composes it at
/// creation, recomposes it while it has not been edited, and stores an edit
/// (§D.7).
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the one place the recomposition rule of DEC-067 lives</b>, and it
/// is in Application rather than in the repository because DEC-055 says so:
/// <c>SaveAsync</c> never receives the state before the change, and a rule that
/// needs it belongs to a use case that holds both. The CLI, and later the API,
/// go through here and never patch a blueprint's fields on their own.
/// </para>
/// <para>
/// Everything here is pure: a project in, a project out. Saving is the
/// caller's business, through the repository, so that the rule can be tested
/// without a disk and so that <c>SaveAsync</c> stays the plain write DEC-055
/// wants it to be.
/// </para>
/// </remarks>
public static class BlueprintEditor
{
    /// <summary>Adds a blueprint whose clause is composed from its fields.</summary>
    /// <remarks>
    /// Appended last: the order of the blueprints decides the order of the
    /// pages, and a new blueprint has no reason to jump the queue.
    /// </remarks>
    public static EditedProject Add(Project project, BlueprintFields fields, IPromptComposer composer)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(composer);

        Blueprint blueprint = new(
            Id: Guid.NewGuid(),
            Race: fields.Race,
            CharacterClass: fields.CharacterClass,
            Size: fields.Size,
            OptionalParameters: fields.OptionalParameters,
            Details: fields.Details,
            SubjectClause: string.Empty,
            Quantity: fields.Quantity,
            Candidates: [],
            ElectedCandidateId: null);

        ComposedSubject composed = composer.ComposeSubject(blueprint, project.Universe);
        blueprint = blueprint with { SubjectClause = composed.Clause };

        Project updated = project with { Blueprints = [.. project.Blueprints, blueprint] };

        return new EditedProject(updated, blueprint, composed.Diagnostics);
    }

    /// <summary>
    /// Replaces the fields of a blueprint, and recomposes its clause if — and
    /// only if — nobody had edited it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The edit is <b>deduced, never stored</b> (DEC-067). The stored clause is
    /// compared to what the composer produces for the <i>old</i> fields. Equal:
    /// nobody touched it, so it follows the new fields. Different: the user
    /// wrote it, and it is left exactly as it is. No flag on the blueprint,
    /// so <c>versionSchema</c> stays at 1.
    /// </para>
    /// <para>
    /// The comparison fails on the safe side. If the template or the catalogue
    /// changed since the clause was composed, the old-fields composition no
    /// longer matches, the clause is treated as edited, and nothing is
    /// overwritten. Being wrong costs a missed recomposition, never a user's
    /// text.
    /// </para>
    /// </remarks>
    /// <exception cref="BlueprintRuleException"><c>BLUEPRINT_NOT_FOUND</c>.</exception>
    public static EditedProject UpdateFields(
        Project project,
        Guid blueprintId,
        BlueprintFields fields,
        IPromptComposer composer)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(fields);
        ArgumentNullException.ThrowIfNull(composer);

        Blueprint before = Find(project, blueprintId);

        Blueprint after = before with
        {
            Race = fields.Race,
            CharacterClass = fields.CharacterClass,
            Size = fields.Size,
            OptionalParameters = fields.OptionalParameters,
            Details = fields.Details,
            Quantity = fields.Quantity,
        };

        IReadOnlyList<CompositionDiagnostic> diagnostics = [];

        if (IsUnedited(before, project.Universe, composer))
        {
            ComposedSubject composed = composer.ComposeSubject(after, project.Universe);
            after = after with { SubjectClause = composed.Clause };
            diagnostics = composed.Diagnostics;
        }

        return new EditedProject(Replace(project, after), after, diagnostics);
    }

    /// <summary>Stores the clause the user wrote, normalised. Composes nothing.</summary>
    /// <remarks>
    /// Normalised at write time, as C.5.3 asks, so that the misalignment
    /// comparison and the recomposition comparison both see the canonical
    /// form. An edit that happens to equal the composed clause is, by
    /// construction, indistinguishable from "never edited" — and that is
    /// correct, since the next field change would then produce exactly what the
    /// user would have typed.
    /// </remarks>
    /// <exception cref="BlueprintRuleException"><c>BLUEPRINT_NOT_FOUND</c>.</exception>
    public static EditedProject EditSubjectClause(Project project, Guid blueprintId, string subjectClause)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(subjectClause);

        Blueprint before = Find(project, blueprintId);
        Blueprint after = before with { SubjectClause = ResolvedPrompt.Normalize(subjectClause) };

        return new EditedProject(Replace(project, after), after, []);
    }

    /// <summary>Whether the stored clause is what the composer would produce for these fields.</summary>
    /// <summary>
    /// Whether the blueprint's subject clause was edited by hand: it differs
    /// from what its fields compose today (DEC-067, deduced, never stored).
    /// </summary>
    /// <remarks>
    /// What the interface's lock shows (DEC-109): closed when the clause
    /// follows the fields, open when it no longer does.
    /// </remarks>
    public static bool IsSubjectClauseEdited(Blueprint blueprint, Universe universe, IPromptComposer composer)
    {
        ArgumentNullException.ThrowIfNull(blueprint);
        ArgumentNullException.ThrowIfNull(composer);

        return !IsUnedited(blueprint, universe, composer);
    }

    /// <summary>
    /// "Back to the automatic text" (DEC-109): the subject clause is composed
    /// again from the fields, whatever it said.
    /// </summary>
    /// <remarks>
    /// The counterpart of <see cref="EditSubjectClause"/>. After it, the
    /// clause follows the fields again — not because a flag says so, but
    /// because it equals their composition, which is what DEC-067 checks.
    /// </remarks>
    public static EditedProject ResetSubjectClause(Project project, Guid blueprintId, IPromptComposer composer)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(composer);

        Blueprint before = Find(project, blueprintId);
        ComposedSubject composed = composer.ComposeSubject(before, project.Universe);
        Blueprint after = before with { SubjectClause = composed.Clause };

        return new EditedProject(Replace(project, after), after, composed.Diagnostics);
    }

    private static bool IsUnedited(Blueprint blueprint, Universe universe, IPromptComposer composer)
    {
        string composed = composer.ComposeSubject(blueprint, universe).Clause;

        // Ordinal, on normalised strings - the misalignment rule, for the same
        // reason (C.5.4).
        return string.Equals(
            ResolvedPrompt.Normalize(blueprint.SubjectClause),
            ResolvedPrompt.Normalize(composed),
            StringComparison.Ordinal);
    }

    internal static Blueprint Find(Project project, Guid blueprintId)
    {
        return project.Blueprints.FirstOrDefault(blueprint => blueprint.Id == blueprintId)
            ?? throw new BlueprintRuleException(
                BlueprintRuleCode.BlueprintNotFound,
                $"The project has no blueprint with the identifier {blueprintId}.");
    }

    /// <summary>The project with one blueprint swapped for its new version, at the same position.</summary>
    internal static Project Replace(Project project, Blueprint replacement)
    {
        // Position preserved: the order of the blueprints is the order of the
        // pages (C.3.4), and editing a blueprint must not move its page.
        List<Blueprint> blueprints = [.. project.Blueprints];
        int index = blueprints.FindIndex(blueprint => blueprint.Id == replacement.Id);
        blueprints[index] = replacement;

        return project with { Blueprints = blueprints };
    }
}
