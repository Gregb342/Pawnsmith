using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Domain.Tests.Prompts;

/// <summary>
/// The closed list of tokens: the domain half of tests 18 and 19 of D.11.
/// </summary>
public class PromptTemplateTests
{
    private static PromptTemplate Create(
        string head = "a {race} {characterClass}",
        string fallback = "{value}",
        params string[] order) =>
        PromptTemplate.Create(Universe.Fantasy, head, order, fallback);

    [Fact]
    public void AWellFormedTemplateIsAccepted()
    {
        PromptTemplate template = Create(order: ["weapon", "armour"]);

        template.SubjectHead.ShouldBe("a {race} {characterClass}");
        template.OptionalOrder.ShouldBe(["weapon", "armour"]);
        template.UnknownValueFragment.ShouldBe("{value}");
    }

    // --- D.11 n° 18 : jeton inconnu, nommé ----------------------------------

    [Fact]
    public void AnUnknownTokenInTheHeadIsRefusedAndNamed()
    {
        PromptTemplateException error = Should.Throw<PromptTemplateException>(() =>
            Create(head: "a {race} {characterClass} of {taille} size"));

        error.Fault.ShouldBe(PromptTemplateFault.UnknownToken);
        error.Message.ShouldContain("{taille}");
    }

    [Fact]
    public void AnyTokenOtherThanValueInTheFallbackIsRefused()
    {
        PromptTemplateException error = Should.Throw<PromptTemplateException>(() =>
            Create(fallback: "{race} with {value}"));

        error.Fault.ShouldBe(PromptTemplateFault.UnknownToken);
        error.Message.ShouldContain("{race}");
    }

    // --- D.11 n° 19 : les deux jetons de tête sont obligatoires -------------

    [Fact]
    public void AHeadWithoutRaceIsRefused()
    {
        PromptTemplateException error = Should.Throw<PromptTemplateException>(() =>
            Create(head: "a {characterClass}"));

        error.Fault.ShouldBe(PromptTemplateFault.MissingToken);
        error.Message.ShouldContain("{race}");
    }

    [Fact]
    public void AHeadWithoutCharacterClassIsRefused()
    {
        PromptTemplateException error = Should.Throw<PromptTemplateException>(() =>
            Create(head: "a {race}"));

        error.Fault.ShouldBe(PromptTemplateFault.MissingToken);
        error.Message.ShouldContain("{characterClass}");
    }

    // --- Forme ---------------------------------------------------------------

    [Fact]
    public void AnUnclosedBraceIsRefused()
    {
        PromptTemplateException error = Should.Throw<PromptTemplateException>(() =>
            Create(head: "a {race {characterClass}"));

        error.Fault.ShouldBe(PromptTemplateFault.Malformed);
    }

    [Fact]
    public void AnEmptyTokenIsRefused()
    {
        Should.Throw<PromptTemplateException>(() => Create(head: "a {} {race} {characterClass}"))
            .Fault.ShouldBe(PromptTemplateFault.Malformed);
    }

    [Fact]
    public void ARepeatedKeyInTheOptionalOrderIsRefused()
    {
        PromptTemplateException error = Should.Throw<PromptTemplateException>(() =>
            Create(order: ["weapon", "armour", "weapon"]));

        error.Fault.ShouldBe(PromptTemplateFault.Malformed);
        error.Message.ShouldContain("'weapon'");
    }

    [Fact]
    public void AnEmptyHeadOrFallbackIsRefused()
    {
        Should.Throw<PromptTemplateException>(() => Create(head: "  "))
            .Fault.ShouldBe(PromptTemplateFault.Malformed);

        Should.Throw<PromptTemplateException>(() => Create(fallback: ""))
            .Fault.ShouldBe(PromptTemplateFault.Malformed);
    }

    [Fact]
    public void SubstitutionReplacesEveryOccurrenceOrdinally()
    {
        string result = TemplateToken.Substitute(
            "a {race} {characterClass}, a proud {race}",
            new Dictionary<string, string> { ["race"] = "goblin", ["characterClass"] = "skirmisher" });

        result.ShouldBe("a goblin skirmisher, a proud goblin");
    }

    [Fact]
    public void ASubstitutedValueIsNeverScannedAgain()
    {
        // Chained string.Replace calls would print the class twice here. The
        // values are free user text, so the pass has to be single.
        string result = TemplateToken.Substitute(
            "a {race} {characterClass}",
            new Dictionary<string, string> { ["race"] = "{characterClass}", ["characterClass"] = "goblin" });

        result.ShouldBe("a {characterClass} goblin");
    }

    [Fact]
    public void AnUnknownTokenIsLeftVisibleRatherThanBlanked()
    {
        string result = TemplateToken.Substitute(
            "a {race} {mount}",
            new Dictionary<string, string> { ["race"] = "goblin" });

        result.ShouldBe("a goblin {mount}");
    }
}
