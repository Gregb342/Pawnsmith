using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

using Pawnsmith.Domain.Tests.Fixtures;

namespace Pawnsmith.Domain.Tests.Prompts;

/// <summary>
/// The internal consistency of a catalogue: the domain half of tests 15, 16
/// and 21 of D.11. The file reader's half — mapping these refusals to
/// <c>CATALOG_INVALID</c> — is tested with the reader.
/// </summary>
public class CatalogTests
{
    private static CatalogParameter Weapon(params CatalogEntry[] entries) => PromptFixture.Param("weapon", entries);

    [Fact]
    public void AWellFormedCatalogueAnswersForTheValuesItKnows()
    {
        var catalog = Catalog.Create(Universe.Fantasy,
        [
            Weapon(PromptFixture.Entry("axe", "wielding a large battle axe"), PromptFixture.Entry("spear", "wielding a short spear")),
            PromptFixture.Param("armour", PromptFixture.Entry("leather", "wearing leather scraps")),
        ]);

        catalog.TryGetFragment("weapon", "spear", out string fragment).ShouldBeTrue();
        fragment.ShouldBe("wielding a short spear");
        catalog.KnowsKey("armour").ShouldBeTrue();
    }

    [Fact]
    public void AnUnknownKeyOrValueIsSimplyNotFound()
    {
        var catalog = Catalog.Create(Universe.Fantasy, [Weapon(PromptFixture.Entry("axe", "wielding an axe"))]);

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
        var catalog = Catalog.Create(Universe.Fantasy, [Weapon(PromptFixture.Entry("axe", "wielding an axe"))]);

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
                Weapon(PromptFixture.Entry("axe", "wielding an axe")),
                Weapon(PromptFixture.Entry("spear", "wielding a spear")),
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
                Weapon(PromptFixture.Entry("axe", "wielding an axe"), PromptFixture.Entry("axe", "wielding another axe")),
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
            PromptFixture.Param("armour", PromptFixture.Entry("leather", "wearing leather armour")),
            PromptFixture.Param("clothing", PromptFixture.Entry("leather", "dressed in leather")),
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
            Catalog.Create(Universe.Fantasy, [Weapon(PromptFixture.Entry("axe", "   "))]));

        error.Message.ShouldContain("weapon: axe");
    }

    [Fact]
    public void AnEmptyKeyOrValueIsRefused()
    {
        Should.Throw<CatalogException>(() =>
            Catalog.Create(Universe.Fantasy, [PromptFixture.Param(" ")]));

        Should.Throw<CatalogException>(() =>
            Catalog.Create(Universe.Fantasy, [Weapon(PromptFixture.Entry("", "wielding something"))]));
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
            PromptFixture.Param("weapon"),
            PromptFixture.Param("armour"),
        ]);

        catalog.Parameters.Select(parameter => parameter.Key).ShouldBe(["weapon", "armour"]);
    }

    // --- I.12 n° 1 : un libellé par culture d'interface (DEC-106) ------------

    [Fact]
    public void AnEntryWithoutAFrenchLabelIsRefusedAndNamed()
    {
        var englishOnly = new Dictionary<string, string>(StringComparer.Ordinal) { ["en"] = "axe" };

        CatalogException error = Should.Throw<CatalogException>(() =>
            Catalog.Create(Universe.Fantasy, [Weapon(new CatalogEntry("axe", "wielding an axe", englishOnly))]));

        error.Message.ShouldContain("weapon: axe");
        error.Message.ShouldContain("'fr'");
    }

    [Fact]
    public void AParameterWithABlankLabelIsRefused()
    {
        var blank = new Dictionary<string, string>(StringComparer.Ordinal) { ["en"] = "Weapon", ["fr"] = "  " };

        CatalogException error = Should.Throw<CatalogException>(() =>
            Catalog.Create(Universe.Fantasy, [new CatalogParameter("weapon", blank, [])]));

        error.Message.ShouldContain("'weapon'");
    }

    [Fact]
    public void AnEntryIsShippedUnlessSaidOtherwise()
    {
        PromptFixture.Entry("axe", "wielding an axe").Origin.ShouldBe(CatalogEntryOrigin.Shipped);
    }
}
