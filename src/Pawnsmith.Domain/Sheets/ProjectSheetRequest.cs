using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Domain.Sheets;

/// <summary>Why a blueprint contributed no cell to the sheet.</summary>
public enum SkipReason
{
    /// <summary>The blueprint has no elected candidate yet. The ordinary case (DEC-069).</summary>
    NoElectedCandidate,

    /// <summary>
    /// The elected candidate lacks a front or back cut-out. Unreachable through
    /// <c>CandidateElection</c>, which refuses it (DEC-071); reachable through a
    /// hand-edited file or an in-memory project.
    /// </summary>
    ElectedCandidateNotCutOut,

    /// <summary>
    /// <c>electedCandidateId</c> designates no candidate of the blueprint.
    /// Unreachable through the reader, which refuses it as intrinsic
    /// validation (C.3.4); reachable in memory.
    /// </summary>
    ElectedCandidateNotFound,
}

/// <summary>A blueprint the sheet left out, and why.</summary>
/// <param name="Blueprint">The blueprint, for the message.</param>
/// <param name="Reason">Why it contributed nothing.</param>
/// <param name="Message">What to tell the user, naming the blueprint.</param>
public sealed record SkippedBlueprint(Blueprint Blueprint, SkipReason Reason, string Message);

/// <summary>
/// What the sheet engine of T1 receives for a project, and which blueprints
/// it will not see.
/// </summary>
/// <param name="Request">The request, holding only the blueprints that have something to print.</param>
/// <param name="Skipped">The blueprints left out. Empty when every blueprint has an elected, cut-out candidate.</param>
public sealed record ProjectSheetRequest(SheetRequest Request, IReadOnlyList<SkippedBlueprint> Skipped);

/// <summary>
/// Turns a project into a sheet request: one item per blueprint that has an
/// elected candidate with both cut-outs, in blueprint order (§D.8.1).
/// </summary>
/// <remarks>
/// <para>
/// <b>A blueprint without an elected candidate is skipped and reported, never
/// blocking</b> (DEC-069). A partial sheet is an ordinary thing to want — six
/// goblins elected and printed while the ogre is still generating — so the
/// export is never refused for this. But a blueprint declared with a quantity
/// of six vanishing from the sheet with nothing to say so would be found out
/// with the paper in hand, so every skip is named.
/// </para>
/// <para>
/// <b>Never an empty page.</b> A size group with no elected blueprint produces
/// no item of that size, and <see cref="Pagination"/> only plans pages for the
/// sizes it is given. Nothing here has to know about pages for that to hold.
/// </para>
/// <para>
/// This is the one bridge between the model of T2 and the engine of T1, and
/// it is where DEC-069 lives. The engine itself is untouched: it still
/// receives a <see cref="SheetRequest"/> and knows nothing about elections.
/// </para>
/// </remarks>
public static class ProjectSheetRequestBuilder
{
    /// <summary>Builds the request for <paramref name="project"/> on <paramref name="paperFormat"/>.</summary>
    /// <param name="project">The project. Its blueprints are read in order.</param>
    /// <param name="paperFormat">The paper, already resolved by the caller from the project's format name.</param>
    public static ProjectSheetRequest From(Project project, PaperFormat paperFormat)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(paperFormat);

        List<SheetItem> items = [];
        List<SkippedBlueprint> skipped = [];

        foreach (Blueprint blueprint in project.Blueprints)
        {
            string name = ItemName(blueprint);

            if (blueprint.ElectedCandidateId is not Guid electedId)
            {
                skipped.Add(new SkippedBlueprint(
                    blueprint,
                    SkipReason.NoElectedCandidate,
                    $"The blueprint '{name}' has no elected candidate and was left off the sheet."));
                continue;
            }

            Candidate? elected = blueprint.Candidates.FirstOrDefault(candidate => candidate.Id == electedId);

            if (elected is null)
            {
                skipped.Add(new SkippedBlueprint(
                    blueprint,
                    SkipReason.ElectedCandidateNotFound,
                    $"The blueprint '{name}' elects the candidate {electedId}, which it does not have; it was left off the sheet."));
                continue;
            }

            if (elected.FrontImageFile is null || elected.BackImageFile is null)
            {
                skipped.Add(new SkippedBlueprint(
                    blueprint,
                    SkipReason.ElectedCandidateNotCutOut,
                    $"The elected candidate of the blueprint '{name}' has no cut-out yet; it was left off the sheet."));
                continue;
            }

            items.Add(new SheetItem(
                Name: name,
                Size: blueprint.Size,
                Quantity: blueprint.Quantity,
                FrontImageFile: elected.FrontImageFile,
                BackImageFile: elected.BackImageFile));
        }

        return new ProjectSheetRequest(
            new SheetRequest(project.Geometry, paperFormat, items),
            skipped);
    }

    /// <summary>
    /// What the engine calls this blueprint in its messages: race and class,
    /// which is how the user thinks of it.
    /// </summary>
    /// <remarks>
    /// Two blueprints can share a name, and that only blurs a message; the
    /// identifier is not put in because "goblin skirmisher
    /// (2d6b1f04-…)" is not something anyone reads on a command line.
    /// </remarks>
    private static string ItemName(Blueprint blueprint) =>
        $"{blueprint.Race.Trim()} {blueprint.CharacterClass.Trim()}";
}
