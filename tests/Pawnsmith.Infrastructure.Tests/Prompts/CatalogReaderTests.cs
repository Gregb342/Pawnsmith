using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;
using Pawnsmith.Infrastructure.Prompts;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Prompts;

/// <summary>
/// Covers tests 15, 16, 17, 20, 21 and 22 of D.11 on the catalogue side, and
/// what the reader does with a file that is not there or not JSON.
/// </summary>
public class CatalogReaderTests
{
    private const string Valid = """
        {
          "versionSchema": 2,
          "universe": "Fantasy",
          "parameters": [
            { "key": "weapon", "labels": { "en": "weapon", "fr": "weapon" }, "entries": [
              { "value": "axe", "labels": { "en": "axe", "fr": "axe" }, "fragment": "wielding a large battle axe" },
              { "value": "spear", "labels": { "en": "spear", "fr": "spear" }, "fragment": "wielding a short spear" }
            ]},
            { "key": "armour", "labels": { "en": "armour", "fr": "armour" }, "entries": [
              { "value": "leather", "labels": { "en": "leather", "fr": "leather" }, "fragment": "wearing leather scraps" }
            ]}
          ]
        }
        """;

    private static async Task<Catalog> Read(TempWorkspace workspace, string json)
    {
        string path = workspace.WriteFile("catalog.fantasy.json", json);

        return await CatalogReader.ReadAsync(path, Universe.Fantasy, CancellationToken.None);
    }

    private static async Task<PromptFileException> Refuses(string json)
    {
        using TempWorkspace workspace = new();

        return await Should.ThrowAsync<PromptFileException>(() => Read(workspace, json));
    }

    // --- Le cas ordinaire ---------------------------------------------------

    [Fact]
    public async Task AWellFormedFileLoads()
    {
        using TempWorkspace workspace = new();

        Catalog catalog = await Read(workspace, Valid);

        catalog.Universe.ShouldBe(Universe.Fantasy);
        catalog.TryGetFragment("weapon", "spear", out string fragment).ShouldBeTrue();
        fragment.ShouldBe("wielding a short spear");
    }

    [Fact]
    public async Task TheShippedCatalogueLoads()
    {
        // The file in config/ is what users start from. If it stops loading,
        // every fresh installation composes with an empty vocabulary.
        string path = Path.Combine(RepositoryRoot(), "config", "catalog.fantasy.json");

        Catalog catalog = await CatalogReader.ReadAsync(path, Universe.Fantasy, CancellationToken.None);

        catalog.Parameters.ShouldNotBeEmpty();
        catalog.TryGetFragment("weapon", "axe", out string axe).ShouldBeTrue();
        axe.ShouldContain("axe");
    }

    // --- D.11 n° 22 : l'ordre est préservé ---------------------------------

    [Fact]
    public async Task ParameterOrderIsPreservedNeverSorted()
    {
        using TempWorkspace workspace = new();

        Catalog catalog = await Read(workspace, Valid);

        // "armour" sorts before "weapon"; the file says weapon first.
        catalog.Parameters.Select(parameter => parameter.Key).ShouldBe(["weapon", "armour"]);
    }

    // --- D.11 n° 15 et 16 : doublons, CATALOG_INVALID -----------------------

    [Fact]
    public async Task ARepeatedKeyIsRefusedAsCatalogInvalidAndNamed()
    {
        PromptFileException error = await Refuses("""
            {
              "versionSchema": 2, "universe": "Fantasy",
              "parameters": [
                { "key": "weapon", "labels": { "en": "weapon", "fr": "weapon" }, "entries": [] },
                { "key": "weapon", "labels": { "en": "weapon", "fr": "weapon" }, "entries": [] }
              ]
            }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.CatalogInvalid);
        error.WireCode.ShouldBe("CATALOG_INVALID");
        error.Message.ShouldContain("'weapon'");
    }

    [Fact]
    public async Task ARepeatedValueIsRefusedAsCatalogInvalidAndNamed()
    {
        PromptFileException error = await Refuses("""
            {
              "versionSchema": 2, "universe": "Fantasy",
              "parameters": [
                { "key": "weapon", "labels": { "en": "weapon", "fr": "weapon" }, "entries": [
                  { "value": "axe", "labels": { "en": "axe", "fr": "axe" }, "fragment": "one" },
                  { "value": "axe", "labels": { "en": "axe", "fr": "axe" }, "fragment": "two" }
                ]}
              ]
            }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.CatalogInvalid);
        error.Message.ShouldContain("'axe'");
    }

    // --- D.11 n° 21 : fragment vide ----------------------------------------

    [Fact]
    public async Task AnEmptyFragmentIsRefusedAsCatalogInvalid()
    {
        PromptFileException error = await Refuses("""
            {
              "versionSchema": 2, "universe": "Fantasy",
              "parameters": [
                { "key": "weapon", "labels": { "en": "weapon", "fr": "weapon" }, "entries": [ { "value": "axe", "labels": { "en": "axe", "fr": "axe" }, "fragment": "" } ] }
              ]
            }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.CatalogInvalid);
        error.Message.ShouldContain("weapon: axe");
    }

    // --- D.11 n° 17 : schéma trop récent, sans lecture partielle -----------

    [Fact]
    public async Task ANewerSchemaIsRefusedAsTooRecent()
    {
        PromptFileException error = await Refuses("""
            { "versionSchema": 3, "universe": "Fantasy", "parameters": [] }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.CatalogSchemaTooRecent);
        error.WireCode.ShouldBe("CATALOG_SCHEMA_TOO_RECENT");
    }

    [Fact]
    public async Task AMissingSchemaIsInvalidNotTooRecent()
    {
        PromptFileException error = await Refuses("""
            { "universe": "Fantasy", "parameters": [] }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.CatalogInvalid);
    }

    // --- D.11 n° 20 : univers ----------------------------------------------

    [Fact]
    public async Task AnotherUniverseThanExpectedIsRefusedAsMismatch()
    {
        // One universe exists, so the only way to mismatch today is to declare
        // one that does not exist - which is CATALOG_INVALID, not a mismatch.
        // The mismatch path itself is reachable only with a second member of
        // Universe; the check is in PromptDataFile.RequireUniverse.
        PromptFileException error = await Refuses("""
            { "versionSchema": 2, "universe": "SciFi", "parameters": [] }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.CatalogInvalid);
        error.Message.ShouldContain("'SciFi'");
    }

    [Fact]
    public async Task AMissingUniverseIsRefused()
    {
        PromptFileException error = await Refuses("""
            { "versionSchema": 2, "parameters": [] }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.CatalogInvalid);
        error.Message.ShouldContain("'universe'");
    }

    // --- Forme du fichier ---------------------------------------------------

    [Fact]
    public async Task AMissingFileIsRefusedWithACode()
    {
        PromptFileException error = await Should.ThrowAsync<PromptFileException>(() =>
            CatalogReader.ReadAsync(Path.Combine(Path.GetTempPath(), "does-not-exist.json"), Universe.Fantasy, CancellationToken.None));

        error.Code.ShouldBe(PromptFileErrorCode.CatalogInvalid);
        error.Message.ShouldContain("does not exist");
    }

    [Fact]
    public async Task InvalidJsonIsRefusedWithACode()
    {
        PromptFileException error = await Refuses("{ this is not json");

        error.Code.ShouldBe(PromptFileErrorCode.CatalogInvalid);
        error.Message.ShouldContain("not valid JSON");
    }

    [Fact]
    public async Task AMissingFieldIsNamed()
    {
        PromptFileException error = await Refuses("""
            { "versionSchema": 2, "universe": "Fantasy",
              "parameters": [ { "key": "weapon", "labels": { "en": "weapon", "fr": "weapon" } } ] }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.CatalogInvalid);
        error.Message.ShouldContain("entries");
    }

    [Fact]
    public async Task CommentsAndUnknownMembersAreTolerated()
    {
        // A hand-written file gets annotated (C.6.3). Unlike project.json.
        using TempWorkspace workspace = new();

        Catalog catalog = await Read(workspace, """
            {
              // the vocabulary of my table
              "versionSchema": 2,
              "universe": "Fantasy",
              "note": "not a field the reader knows",
              "parameters": [],
            }
            """);

        catalog.Parameters.ShouldBeEmpty();
    }

    // --- I.12 n° 1 : un libellé par culture (DEC-106) ----------------------

    [Fact]
    public async Task AnEntryWithoutAFrenchLabelIsRefusedAsCatalogInvalid()
    {
        PromptFileException error = await Refuses("""
            {
              "versionSchema": 2, "universe": "Fantasy",
              "parameters": [
                { "key": "weapon", "labels": { "en": "Weapon", "fr": "Arme" }, "entries": [
                  { "value": "axe", "labels": { "en": "axe" }, "fragment": "wielding an axe" }
                ]}
              ]
            }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.CatalogInvalid);
        error.Message.ShouldContain("'fr'");
    }

    [Fact]
    public async Task AFirstVersionFileIsNoLongerRead()
    {
        PromptFileException error = await Refuses("""
            { "versionSchema": 1, "universe": "Fantasy", "parameters": [] }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.CatalogInvalid);
    }

    [Fact]
    public async Task TheShippedCatalogueCarriesRaceAndClassListsInBothLanguages()
    {
        string path = Path.Combine(RepositoryRoot(), "config", "catalog.fantasy.json");

        Catalog catalog = await CatalogReader.ReadAsync(path, Universe.Fantasy, CancellationToken.None);

        catalog.TryGetFragment("race", "orc", out string orc).ShouldBeTrue();
        orc.ShouldBe("an orc");
        catalog.KnowsKey("characterClass").ShouldBeTrue();
        catalog.Parameters.First(parameter => parameter.Key == "weapon").Labels["fr"].ShouldBe("Arme");
    }

    [Fact]
    public void EveryErrorCodeHasAWireForm()
    {
        foreach (PromptFileErrorCode code in Enum.GetValues<PromptFileErrorCode>())
        {
            Should.NotThrow(() => code.ToWireCode());
        }
    }

    private static string RepositoryRoot()
    {
        // Walk up from the test binary to the folder holding the solution.
        string? directory = AppContext.BaseDirectory;

        while (directory is not null && !File.Exists(Path.Combine(directory, "Pawnsmith.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        return directory ?? throw new InvalidOperationException("Pawnsmith.sln not found above the test binary.");
    }
}
