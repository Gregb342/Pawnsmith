using Pawnsmith.Domain.Prompts;
using Pawnsmith.Domain.Tests.Fixtures;

namespace Pawnsmith.Domain.Tests.Prompts;

/// <summary>The coherence of a style library (DEC-110).</summary>
public class StylePresetTests
{
    private static StylePreset Preset(string id = "ink", string clause = "Ink drawing.") =>
        new(id, PromptFixture.Labels("Ink"), clause, string.Empty);

    [Fact]
    public void AWellFormedListPasses()
    {
        StylePresets.Check([Preset("ink"), Preset("gouache")]).Count.ShouldBe(2);
    }

    [Fact]
    public void ARepeatedIdentifierIsRefusedAndNamed()
    {
        Should.Throw<StylePresetException>(() => StylePresets.Check([Preset("ink"), Preset("ink")])).Message.ShouldContain("'ink'");
    }

    [Fact]
    public void AMissingFrenchNameIsRefused()
    {
        var englishOnly = new Dictionary<string, string>(StringComparer.Ordinal) { ["en"] = "Ink" };

        Should.Throw<StylePresetException>(() => StylePresets.Check([new StylePreset("ink", englishOnly, "Ink drawing.", "")]))
            .Message.ShouldContain("'fr'");
    }

    [Fact]
    public void AnEmptyStyleClauseIsRefused()
    {
        Should.Throw<StylePresetException>(() => StylePresets.Check([Preset(clause: " ")]));
    }
}
