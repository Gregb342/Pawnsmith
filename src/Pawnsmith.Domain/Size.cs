namespace Pawnsmith.Domain;

/// <summary>
/// A creature size category. These are the tabletop rules' own category names,
/// not a translation of anything (DEC-031, DEC-037).
/// </summary>
/// <remarks>
/// A size carries no dimension of its own: it is a key into the calibration
/// file, which holds the millimetres. See <see cref="PawnDimensions"/>.
/// <para>
/// <c>Small</c> and <c>Medium</c> deliberately share a grid footprint — in the
/// rules, both occupy a 5-foot square. Only their height differs. This is not a
/// mistake to be tidied away.
/// </para>
/// </remarks>
public enum Size
{
    Small,
    Medium,
    Large,
    Huge,
    Gargantuan,
}
