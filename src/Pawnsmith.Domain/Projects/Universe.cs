namespace Pawnsmith.Domain.Projects;

/// <summary>
/// The overall aesthetic register of a project, which decides which set of
/// prompt templates is used.
/// </summary>
/// <remarks>
/// One value in v1 (DEC-025). The field exists so that adding a universe later
/// is a new member and a new template file, not a change of shape — EVO-004.
/// <para>
/// A single-valued enumeration is not a lock: <c>universe</c> is modifiable
/// like every other project field, and changing it misaligns the existing
/// candidates rather than being forbidden (DEC-030, DEC-055).
/// </para>
/// </remarks>
public enum Universe
{
    Fantasy,
}
