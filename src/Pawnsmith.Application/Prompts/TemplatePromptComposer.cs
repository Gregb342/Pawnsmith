using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Application.Prompts;

/// <summary>
/// The composer of v1: a template and a catalogue, nothing else (DEC-009).
/// </summary>
/// <remarks>
/// <para>
/// Deliberately thin. The rule is <see cref="SubjectClause.Compose"/>, in the
/// domain; this class holds the two data files for one universe and hands them
/// over. It is the implementation the port exists to let EVO-001 replace, and
/// it stays small so that replacing it replaces nothing else.
/// </para>
/// <para>
/// One universe per instance, not a dictionary of them. There is one universe
/// in v1 (DEC-025), and a lookup table keyed by universe would be a structure
/// introduced for a case that does not exist yet. When a second universe
/// arrives, whoever wires the composer decides whether to hold two instances
/// or to grow this one — with the case in front of them.
/// </para>
/// <para>
/// <b>The catalogue is read at each composition</b>, through a function, since
/// T6's front: personal entries change it while the application runs
/// (DEC-107), and a composer holding the catalogue of its start-up would
/// compose without them. The constructor taking a <see cref="Catalog"/> keeps
/// the fixed case — tests, the CLI — as simple as it was.
/// </para>
/// </remarks>
public sealed class TemplatePromptComposer : IPromptComposer
{
    private readonly PromptTemplate template;
    private readonly Func<Catalog> catalog;

    /// <exception cref="ArgumentException">The template and the catalogue are not for the same universe.</exception>
    public TemplatePromptComposer(PromptTemplate template, Catalog catalog)
        : this(template, Fixed(catalog))
    {
    }

    /// <param name="template">The universe's sentence structure.</param>
    /// <param name="catalog">Gives the catalogue as it is at the moment of each composition.</param>
    /// <exception cref="ArgumentException">The template and the catalogue are not for the same universe.</exception>
    public TemplatePromptComposer(PromptTemplate template, Func<Catalog> catalog)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(catalog);

        Catalog first = catalog();

        if (template.Universe != first.Universe)
        {
            throw new ArgumentException(
                $"The template is for the universe {template.Universe} and the catalogue for {first.Universe}; " +
                "a composer needs both for the same universe.",
                nameof(catalog));
        }

        this.template = template;
        this.catalog = catalog;
    }

    /// <summary>The universe this composer is wired for.</summary>
    public Universe Universe => template.Universe;

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// The project's universe is not the one this composer holds files for. It
    /// cannot happen with one universe; the check is what makes that statement
    /// true rather than assumed.
    /// </exception>
    public ComposedSubject ComposeSubject(Blueprint blueprint, Universe universe)
    {
        ArgumentNullException.ThrowIfNull(blueprint);

        if (universe != template.Universe)
        {
            throw new InvalidOperationException(
                $"This composer holds the template and catalogue of the universe {template.Universe}, " +
                $"and was asked to compose for {universe}.");
        }

        return SubjectClause.Compose(blueprint, template, catalog());
    }

    private static Func<Catalog> Fixed(Catalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        return () => catalog;
    }
}
