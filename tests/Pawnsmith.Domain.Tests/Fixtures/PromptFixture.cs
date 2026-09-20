using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Domain.Tests.Fixtures;

/// <summary>
/// A small template and catalogue for the composition tests.
/// </summary>
/// <remarks>
/// Fragments are short on purpose — the tests are about ordering and
/// fallback rules, and a realistic fragment would only make an assertion
/// harder to read. The shipped files carry the real ones.
/// </remarks>
internal static class PromptFixture
{
    public static PromptTemplate Template(
        string head = "a {race} {characterClass}",
        string fallback = "{value}",
        params string[] optionalOrder)
    {
        return PromptTemplate.Create(Universe.Fantasy, head, optionalOrder, fallback);
    }

    /// <summary>Weapon and armour, two values each. Nothing else.</summary>
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
                new CatalogEntry("mail", "wearing a mail hauberk"),
            ]),
        ]);
    }

    public static Dictionary<string, string> Parameters(params (string Key, string Value)[] pairs)
    {
        Dictionary<string, string> parameters = new(StringComparer.Ordinal);

        foreach ((string key, string value) in pairs)
        {
            parameters[key] = value;
        }

        return parameters;
    }
}
