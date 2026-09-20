using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Domain.Tests.Prompts;

/// <summary>
/// The internal consistency of a catalogue: the domain half of tests 15, 16
/// and 21 of D.11. The file reader's half — mapping these refusals to
/// <c>CATALOG_INVALID</c> — is tested with the reader.
/// </summary>
public class CatalogTests
{
    private static CatalogParameter Weapon(params CatalogEntry[] entries) => new("weapon", entries);

    [Fact]
    public void AWellFormedCatalogueAnswersForTheValuesItKnows()
    {
        var catalog = Catalog.Create(Universe.Fantasy,
        [
            Weapon(new CatalogEntry("axe", "wielding a large battle axe"), new CatalogEntry("spear", "wielding a short spear")),
            new CatalogParameter("armour", [new("leather", "wearing leather scraps")]),
        ]);

        catalog.TryGetFragment("weapon", "spear", out string fragment).ShouldBeTrue();
        fragment.ShouldBe("wielding a short spear");
        catalog.KnowsKey("armour").ShouldBeTrue();
    }

    [Fact]
    public void AnUnknownKeyOrValueIsSimplyNotFound()
    {
        var catalog = Catalog.Create(Universe.Fantasy, [Weapon(new CatalogEntry("axe", "wielding an axe"))]);

        catalog.TryGetFragment("weapon", "halberd", out _).ShouldBeFalse();
        catalog.TryGetFragment("mount", "horse", out _).ShouldBeFalse();
        catalog.KnowsKey("mount").ShouldBeFalse();
    }

    [Fact]
    public void KeysAndValuesAreMatchedOrdinallyAndCaseSensitively()
    {
        // The project's rule for free strings, everywhere (C.3.3, C.5.4). A
        // lenient match here would make the composed clause obey a rule the
        // misalignment check does not share.
        var catalog = Catalog.Create(Universe.Fantasy, [Weapon(new CatalogEntry("axe", "wielding an axe"))]);

        catalog.TryGetFragment("Weapon", "axe", out _).ShouldBeFalse();
        catalog.TryGetFragment("weapon", "Axe", out _).ShouldBeFalse();
    }

    // --- D.11 n° 15 : clé dupliquée ----------------------------------------

    [Fact]
    public void ARepeatedKeyIsRefusedAndNamed()
    {
        CatalogException error = Should.Throw<CatalogException>(() =>
            Catalog.Create(Universe.Fantasy,
            [
                Weapon(new CatalogEntry("axe", "wielding an axe")),
                Weapon(new CatalogEntry("spear", "wielding a spear")),
            ]));

        error.Message.ShouldContain("'weapon'");
    }

    // --- D.11 n° 16 : valeur dupliquée pour une même clé --------------------

    [Fact]
    public void ARepeatedValueUnderOneKeyIsRefusedAndNamed()
    {
        CatalogException error = Should.Throw<CatalogException>(() =>
            Catalog.Create(Universe.Fantasy,
            [
                Weapon(new CatalogEntry("axe", "wielding an axe"), new CatalogEntry("axe", "wielding another axe")),
            ]));

        error.Message.ShouldContain("'weapon'");
        error.Message.ShouldContain("'axe'");
    }

    [Fact]
    public void TheSameValueUnderTwoDifferentKeysIsFine()
    {
        // "leather" can be an armour and a clothing. Uniqueness is per key.
        var catalog = Catalog.Create(Universe.Fantasy,
        [
            new CatalogParameter("armour", [new("leather", "wearing leather armour")]),
            new CatalogParameter("clothing", [new("leather", "dressed in leather")]),
        ]);

        catalog.TryGetFragment("armour", "leather", out string armour).ShouldBeTrue();
        catalog.TryGetFragment("clothing", "leather", out string clothing).ShouldBeTrue();
        armour.ShouldNotBe(clothing);
    }

    // --- D.11 n° 21 : fragment vide -----------------------------------------

    [Fact]
    public void AnEmptyFragmentIsRefusedAndTheEntryIsNamed()
    {
        CatalogException error = Should.Throw<CatalogException>(() =>
            Catalog.Create(Universe.Fantasy, [Weapon(new CatalogEntry("axe", "   "))]));

        error.Message.ShouldContain("weapon: axe");
    }

    [Fact]
    public void AnEmptyKeyOrValueIsRefused()
    {
        Should.Throw<CatalogException>(() =>
            Catalog.Create(Universe.Fantasy, [new CatalogParameter(" ", [])]));

        Should.Throw<CatalogException>(() =>
            Catalog.Create(Universe.Fantasy, [Weapon(new CatalogEntry("", "wielding something"))]));
    }

    [Fact]
    public void AnEmptyCatalogueIsLegitimate()
    {
        var catalog = Catalog.Empty(Universe.Fantasy);

        catalog.Parameters.ShouldBeEmpty();
        catalog.TryGetFragment("weapon", "axe", out _).ShouldBeFalse();
    }

    [Fact]
    public void ParameterOrderIsPreservedNeverSorted()
    {
        // D.11 n° 22, domain half: the composition rule relies on this order.
        var catalog = Catalog.Create(Universe.Fantasy,
        [
            new CatalogParameter("weapon", []),
            new CatalogParameter("armour", []),
        ]);

        catalog.Parameters.Select(parameter => parameter.Key).ShouldBe(["weapon", "armour"]);
    }
}
