using System.Text.Json.Nodes;

using Pawnsmith.Infrastructure.Generation;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Generation;

/// <summary>
/// Covers tests 10 to 19 of E.12: the workflow template of DEC-076.
/// </summary>
public class WorkflowTemplateTests
{
    private static WorkflowTemplate Parse(string json) => WorkflowTemplateReader.Parse(json, "workflow.test.json");

    private static GeneratorConfigException Refused(string json) =>
        Should.Throw<GeneratorConfigException>(() => Parse(json));

    // --- E.12 n° 10 : un fichier valide ---------------------------------------

    [Fact]
    public void AValidFileReadsAndItsFramingClauseIsItsLinesJoinedByLineFeeds()
    {
        WorkflowTemplate template = Parse(WorkflowFixture.File());

        template.FramingClause.ShouldBe($"{WorkflowFixture.FramingFirstLine}\n{WorkflowFixture.FramingSecondLine}");
        template.OutputNodeId.ShouldBe("9");
        template.HasNegativeToken.ShouldBeTrue();
    }

    [Fact]
    public void TheFramingClauseIsNormalised()
    {
        WorkflowTemplate template = Parse(WorkflowFixture.File(framing: "\"  \", \"  front view\\r\\n\", \"back view   \", \"\""));

        // CRLF reduced to LF, then trimmed as a whole - the rule of C.5.3.
        template.FramingClause.ShouldBe("front view\n\nback view");
    }

    [Fact]
    public async Task TheShippedExampleIsAValidTemplate()
    {
        string path = Path.Combine(RepositoryRoot.Path(), "config", "workflow.comfyui.example.json");

        WorkflowTemplate template = await WorkflowTemplateReader.ReadAsync(path, CancellationToken.None);

        template.FramingClause.ShouldStartWith("Character rotation sheet");
        template.FramingClause.ShouldContain("front view on the left and back view on the");
    }

    [Fact]
    public void CommentsAndUnknownTopLevelMembersAreTolerated()
    {
        string json = WorkflowFixture.File().Replace(
            "\"versionSchema\"",
            "// a note\n  \"_readme\": \"example\",\n  \"versionSchema\"",
            StringComparison.Ordinal);

        Parse(json).OutputNodeId.ShouldBe("9");
    }

    // --- E.12 n° 11 : schéma trop récent ---------------------------------------

    [Fact]
    public void ANewerSchemaIsRefused()
    {
        Refused(WorkflowFixture.File(versionSchema: 2)).Code.ShouldBe(GeneratorConfigErrorCode.WorkflowSchemaTooRecent);
    }

    [Fact]
    public void AnOlderSchemaIsInvalid()
    {
        Refused(WorkflowFixture.File(versionSchema: 0)).Code.ShouldBe(GeneratorConfigErrorCode.WorkflowInvalid);
    }

    // --- E.12 n° 12 et 13 : POSITIVE et SEED exactement une fois ----------------

    [Theory]
    [InlineData("{{POSITIVE}}", "\"text\": \"a fixed text\"")]
    [InlineData("{{SEED}}", "\"seed\": 42")]
    public void AMandatoryTokenThatIsMissingIsRefused(string token, string replacement)
    {
        string graph = WorkflowFixture.Graph
            .Replace("\"text\": \"{{POSITIVE}}\"", token == "{{POSITIVE}}" ? replacement : "\"text\": \"{{POSITIVE}}\"", StringComparison.Ordinal)
            .Replace("\"seed\": \"{{SEED}}\"", token == "{{SEED}}" ? replacement : "\"seed\": \"{{SEED}}\"", StringComparison.Ordinal);

        GeneratorConfigException error = Refused(WorkflowFixture.File(graph));

        error.Code.ShouldBe(GeneratorConfigErrorCode.WorkflowInvalid);
        error.Message.ShouldContain($"{token} appears 0 time(s)");
    }

    [Theory]
    [InlineData("{{POSITIVE}}")]
    [InlineData("{{SEED}}")]
    [InlineData("{{NEGATIVE}}")]
    public void ATokenThatAppearsTwiceIsRefused(string token)
    {
        string graph = WorkflowFixture.Graph.Replace(
            "\"filename_prefix\": \"pawnsmith\"",
            $"\"filename_prefix\": \"{token}\"",
            StringComparison.Ordinal);

        GeneratorConfigException error = Refused(WorkflowFixture.File(graph));

        error.Code.ShouldBe(GeneratorConfigErrorCode.WorkflowInvalid);
        error.Message.ShouldContain($"{token} appears 2 time(s)");
    }

    // --- E.12 n° 14 : NEGATIVE peut manquer --------------------------------------

    [Fact]
    public void AGraphWithoutANegativeTokenIsAccepted()
    {
        string graph = WorkflowFixture.Graph.Replace("{{NEGATIVE}}", "", StringComparison.Ordinal);

        WorkflowTemplate template = Parse(WorkflowFixture.File(graph));

        template.HasNegativeToken.ShouldBeFalse();
    }

    // --- E.12 n° 15 : jeton inconnu, nommé ---------------------------------------

    [Theory]
    [InlineData("{{WIDTH}}")]
    [InlineData("{{STEPS}}")]
    [InlineData("{{positive}}")]
    public void AnUnknownTokenIsRefusedByName(string token)
    {
        string graph = WorkflowFixture.Graph.Replace("\"steps\": 8", $"\"steps\": \"{token}\"", StringComparison.Ordinal);

        GeneratorConfigException error = Refused(WorkflowFixture.File(graph));

        error.Code.ShouldBe(GeneratorConfigErrorCode.WorkflowUnknownToken);
        error.WireCode.ShouldBe("WORKFLOW_UNKNOWN_TOKEN");
        error.Message.ShouldContain(token);
        error.Message.ShouldContain("workflow['7']['inputs']['steps']");
    }

    // --- E.12 n° 16 : jeton noyé dans un texte ------------------------------------

    [Theory]
    [InlineData("{{POSITIVE}}, masterpiece")]
    [InlineData("best quality {{POSITIVE}}")]
    [InlineData("{{POSITIVE}}{{NEGATIVE}}")]
    [InlineData("a stray }} brace")]
    public void ATokenInsideALongerTextIsRefused(string text)
    {
        string graph = WorkflowFixture.Graph.Replace("\"{{POSITIVE}}\"", $"\"{text}\"", StringComparison.Ordinal);

        GeneratorConfigException error = Refused(WorkflowFixture.File(graph));

        error.Code.ShouldBe(GeneratorConfigErrorCode.WorkflowInvalid);
        error.Message.ShouldContain("whole value");
    }

    [Fact]
    public void ATokenUsedAsAKeyIsRefused()
    {
        string graph = WorkflowFixture.Graph.Replace("\"steps\": 8", "\"{{STEPS}}\": 8", StringComparison.Ordinal);

        Refused(WorkflowFixture.File(graph)).Code.ShouldBe(GeneratorConfigErrorCode.WorkflowInvalid);
    }

    // --- E.12 n° 17 : nœud de sortie, cadrage vide, format interface ---------------

    [Fact]
    public void AnOutputNodeAbsentFromTheGraphIsRefused()
    {
        GeneratorConfigException error = Refused(WorkflowFixture.File(outputNodeId: "42"));

        error.Code.ShouldBe(GeneratorConfigErrorCode.WorkflowInvalid);
        error.Message.ShouldContain("'42'");
    }

    [Fact]
    public void AnEmptyFramingClauseIsRefused()
    {
        GeneratorConfigException error = Refused(WorkflowFixture.File(framing: "\"   \", \"\""));

        error.Code.ShouldBe(GeneratorConfigErrorCode.WorkflowInvalid);
        error.Message.ShouldContain("framing clause is empty");
    }

    [Fact]
    public void TheInterfaceFormatIsRefusedWithWhatToDo()
    {
        const string interfaceExport = """
            { "nodes": [ { "id": 4, "widgets_values": ["{{POSITIVE}}"] } ], "links": [] }
            """;

        GeneratorConfigException error = Refused(WorkflowFixture.File(interfaceExport));

        error.Code.ShouldBe(GeneratorConfigErrorCode.WorkflowInvalid);
        error.Message.ShouldContain("Export (API)");
    }

    [Fact]
    public void ARepeatedKeyInsideTheGraphIsACodedRefusal()
    {
        string graph = WorkflowFixture.Graph.Replace("\"steps\": 8", "\"steps\": 8, \"steps\": 9", StringComparison.Ordinal);

        Refused(WorkflowFixture.File(graph)).Code.ShouldBe(GeneratorConfigErrorCode.WorkflowInvalid);
    }

    [Fact]
    public void ARepeatedKeyAtTheTopIsACodedRefusal()
    {
        string json = WorkflowFixture.File().Replace("\"outputNodeId\"", "\"outputNodeId\": \"7\", \"outputNodeId\"", StringComparison.Ordinal);

        Refused(json).Code.ShouldBe(GeneratorConfigErrorCode.WorkflowInvalid);
    }

    [Fact]
    public async Task AMissingFileIsACodedRefusal()
    {
        GeneratorConfigException error = await Should.ThrowAsync<GeneratorConfigException>(() =>
            WorkflowTemplateReader.ReadAsync(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json"), CancellationToken.None));

        error.Code.ShouldBe(GeneratorConfigErrorCode.WorkflowInvalid);
    }

    // --- E.12 n° 18 : la substitution transmet le prompt à l'identique ------------

    [Fact]
    public void TheSubstitutionCarriesAnyPromptVerbatimAndTheSeedAsANumber()
    {
        WorkflowTemplate template = Parse(WorkflowFixture.File());

        // Every character that has broken a textual substitution somewhere:
        // quotes, a backslash, line feeds, non-ASCII, and the literal text of
        // another token, which must NOT receive the seed.
        const string prompt = "a \"quoted\" orc\\shaman\nwearing {{SEED}} and {{NEGATIVE}}, déjà vu, 鬼";
        const string negative = "blurry, \"text\"";
        const ulong seed = 18446744073709551615UL;

        JsonObject graph = template.Substitute(prompt, negative, seed);

        WorkflowFixture.Input(graph, "4", "text")!.GetValue<string>().ShouldBe(prompt);
        WorkflowFixture.Input(graph, "5", "text")!.GetValue<string>().ShouldBe(negative);
        WorkflowFixture.Input(graph, "7", "seed")!.GetValue<ulong>().ShouldBe(seed);

        // And what goes over the wire decodes back to the same text: the
        // serialised graph round-trips through a plain JSON parser.
        var reparsed = (JsonObject)JsonNode.Parse(graph.ToJsonString())!;
        WorkflowFixture.Input(reparsed, "4", "text")!.GetValue<string>().ShouldBe(prompt);
        graph.ToJsonString().ShouldContain("\"seed\":18446744073709551615");
    }

    [Fact]
    public void TheRestOfTheGraphIsPassedOnUntouched()
    {
        WorkflowTemplate template = Parse(WorkflowFixture.File());

        JsonObject graph = template.Substitute("p", "n", 1);

        WorkflowFixture.Input(graph, "7", "steps")!.GetValue<int>().ShouldBe(8);
        WorkflowFixture.Input(graph, "9", "filename_prefix")!.GetValue<string>().ShouldBe("pawnsmith");
        graph["9"]!["class_type"]!.GetValue<string>().ShouldBe("SaveImage");
    }

    [Fact]
    public void ATokenInsideAnArrayIsFoundAndReplaced()
    {
        string graph = WorkflowFixture.Graph.Replace("\"seed\": \"{{SEED}}\"", "\"seeds\": [1, \"{{SEED}}\"]", StringComparison.Ordinal);

        JsonObject substituted = Parse(WorkflowFixture.File(graph)).Substitute("p", "n", 77);

        WorkflowFixture.Input(substituted, "7", "seeds")![1]!.GetValue<ulong>().ShouldBe(77UL);
        WorkflowFixture.Input(substituted, "7", "seeds")![0]!.GetValue<int>().ShouldBe(1);
    }

    // --- E.12 n° 19 : deux substitutions ne se contaminent pas --------------------

    [Fact]
    public void TwoSubstitutionsAreIndependentAndTheTemplateNeverChanges()
    {
        WorkflowTemplate template = Parse(WorkflowFixture.File());

        JsonObject first = template.Substitute("first prompt", "first negative", 1);
        JsonObject second = template.Substitute("second prompt", "second negative", 2);

        WorkflowFixture.Input(first, "4", "text")!.GetValue<string>().ShouldBe("first prompt");
        WorkflowFixture.Input(second, "4", "text")!.GetValue<string>().ShouldBe("second prompt");
        WorkflowFixture.Input(second, "7", "seed")!.GetValue<ulong>().ShouldBe(2UL);

        // Mutating a result must not reach the template either.
        first["4"]!["inputs"]!["text"] = "tampered";
        WorkflowFixture.Input(template.Substitute("third", "n", 3), "4", "text")!.GetValue<string>().ShouldBe("third");
    }
}
