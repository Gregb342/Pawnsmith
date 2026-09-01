namespace Pawnsmith.Domain;

/// <summary>
/// How a pawn is physically built. Locked at project level (DEC-001), so a
/// single value applies to a whole sheet.
/// </summary>
/// <remarks>
/// The only difference between the two is what sits below the feet line and
/// how much the unfolded height grows as a result. Everything else is common.
/// </remarks>
public enum Geometry
{
    /// <summary>
    /// Flaps below each feet line fold outwards to form a base. No socket needed.
    /// </summary>
    FoldedTent,

    /// <summary>
    /// A rectangular tab below each feet line slides into a commercial base.
    /// </summary>
    TabAndSocket,
}
