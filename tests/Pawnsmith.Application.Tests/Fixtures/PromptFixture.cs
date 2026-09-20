using Pawnsmith.Application.Prompts;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Application.Tests.Fixtures;

/// <summary>
/// A template and a catalogue whose composition of the fixture blueprint is
/// exactly <see cref="ProjectFixture.Subject"/>.
/// </summary>
/// <remarks>
/// That equality is what the recomposition tests rely on: a blueprint whose
/// stored clause is the composed one counts as "never edited" (DEC-067).
/// </remarks>
internal static class PromptFixture
{
    public static PromptTemplate Template(string head = "a {race} {characterClass}")
    {
        return PromptTemplate.Create(Universe.Fantasy, head, ["weapon", "armour"], "{value}");
    }

    public static Catalog Catalog()
    {
        return Domain.Prompts.Catalog.Create(Universe.Fantasy,
        [
            new CatalogParameter("weapon",
            [
                new CatalogEntry("axe", "wielding a large battle axe"),
                new CatalogEntry("spear", "wielding a short spear"),
            ]),
            new CatalogParameter("armour",
            [
                new CatalogEntry("leather", "wearing leather scraps"),
            ]),
        ]);
    }

    public static TemplatePromptComposer Composer(PromptTemplate? template = null) =>
        new(template ?? Template(), Catalog());
}
