using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Blueprints;

/// <summary>
/// Elects a candidate: designates what the sheet will consume for a blueprint
/// (§D.8.3, §D.8.4).
/// </summary>
/// <remarks>
/// <para>
/// <b>Election and status are two axes</b> (DEC-068). Electing a candidate
/// moves <see cref="Blueprint.ElectedCandidateId"/> and nothing else: the
/// previous elected candidate keeps its status, because the user judged it
/// good and preferring another one does not make it bad. Merging the two axes
/// is the trap section 3.1 of the bible already names for misalignment.
/// </para>
/// <para>
/// <b>Election requires both cut-outs; status requires nothing</b> (DEC-071).
/// The status is the user's judgement, and they judge on the paired image long
/// before a cut-out exists. Election is not a judgement — it is the designation
/// of what the sheet consumes, and the sheet consumes two transparent PNGs. The
/// constraint is placed here, once, where it serves, rather than surfacing at
/// layout time as a file that cannot be found.
/// </para>
/// </remarks>
public static class CandidateElection
{
    /// <summary>Makes <paramref name="candidateId"/> the elected candidate of its blueprint.</summary>
    /// <exception cref="BlueprintRuleException">
    /// <c>BLUEPRINT_NOT_FOUND</c>, <c>CANDIDATE_NOT_FOUND</c>, or
    /// <c>CANDIDATE_NOT_CUT_OUT</c> when the candidate lacks a front or back file.
    /// </exception>
    public static EditedProject Elect(Project project, Guid blueprintId, Guid candidateId)
    {
        ArgumentNullException.ThrowIfNull(project);

        Blueprint blueprint = BlueprintEditor.Find(project, blueprintId);

        Candidate candidate = blueprint.Candidates.FirstOrDefault(each => each.Id == candidateId)
            ?? throw new BlueprintRuleException(
                BlueprintRuleCode.CandidateNotFound,
                $"The blueprint {blueprintId} has no candidate with the identifier {candidateId}.");

        if (candidate.FrontImageFile is null || candidate.BackImageFile is null)
        {
            throw new BlueprintRuleException(
                BlueprintRuleCode.CandidateNotCutOut,
                $"The candidate {candidateId} cannot be elected: it has no " +
                (candidate.FrontImageFile is null && candidate.BackImageFile is null
                    ? "front or back cut-out"
                    : candidate.FrontImageFile is null ? "front cut-out" : "back cut-out") +
                " yet. A sheet consumes both.");
        }

        // Only the election moves. The candidates list is passed through
        // untouched, statuses included (DEC-068).
        Blueprint elected = blueprint with { ElectedCandidateId = candidateId };

        return new EditedProject(BlueprintEditor.Replace(project, elected), elected, []);
    }

    /// <summary>Leaves the blueprint with no elected candidate.</summary>
    /// <remarks>
    /// The symmetric operation, so that a caller never has to reach for
    /// <c>with</c> on a blueprint to undo an election. Statuses are untouched
    /// here too.
    /// </remarks>
    /// <exception cref="BlueprintRuleException"><c>BLUEPRINT_NOT_FOUND</c>.</exception>
    public static EditedProject Unelect(Project project, Guid blueprintId)
    {
        ArgumentNullException.ThrowIfNull(project);

        Blueprint blueprint = BlueprintEditor.Find(project, blueprintId);
        Blueprint cleared = blueprint with { ElectedCandidateId = null };

        return new EditedProject(BlueprintEditor.Replace(project, cleared), cleared, []);
    }
}
