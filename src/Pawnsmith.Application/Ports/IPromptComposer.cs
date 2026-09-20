using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Application.Ports;

/// <summary>
/// Produces the initial subject clause of a blueprint.
/// </summary>
/// <remarks>
/// <para>
/// <b>One method, and that is the point</b> (DEC-066). Chapter 7 of the bible
/// sketched a second one, <c>Assemble</c>, and it is gone: the assembly of the
/// three clauses is <see cref="ResolvedPrompt.From"/>, a pure domain function
/// written in T2. A port exists so that its implementation can be substituted,
/// and the only substitute on the horizon — the language-model composer of
/// EVO-001 — "rewrites the subject clause only; the style and framing clauses
/// stay out of its reach". The assembly will never have a second
/// implementation, so putting it behind an interface would promise a
/// substitution the design forbids.
/// </para>
/// <para>
/// The lock of DEC-028 comes out <b>stronger</b>: no method here receives or
/// returns a style or framing clause. The guarantee that the blueprint level
/// cannot reach the locked clauses is carried by an interface that does not
/// mention them at all.
/// </para>
/// <para>
/// The result is a record, not a string, because a value the catalogue does
/// not know produces a diagnostic <i>beside</i> the clause (§D.6.3). A
/// language-model composer would simply return an empty list.
/// </para>
/// </remarks>
public interface IPromptComposer
{
    /// <summary>The clause this blueprint's fields describe, in this universe.</summary>
    /// <param name="blueprint">The blueprint. Only its composition fields are read: race, class, optional parameters, details.</param>
    /// <param name="universe">The project's universe, which selects the template and catalogue.</param>
    ComposedSubject ComposeSubject(Blueprint blueprint, Universe universe);
}
