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
    /// <summary>The same word for every interface culture: the tests are not about translation.</summary>
    public static Dictionary<string, string> Labels(string word) =>
        Pawnsmith.Domain.Primitives.InterfaceCulture.All.ToDictionary(culture => culture, _ => word, StringComparer.Ordinal);

    public static CatalogEntry Entry(string value, string fragment) => new(value, fragment, Labels(value));

    public static CatalogParameter Param(string key, params CatalogEntry[] entries) => new(key, Labels(key), entries);

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
            Param("weapon",
                Entry("axe", "wielding a large battle axe"),
                Entry("spear", "wielding a short spear")),
            Param("armour",
                Entry("leather", "wearing leather scraps"),
                Entry("mail", "wearing a mail hauberk")),
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
