namespace Pawnsmith.Domain;

/// <summary>
/// Where a candidate stands in the user's arbitration.
/// </summary>
/// <remarks>
/// <b>Misalignment is not one of these values, and must never become one.</b>
/// The two are independent axes: a <see cref="Valid"/> candidate can become
/// misaligned without ceasing to be validated. Merging them would make it
/// impossible to tell "rejected by the user" from "produced under a style that
/// is no longer the project's" (DEC-030).
/// </remarks>
public enum CandidateStatus
{
    /// <summary>Produced, not yet arbitrated.</summary>
    Draft,

    /// <summary>Kept by the user. Eligible for election.</summary>
    Valid,

    /// <summary>Turned down by the user. Dropped from a <c>Share</c> archive (DEC-050).</summary>
    Rejected,
}
