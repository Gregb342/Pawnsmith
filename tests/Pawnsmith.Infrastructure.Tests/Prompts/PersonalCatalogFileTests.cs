using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;
using Pawnsmith.Infrastructure.Prompts;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Prompts;

/// <summary>The personal catalogue on disk (§I.4.2): absent, written, read back.</summary>
public class PersonalCatalogFileTests
{
    private static readonly Dictionary<string, string> Labels =
        new(StringComparer.Ordinal) { ["fr"] = "hallebarde", ["en"] = "halberd" };

    [Fact]
    public async Task NoFileMeansNoPersonalEntry()
    {
        using TempWorkspace workspace = new();

        (await new PersonalCatalogFile(workspace.Root).ReadAsync(Universe.Fantasy, CancellationToken.None)).ShouldBeEmpty();
    }

    [Fact]
    public async Task WhatIsWrittenIsReadBackInTheShippedFormat()
    {
        using TempWorkspace workspace = new();
        var file = new PersonalCatalogFile(Path.Combine(workspace.Root, "user"));
        CatalogParameter weapon = new("weapon", new Dictionary<string, string>(StringComparer.Ordinal),
            [new CatalogEntry("halberd", "holding a halberd upright", Labels, CatalogEntryOrigin.Personal)]);

        await file.WriteAsync(Universe.Fantasy, [weapon], CancellationToken.None);
        IReadOnlyList<CatalogParameter> read = await file.ReadAsync(Universe.Fantasy, CancellationToken.None);

        CatalogEntry entry = read.ShouldHaveSingleItem().Entries.ShouldHaveSingleItem();
        entry.Value.ShouldBe("halberd");
        entry.Fragment.ShouldBe("holding a halberd upright");
        entry.Labels["fr"].ShouldBe("hallebarde");

        string written = await File.ReadAllTextAsync(Path.Combine(workspace.Root, "user", "catalog.fantasy.json"));
        written.ShouldContain("\"versionSchema\": 2");
        written.ShouldNotContain("\"labels\": null");

        // Labels in culture order, whatever the dictionary's own order.
        written.IndexOf("\"en\"", StringComparison.Ordinal).ShouldBeLessThan(written.IndexOf("\"fr\"", StringComparison.Ordinal));
        Directory.GetFiles(Path.Combine(workspace.Root, "user")).Select(Path.GetFileName).ShouldBe(["catalog.fantasy.json"]);
    }

    [Fact]
    public async Task ABrokenFileIsRefusedWithACode()
    {
        using TempWorkspace workspace = new();
        workspace.WriteFile("catalog.fantasy.json", "{ not json");

        PromptFileException error = await Should.ThrowAsync<PromptFileException>(() =>
            new PersonalCatalogFile(workspace.Root).ReadAsync(Universe.Fantasy, CancellationToken.None));

        error.WireCode.ShouldBe("CATALOG_INVALID");
    }
}
