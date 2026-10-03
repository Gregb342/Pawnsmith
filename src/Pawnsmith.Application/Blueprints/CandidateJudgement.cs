using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Blueprints;

/// <summary>
/// Records the user's judgement on a candidate: draft, valid, rejected
/// (§G.6.1).
/// </summary>
/// <remarks>
/// <para>
/// The journey of §D.3 has the user <b>rule on each candidate</b>, and nothing
/// let them: T3 wrote the election, not the judgement. This is the one use case
/// the API needed and could not find.
/// </para>
/// <para>
/// <b>The status moves, and nothing else does.</b> Not the election — the two
/// are independent axes (DEC-068), so an elected candidate may be rejected and
/// stays elected until the user elects another. Not the files — a status
/// requires none (DEC-071), since the user judges the paired image long before
/// any cut-out exists.
/// </para>
/// </remarks>
public static class CandidateJudgement
{
    /// <summary>Sets the status of one candidate of one blueprint.</summary>
    /// <exception cref="BlueprintRuleException"><c>BLUEPRINT_NOT_FOUND</c> or <c>CANDIDATE_NOT_FOUND</c>.</exception>
    public static EditedProject SetStatus(Project project, Guid blueprintId, Guid candidateId, CandidateStatus status)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "Not a status.");
        }

        Blueprint blueprint = BlueprintEditor.Find(project, blueprintId);

        int index = blueprint.Candidates.ToList().FindIndex(candidate => candidate.Id == candidateId);

        if (index < 0)
        {
            throw new BlueprintRuleException(
                BlueprintRuleCode.CandidateNotFound,
                $"The blueprint {blueprintId} has no candidate with the identifier {candidateId}.");
        }

        // Position preserved: the order of the candidates is the order of the
        // gallery, and judging one must not move it.
        List<Candidate> candidates = [.. blueprint.Candidates];
        candidates[index] = candidates[index] with { Status = status };

        Blueprint judged = blueprint with { Candidates = candidates };

        return new EditedProject(BlueprintEditor.Replace(project, judged), judged, []);
    }
}
