using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;
using Pawnsmith.Infrastructure.Prompts;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Prompts;

/// <summary>
/// Covers tests 17, 18, 19 and 20 of D.11 on the template side.
/// </summary>
public class PromptTemplateReaderTests
{
    private static async Task<PromptTemplate> Read(TempWorkspace workspace, string json)
    {
        string path = workspace.WriteFile("prompt-template.fantasy.json", json);

        return await PromptTemplateReader.ReadAsync(path, Universe.Fantasy, CancellationToken.None);
    }

    private static async Task<PromptFileException> Refuses(string json)
    {
        using TempWorkspace workspace = new();

        return await Should.ThrowAsync<PromptFileException>(() => Read(workspace, json));
    }

    [Fact]
    public async Task AWellFormedFileLoads()
    {
        using TempWorkspace workspace = new();

        PromptTemplate template = await Read(workspace, """
            {
              "versionSchema": 1,
              "universe": "Fantasy",
              "subjectHead": "a {race} {characterClass}",
              "optionalOrder": ["weapon", "armour"],
              "unknownValueFragment": "{value}"
            }
            """);

        template.SubjectHead.ShouldBe("a {race} {characterClass}");
        template.OptionalOrder.ShouldBe(["weapon", "armour"]);
    }

    [Fact]
    public async Task TheShippedTemplateLoads()
    {
        string path = Path.Combine(RepositoryRoot(), "config", "prompt-template.fantasy.json");

        PromptTemplate template = await PromptTemplateReader.ReadAsync(path, Universe.Fantasy, CancellationToken.None);

        template.OptionalOrder.ShouldContain("weapon");
    }

    // --- D.11 n° 18 : jeton inconnu, nommé, TEMPLATE_UNKNOWN_TOKEN ----------

    [Fact]
    public async Task AnUnknownTokenIsRefusedWithItsOwnCodeAndNamed()
    {
        PromptFileException error = await Refuses("""
            {
              "versionSchema": 1, "universe": "Fantasy",
              "subjectHead": "a {race} {characterClass} of {taille} size",
              "optionalOrder": [], "unknownValueFragment": "{value}"
            }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.TemplateUnknownToken);
        error.WireCode.ShouldBe("TEMPLATE_UNKNOWN_TOKEN");
        error.Message.ShouldContain("{taille}");
    }

    // --- D.11 n° 19 : jeton de tête manquant, TEMPLATE_INVALID -------------

    [Fact]
    public async Task AHeadWithoutBothRequiredTokensIsRefusedAsInvalid()
    {
        PromptFileException error = await Refuses("""
            {
              "versionSchema": 1, "universe": "Fantasy",
              "subjectHead": "a {race}",
              "optionalOrder": [], "unknownValueFragment": "{value}"
            }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.TemplateInvalid);
        error.WireCode.ShouldBe("TEMPLATE_INVALID");
        error.Message.ShouldContain("{characterClass}");
    }

    // --- D.11 n° 17 : schéma trop récent -----------------------------------

    [Fact]
    public async Task ANewerSchemaIsRefusedAsTooRecent()
    {
        PromptFileException error = await Refuses("""
            { "versionSchema": 3, "universe": "Fantasy", "subjectHead": "a {race} {characterClass}",
              "optionalOrder": [], "unknownValueFragment": "{value}" }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.TemplateSchemaTooRecent);
    }

    // --- D.11 n° 20 : univers ----------------------------------------------

    [Fact]
    public async Task AnUnknownUniverseIsRefused()
    {
        PromptFileException error = await Refuses("""
            { "versionSchema": 1, "universe": "Steampunk", "subjectHead": "a {race} {characterClass}",
              "optionalOrder": [], "unknownValueFragment": "{value}" }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.TemplateInvalid);
        error.Message.ShouldContain("'Steampunk'");
    }

    // --- Forme ---------------------------------------------------------------

    [Fact]
    public async Task AMissingFieldIsNamed()
    {
        PromptFileException error = await Refuses("""
            { "versionSchema": 1, "universe": "Fantasy", "subjectHead": "a {race} {characterClass}",
              "unknownValueFragment": "{value}" }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.TemplateInvalid);
        error.Message.ShouldContain("'optionalOrder'");
    }

    [Fact]
    public async Task ARepeatedKeyInTheOrderIsRefusedAsInvalid()
    {
        PromptFileException error = await Refuses("""
            { "versionSchema": 1, "universe": "Fantasy", "subjectHead": "a {race} {characterClass}",
              "optionalOrder": ["weapon", "weapon"], "unknownValueFragment": "{value}" }
            """);

        error.Code.ShouldBe(PromptFileErrorCode.TemplateInvalid);
        error.Message.ShouldContain("'weapon'");
    }

    private static string RepositoryRoot()
    {
        string? directory = AppContext.BaseDirectory;

        while (directory is not null && !File.Exists(Path.Combine(directory, "Pawnsmith.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        return directory ?? throw new InvalidOperationException("Pawnsmith.sln not found above the test binary.");
    }
}
