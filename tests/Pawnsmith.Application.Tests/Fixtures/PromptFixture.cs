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
    /// <summary>The same word for every interface culture: the tests are not about translation.</summary>
    public static Dictionary<string, string> Labels(string word) =>
        Pawnsmith.Domain.Primitives.InterfaceCulture.All.ToDictionary(culture => culture, _ => word, StringComparer.Ordinal);

    public static CatalogEntry Entry(string value, string fragment) => new(value, fragment, Labels(value));

    public static CatalogParameter Param(string key, params CatalogEntry[] entries) => new(key, Labels(key), entries);

    public static PromptTemplate Template(string head = "a {race} {characterClass}")
    {
        return PromptTemplate.Create(Universe.Fantasy, head, ["weapon", "armour"], "{value}");
    }

    public static Catalog Catalog()
    {
        return Domain.Prompts.Catalog.Create(Universe.Fantasy,
        [
            Param("weapon",
                Entry("axe", "wielding a large battle axe"),
                Entry("spear", "wielding a short spear")),
            Param("armour",
                Entry("leather", "wearing leather scraps")),
        ]);
    }

    public static TemplatePromptComposer Composer(PromptTemplate? template = null) =>
        new(template ?? Template(), Catalog());
}
