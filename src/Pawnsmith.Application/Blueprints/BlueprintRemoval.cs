using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Blueprints;

/// <summary>
/// A project after a blueprint was removed, and the files that blueprint was
/// the last to reference.
/// </summary>
/// <param name="Project">The project without the blueprint. Not yet saved.</param>
/// <param name="Removed">The blueprint that left, for the caller's message.</param>
/// <param name="ReferencedFiles">
/// The image paths its candidates referenced, relative to the project folder,
/// each once. <b>Not yet deleted</b>: the disk is touched after the model, by
/// whoever saved the project.
/// </param>
public sealed record RemovedBlueprint(
    Project Project,
    Blueprint Removed,
    IReadOnlyList<string> ReferencedFiles);

/// <summary>
/// Removes a blueprint, its candidates with it, and says which files went
/// with them (§D.8.2).
/// </summary>
/// <remarks>
/// <para>
/// <b>The files go too</b> (DEC-070). A file nothing references is not a
/// backup: it is invisible from the application, reachable only through a file
/// manager, and it weighs on every <c>Backup</c> archive forever. What makes
/// the decision bearable is that a project is an ordinary folder (DEC-011) and
/// the <c>Backup</c> profile exists precisely for this — the net is there, it is
/// explicit, and it does not depend on orphans.
/// </para>
/// <para>
/// <b>No refusal, no un-election step first.</b> An elected candidate does not
/// protect its blueprint: the extra step would explain nothing, and a user set
/// on deleting would go through it mechanically.
/// </para>
/// <para>
/// <b>Model first, disk second, and only the listed files.</b> This function
/// is pure and returns the paths; the caller saves the project and then
/// deletes exactly these. It never sweeps <c>images/</c> for whatever is no
/// longer referenced — such a sweep would also take the files of a project
/// half-written by another process, and turn a local operation into a global
/// one. The order matters for failure too: a save that fails leaves the files
/// in place and the project intact; a deletion that fails leaves orphans,
/// which are harmless.
/// </para>
/// </remarks>
public static class BlueprintRemoval
{
    /// <summary>Removes the blueprint from the project and lists what it referenced.</summary>
    /// <exception cref="BlueprintRuleException"><c>BLUEPRINT_NOT_FOUND</c>.</exception>
    public static RemovedBlueprint Remove(Project project, Guid blueprintId)
    {
        ArgumentNullException.ThrowIfNull(project);

        Blueprint removed = BlueprintEditor.Find(project, blueprintId);

        // Distinct, because a hand-edited project could point two candidates at
        // one file, and deleting it twice would fail the second time for no
        // reason worth reporting.
        var files = removed.Candidates
            .SelectMany(candidate => new[]
            {
                candidate.PairedImageFile,
                candidate.FrontImageFile,
                candidate.BackImageFile,
            })
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();

        // Position of the others preserved: removing one page must not
        // reorder the rest.
        var remaining = project.Blueprints
            .Where(blueprint => blueprint.Id != blueprintId)
            .ToList();

        return new RemovedBlueprint(project with { Blueprints = remaining }, removed, files);
    }
}
