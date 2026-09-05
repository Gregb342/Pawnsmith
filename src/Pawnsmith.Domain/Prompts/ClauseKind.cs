using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Domain.Prompts;

/// <summary>
/// Which of the three segments of a prompt is being talked about.
/// </summary>
/// <remarks>
/// The three have different scopes, and that is the whole point of naming them
/// separately: the framing clause belongs to the application, the subject
/// clause to the blueprint, the style clause to the project (section 4.1 of the
/// bible).
/// <para>
/// This enumeration exists so that misalignment can say <i>which</i> clause
/// moved rather than "something changed" (DEC-049).
/// </para>
/// </remarks>
public enum ClauseKind
{
    /// <summary>
    /// Constant of the application, never exposed in the interface (DEC-029).
    /// It lives in the ComfyUI workflow template, which T4 reads; T2 receives
    /// it as a plain function argument and knows nothing about where it slept.
    /// </summary>
    Framing,

    /// <summary>
    /// The blueprint's own clause. The only segment the user may edit (DEC-028).
    /// </summary>
    Subject,

    /// <summary>
    /// The project's clause, applied identically to every blueprint.
    /// </summary>
    Style,
}
