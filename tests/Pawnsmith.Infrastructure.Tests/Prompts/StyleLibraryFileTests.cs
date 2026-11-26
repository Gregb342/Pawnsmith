using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;
using Pawnsmith.Infrastructure.Prompts;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Prompts;

/// <summary>The style library on disk (§I.7.2).</summary>
public class StyleLibraryFileTests
{
    [Fact]
    public async Task TheShippedLibraryLoadsWithANameInEachLanguage()
    {
        string path = Path.Combine(RepositoryRoot.Path(), "config", "styles.fantasy.json");

        IReadOnlyList<StylePreset> styles = await StyleLibraryFile.ReadShippedAsync(path, Universe.Fantasy, CancellationToken.None);

        styles.ShouldNotBeEmpty();
        styles.ShouldAllBe(style => style.Origin == StyleOrigin.Shipped && InterfaceCulture.All.All(style.Names.ContainsKey));
    }

    [Fact]
    public async Task AShippedStyleWithoutAFrenchNameIsRefusedWithACode()
    {
        using TempWorkspace workspace = new();
        string path = workspace.WriteFile("styles.fantasy.json", """
            { "versionSchema": 1, "universe": "Fantasy",
              "styles": [ { "id": "ink", "names": { "en": "Ink" }, "styleClause": "Ink drawing." } ] }
            """);

        PromptFileException error = await Should.ThrowAsync<PromptFileException>(() =>
            StyleLibraryFile.ReadShippedAsync(path, Universe.Fantasy, CancellationToken.None));

        error.WireCode.ShouldBe("STYLES_INVALID");
    }

    [Fact]
    public async Task PersonalStylesAreAbsentThenReadBackAsWritten()
    {
        using TempWorkspace workspace = new();
        var file = new StyleLibraryFile(workspace.Root);

        (await file.ReadAsync(Universe.Fantasy, CancellationToken.None)).ShouldBeEmpty();

        var names = new Dictionary<string, string>(StringComparer.Ordinal) { ["fr"] = "Mien", ["en"] = "Mien" };
        await file.WriteAsync(Universe.Fantasy, [new StylePreset("abc", names, "Watercolour.", "text", StyleOrigin.Personal)], CancellationToken.None);

        StylePreset read = (await file.ReadAsync(Universe.Fantasy, CancellationToken.None)).ShouldHaveSingleItem();
        read.Id.ShouldBe("abc");
        read.NegativeClause.ShouldBe("text");
        read.Origin.ShouldBe(StyleOrigin.Personal);
    }
}
