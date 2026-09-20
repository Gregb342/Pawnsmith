using System.Globalization;

using Pawnsmith.Domain.Prompts;
using Pawnsmith.Domain.Tests.Fixtures;

namespace Pawnsmith.Domain.Tests.Prompts;

/// <summary>
/// Covers tests 1 to 14 of D.11: the composition rule of §D.6.2 and the
/// catalogue fallback of §D.6.3.
/// </summary>
public class SubjectClauseTests
{
    private static ComposedSubject Compose(
        string race = "goblin",
        string characterClass = "skirmisher",
        Dictionary<string, string>? parameters = null,
        string details = "",
        PromptTemplate? template = null,
        Catalog? catalog = null)
    {
        return SubjectClause.Compose(
            race,
            characterClass,
            parameters ?? [],
            details,
            template ?? PromptFixture.Template(optionalOrder: ["weapon", "armour"]),
            catalog ?? PromptFixture.Catalog());
    }

    // --- D.11 n° 1 : déterminisme ------------------------------------------

    [Fact]
    public void TwoCompositionsOfTheSameInputsGiveTheSameString()
    {
        Dictionary<string, string> parameters = PromptFixture.Parameters(("weapon", "axe"), ("armour", "mail"));

        string first = Compose(parameters: parameters, details: "one ear torn").Clause;
        string second = Compose(parameters: parameters, details: "one ear torn").Clause;

        second.ShouldBe(first);
    }

    // --- D.11 n° 2 : indépendance de la culture -----------------------------

    [Fact]
    public void TheCompositionIsTheSameUnderAnyCulture()
    {
        // Keys outside the template's order fall back to an ordinal sort. A
        // cultural sort would order them differently under some cultures, and
        // tr-TR is the one that bites: it has its own idea of where "i" goes.
        Dictionary<string, string> parameters = PromptFixture.Parameters(
            ("Island", "x"), ("island", "y"), ("izmir", "z"), ("weapon", "axe"));

        string invariant = ComposeUnder(CultureInfo.InvariantCulture, parameters);
        string french = ComposeUnder(new CultureInfo("fr-FR"), parameters);
        string turkish = ComposeUnder(new CultureInfo("tr-TR"), parameters);

        french.ShouldBe(invariant);
        turkish.ShouldBe(invariant);
    }

    private static string ComposeUnder(CultureInfo culture, Dictionary<string, string> parameters)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo previousUi = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            return Compose(parameters: parameters).Clause;
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    // --- D.11 n° 3 : substitution de la tête, champs Trim()és ---------------

    [Fact]
    public void RaceAndClassAreSubstitutedIntoTheHeadAndTrimmed()
    {
        string clause = Compose(race: "  orc ", characterClass: "\tbruiser\n").Clause;

        clause.ShouldBe("a orc bruiser");
    }

    // --- D.11 n° 4 : ordre déclaré par le template --------------------------

    [Fact]
    public void OptionalFragmentsFollowTheTemplateOrder()
    {
        // Alphabetically "armour" comes before "weapon". The template says
        // weapon first, and the template wins.
        string clause = Compose(parameters: PromptFixture.Parameters(("armour", "mail"), ("weapon", "axe"))).Clause;

        clause.ShouldBe("a goblin skirmisher, wielding a large battle axe, wearing a mail hauberk");
    }

    // --- D.11 n° 5 : clés hors template, après, en ordinal ------------------

    [Fact]
    public void KeysTheTemplateDoesNotListComeAfterInOrdinalOrder()
    {
        Dictionary<string, string> parameters = PromptFixture.Parameters(
            ("mount", "wolf"), ("armour", "leather"), ("Banner", "red"), ("weapon", "spear"));

        string clause = Compose(parameters: parameters).Clause;

        // Ordinal puts uppercase "Banner" before lowercase "mount".
        clause.ShouldBe("a goblin skirmisher, wielding a short spear, wearing leather scraps, red, wolf");
    }

    // --- D.11 n° 6 : la seule tête ------------------------------------------

    [Fact]
    public void ABlueprintWithNothingOptionalComposesTheHeadAlone()
    {
        Compose().Clause.ShouldBe("a goblin skirmisher");
    }

    // --- D.11 n° 7 : les détails en dernier --------------------------------

    [Fact]
    public void DetailsComeLast()
    {
        string clause = Compose(
            parameters: PromptFixture.Parameters(("weapon", "axe")),
            details: "one ear torn, a bone fetish tied to the belt").Clause;

        clause.ShouldBe("a goblin skirmisher, wielding a large battle axe, one ear torn, a bone fetish tied to the belt");
    }

    // --- D.11 n° 8 : fragments vides omis ----------------------------------

    [Fact]
    public void EmptyFragmentsAreOmittedAndTheClauseNeverEndsWithASeparator()
    {
        string clause = Compose(
            parameters: PromptFixture.Parameters(("weapon", "   "), ("armour", "mail")),
            details: "  ").Clause;

        clause.ShouldBe("a goblin skirmisher, wearing a mail hauberk");
        clause.ShouldNotEndWith(",");
    }

    [Fact]
    public void AnEmptyValueIsNotConstrainedAndRaisesNoDiagnostic()
    {
        ComposedSubject composed = Compose(parameters: PromptFixture.Parameters(("weapon", "")));

        composed.Clause.ShouldBe("a goblin skirmisher");
        composed.Diagnostics.ShouldBeEmpty();
    }

    // --- D.11 n° 9 : normalisation ------------------------------------------

    [Fact]
    public void TheClauseIsNormalisedLikeEveryOtherClause()
    {
        string clause = Compose(details: "one ear torn\r\nbone fetish  ").Clause;

        clause.ShouldBe("a goblin skirmisher, one ear torn\nbone fetish");
        clause.ShouldNotContain("\r");
        ResolvedPrompt.Normalize(clause).ShouldBe(clause);
    }

    // --- D.11 n° 10 : le fragment, pas la valeur ----------------------------

    [Fact]
    public void AKnownValueProducesItsFragmentNotItsName()
    {
        ComposedSubject composed = Compose(parameters: PromptFixture.Parameters(("weapon", "axe")));

        composed.Clause.ShouldBe("a goblin skirmisher, wielding a large battle axe");
        composed.Clause.ShouldNotContain(", axe");
        composed.Diagnostics.ShouldBeEmpty();
    }

    // --- D.11 n° 11 : valeur inconnue, repli et diagnostic ------------------

    [Fact]
    public void AnUnknownValueUsesTheFallbackAndIsDiagnosed()
    {
        ComposedSubject composed = Compose(
            parameters: PromptFixture.Parameters(("weapon", "halberd")),
            template: PromptFixture.Template(fallback: "carrying a {value}", optionalOrder: ["weapon"]));

        composed.Clause.ShouldBe("a goblin skirmisher, carrying a halberd");

        CompositionDiagnostic diagnostic = composed.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Key.ShouldBe("weapon");
        diagnostic.Value.ShouldBe("halberd");
        diagnostic.Message.ShouldContain("'halberd'");
    }

    // --- D.11 n° 12 : clé inconnue, même repli, même diagnostic -------------

    [Fact]
    public void AnUnknownKeyUsesTheFallbackAndIsDiagnosed()
    {
        ComposedSubject composed = Compose(parameters: PromptFixture.Parameters(("mount", "wolf")));

        composed.Clause.ShouldBe("a goblin skirmisher, wolf");

        CompositionDiagnostic diagnostic = composed.Diagnostics.ShouldHaveSingleItem();
        diagnostic.Key.ShouldBe("mount");
        diagnostic.Message.ShouldContain("does not know the key 'mount'");
    }

    [Fact]
    public void DiagnosticsComeInClauseOrder()
    {
        Dictionary<string, string> parameters = PromptFixture.Parameters(
            ("mount", "wolf"), ("weapon", "halberd"), ("armour", "mail"));

        ComposedSubject composed = Compose(parameters: parameters);

        composed.Diagnostics.Select(diagnostic => diagnostic.Key).ShouldBe(["weapon", "mount"]);
    }

    // --- D.11 n° 13 : jamais d'exception (DEC-056) --------------------------

    [Fact]
    public void AnUnknownValueNeverThrows()
    {
        Dictionary<string, string> parameters = PromptFixture.Parameters(
            ("weapon", "halberd"), ("mount", "wolf"), ("colour", "red"));

        Should.NotThrow(() => Compose(parameters: parameters));
    }

    // --- D.11 n° 14 : catalogue vide ---------------------------------------

    [Fact]
    public void AnEmptyCatalogueStillComposesHeadFallbacksAndDetails()
    {
        ComposedSubject composed = Compose(
            parameters: PromptFixture.Parameters(("weapon", "axe")),
            details: "one ear torn",
            catalog: Catalog.Empty(Domain.Projects.Universe.Fantasy));

        composed.Clause.ShouldBe("a goblin skirmisher, axe, one ear torn");
        composed.Diagnostics.ShouldHaveSingleItem();
    }

    // --- Ce que le composeur ne voit pas -----------------------------------

    [Fact]
    public void TheBlueprintOverloadUsesExactlyTheFourCompositionFields()
    {
        // Size and quantity are not inputs: a Large pawn is large because its
        // cell is, not because the prompt said so (§D.5.2).
        Domain.Projects.Blueprint blueprint = ProjectFixture.Blueprint() with
        {
            OptionalParameters = PromptFixture.Parameters(("weapon", "spear")),
            Details = "one ear torn",
        };

        ComposedSubject composed = SubjectClause.Compose(
            blueprint,
            PromptFixture.Template(optionalOrder: ["weapon"]),
            PromptFixture.Catalog());

        composed.Clause.ShouldBe("a goblin skirmisher, wielding a short spear, one ear torn");
        composed.Clause.ShouldNotContain("Medium");
    }
}
