using System.Text.Json.Nodes;

using Pawnsmith.Infrastructure.Generation;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Generation;

/// <summary>
/// The preparation of a workflow chosen in the interface (§I.16.2, DEC-120).
/// Covers tests 1 to 5 of §I.16.5. Every graph is written out in full, so that
/// each test shows the structure the preparer follows.
/// </summary>
public class WorkflowPreparerTests
{
    private static readonly IReadOnlyList<string> Framing = ["front view on the left, back view on the right,", "plain grey background"];

    private static JsonObject Prepare(string json) => WorkflowPreparer.Prepare(json, Framing);

    private static WorkflowImportException Refused(string json) => Should.Throw<WorkflowImportException>(() => Prepare(json));

    private static JsonNode? Input(JsonObject document, string node, string input) => document["workflow"]![node]!["inputs"]![input];

    /// <summary>The graph of the repository's example, as ComfyUI exports it: no token anywhere.</summary>
    private const string Classic = """
        {
          "1": { "class_type": "UNETLoader", "inputs": { "unet_name": "model.safetensors", "weight_dtype": "default" } },
          "2": { "class_type": "CLIPLoader", "inputs": { "clip_name": "clip.safetensors", "type": "qwen_image" } },
          "3": { "class_type": "VAELoader", "inputs": { "vae_name": "vae.safetensors" } },
          "4": { "class_type": "CLIPTextEncode", "inputs": { "text": "a goblin, plain background", "clip": ["2", 0] } },
          "5": { "class_type": "CLIPTextEncode", "inputs": { "text": "blurry", "clip": ["2", 0] } },
          "6": { "class_type": "EmptySD3LatentImage", "inputs": { "width": 1216, "height": 832, "batch_size": 1 } },
          "7": { "class_type": "KSampler", "inputs": { "seed": 123456789, "steps": 8, "cfg": 1.0, "sampler_name": "euler",
                 "scheduler": "simple", "denoise": 1.0, "model": ["1", 0], "positive": ["4", 0], "negative": ["5", 0], "latent_image": ["6", 0] } },
          "8": { "class_type": "VAEDecode", "inputs": { "samples": ["7", 0], "vae": ["3", 0] } },
          "9": { "class_type": "SaveImage", "inputs": { "filename_prefix": "ComfyUI", "images": ["8", 0] } }
        }
        """;

    // --- §I.16.5 n° 1 : l'exemple, privé de ses jetons ---------------------------------------

    [Fact]
    public void AClassicGraphGetsItsThreeTokensAndItsEnvelope()
    {
        JsonObject document = Prepare(Classic);

        Input(document, "4", "text")!.GetValue<string>().ShouldBe(WorkflowTemplate.PositiveToken);
        Input(document, "5", "text")!.GetValue<string>().ShouldBe(WorkflowTemplate.NegativeToken);
        Input(document, "7", "seed")!.GetValue<string>().ShouldBe(WorkflowTemplate.SeedToken);
        document["outputNodeId"]!.GetValue<string>().ShouldBe("9");
        document["versionSchema"]!.GetValue<int>().ShouldBe(1);
        document["framingClause"]!.AsArray().Select(line => line!.GetValue<string>()).ShouldBe(Framing);

        // Nothing else moved: the steps, the model, the size are the export's.
        Input(document, "7", "steps")!.GetValue<int>().ShouldBe(8);
        Input(document, "6", "width")!.GetValue<int>().ShouldBe(1216);
    }

    [Fact]
    public void ThePreparedDocumentReadsBackAsATemplate()
    {
        JsonObject document = Prepare(Classic);

        WorkflowTemplate template = WorkflowTemplateReader.Parse(document.ToJsonString(), "prepared");

        template.OutputNodeId.ShouldBe("9");
        template.HasNegativeToken.ShouldBeTrue();
    }

    // --- §I.16.5 n° 2 : un graphe de type Flux -----------------------------------------------

    [Fact]
    public void AFluxGraphWithAGuiderAndANoiseNodeIsPreparedWithoutNegative()
    {
        const string flux = """
            {
              "6": { "class_type": "CLIPTextEncode", "inputs": { "text": "a knight", "clip": ["11", 0] } },
              "8": { "class_type": "VAEDecode", "inputs": { "samples": ["13", 0], "vae": ["10", 0] } },
              "9": { "class_type": "SaveImage", "inputs": { "filename_prefix": "flux", "images": ["8", 0] } },
              "13": { "class_type": "SamplerCustomAdvanced", "inputs": { "noise": ["25", 0], "guider": ["22", 0], "sampler": ["16", 0], "sigmas": ["17", 0], "latent_image": ["5", 0] } },
              "22": { "class_type": "BasicGuider", "inputs": { "model": ["12", 0], "conditioning": ["26", 0] } },
              "25": { "class_type": "RandomNoise", "inputs": { "noise_seed": 42 } },
              "26": { "class_type": "FluxGuidance", "inputs": { "guidance": 3.5, "conditioning": ["6", 0] } }
            }
            """;

        JsonObject document = Prepare(flux);

        Input(document, "6", "text")!.GetValue<string>().ShouldBe(WorkflowTemplate.PositiveToken);
        Input(document, "25", "noise_seed")!.GetValue<string>().ShouldBe(WorkflowTemplate.SeedToken);
        document.ToJsonString().ShouldNotContain(WorkflowTemplate.NegativeToken);
    }

    // --- §I.16.5 n° 3 : un négatif à zéro --------------------------------------------------------

    [Fact]
    public void ANegativeThatZeroesThePositiveGetsNoToken()
    {
        const string zeroed = """
            {
              "4": { "class_type": "CLIPTextEncode", "inputs": { "text": "a goblin", "clip": ["2", 0] } },
              "5": { "class_type": "ConditioningZeroOut", "inputs": { "conditioning": ["4", 0] } },
              "7": { "class_type": "KSampler", "inputs": { "seed": 1, "positive": ["4", 0], "negative": ["5", 0] } },
              "9": { "class_type": "SaveImage", "inputs": { "images": ["8", 0] } }
            }
            """;

        JsonObject document = Prepare(zeroed);

        Input(document, "4", "text")!.GetValue<string>().ShouldBe(WorkflowTemplate.PositiveToken);
        document.ToJsonString().ShouldNotContain(WorkflowTemplate.NegativeToken);
    }

    [Fact]
    public void ATextFedByAPrimitiveNodeIsReplacedWhereTheValueLives()
    {
        const string primitive = """
            {
              "3": { "class_type": "PrimitiveStringMultiline", "inputs": { "value": "a goblin" } },
              "4": { "class_type": "CLIPTextEncode", "inputs": { "text": ["3", 0], "clip": ["2", 0] } },
              "7": { "class_type": "KSampler", "inputs": { "seed": 1, "positive": ["4", 0] } },
              "9": { "class_type": "SaveImage", "inputs": { "images": ["8", 0] } }
            }
            """;

        JsonObject document = Prepare(primitive);

        Input(document, "3", "value")!.GetValue<string>().ShouldBe(WorkflowTemplate.PositiveToken);
    }

    // --- §I.16.5 n° 4 : ce qui est refusé, chacun son code ----------------------------------------

    [Fact]
    public void TheOrdinaryExportIsRefused()
    {
        Refused("""{ "last_node_id": 9, "nodes": [], "links": [] }""").Code.ShouldBe(WorkflowImportErrorCode.Invalid);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("[1, 2, 3]")]
    [InlineData("""{ "4": { "class_type": "CLIPTextEncode" } }""")]
    [InlineData("{}")]
    public void WhatIsNotAnApiExportIsInvalid(string json)
    {
        Refused(json).Code.ShouldBe(WorkflowImportErrorCode.Invalid);
    }

    [Fact]
    public void NoSaveImageIsNoOutput()
    {
        Refused(Classic.Replace("\"SaveImage\"", "\"PreviewImage\"", StringComparison.Ordinal)).Code.ShouldBe(WorkflowImportErrorCode.NoOutput);
    }

    [Fact]
    public void TwoSaveImagesAreAmbiguous()
    {
        string two = Classic.Replace(
            "\"9\": {",
            "\"10\": { \"class_type\": \"SaveImage\", \"inputs\": { \"images\": [\"8\", 0] } },\n\"9\": {",
            StringComparison.Ordinal);

        Refused(two).Code.ShouldBe(WorkflowImportErrorCode.Ambiguous);
    }

    [Fact]
    public void ASeedFedByALinkIsNoSeed()
    {
        Refused(Classic.Replace("\"seed\": 123456789", "\"seed\": [\"30\", 0]", StringComparison.Ordinal)).Code.ShouldBe(WorkflowImportErrorCode.NoSeed);
    }

    [Fact]
    public void TwoSeedsAreAmbiguous()
    {
        string two = Classic.Replace(
            "\"9\": {",
            "\"30\": { \"class_type\": \"KSampler\", \"inputs\": { \"seed\": 7 } },\n\"9\": {",
            StringComparison.Ordinal);

        Refused(two).Code.ShouldBe(WorkflowImportErrorCode.Ambiguous);
    }

    [Fact]
    public void AnEncoderWithTwoTextsIsAmbiguous()
    {
        string flux = Classic.Replace(
            "\"4\": { \"class_type\": \"CLIPTextEncode\", \"inputs\": { \"text\": \"a goblin, plain background\", \"clip\": [\"2\", 0] } }",
            "\"4\": { \"class_type\": \"CLIPTextEncodeFlux\", \"inputs\": { \"clip_l\": \"a goblin\", \"t5xxl\": \"a goblin\", \"guidance\": 3.5, \"clip\": [\"2\", 0] } }",
            StringComparison.Ordinal);

        Refused(flux).Code.ShouldBe(WorkflowImportErrorCode.Ambiguous);
    }

    [Fact]
    public void APositiveThatLeadsToNoTextIsNoPrompt()
    {
        Refused(Classic.Replace("\"positive\": [\"4\", 0]", "\"positive\": [\"40\", 0]", StringComparison.Ordinal)).Code.ShouldBe(WorkflowImportErrorCode.NoPrompt);
    }

    [Fact]
    public void AGraphThatLoopsOnItselfEndsInARefusal()
    {
        const string loop = """
            {
              "4": { "class_type": "ConditioningCombine", "inputs": { "conditioning": ["5", 0] } },
              "5": { "class_type": "ConditioningCombine", "inputs": { "conditioning": ["4", 0] } },
              "7": { "class_type": "KSampler", "inputs": { "seed": 1, "positive": ["4", 0] } },
              "9": { "class_type": "SaveImage", "inputs": { "images": ["8", 0] } }
            }
            """;

        Refused(loop).Code.ShouldBe(WorkflowImportErrorCode.NoPrompt);
    }

    [Fact]
    public void ARawExportThatAlreadyCarriesATokenIsInvalid()
    {
        // A second {{POSITIVE}} would send the prompt twice: refused by the
        // same check as any template.
        string twice = Classic.Replace("\"filename_prefix\": \"ComfyUI\"", "\"filename_prefix\": \"{{POSITIVE}}\"", StringComparison.Ordinal);

        Refused(twice).Code.ShouldBe(WorkflowImportErrorCode.Invalid);
    }

    // --- §I.16.5 n° 5 : un fichier déjà préparé --------------------------------------------------

    [Fact]
    public void APreparedFileIsKeptAsItIsWithItsOwnFramingClause()
    {
        JsonObject document = Prepare(WorkflowFixture.File());

        document["framingClause"]!.AsArray().Select(line => line!.GetValue<string>())
            .ShouldBe([WorkflowFixture.FramingFirstLine, WorkflowFixture.FramingSecondLine]);
        Input(document, "4", "text")!.GetValue<string>().ShouldBe(WorkflowTemplate.PositiveToken);
    }

    [Fact]
    public void APreparedFileTheTemplateRulesRefuseIsInvalid()
    {
        Refused(WorkflowFixture.File(outputNodeId: "42")).Code.ShouldBe(WorkflowImportErrorCode.Invalid);
    }

    [Fact]
    public async Task TheShippedFramingClauseFileReads()
    {
        string path = Path.Combine(RepositoryRoot.Path(), "config", FramingClauseFile.FileName);

        IReadOnlyList<string> lines = await FramingClauseFile.ReadAsync(path, CancellationToken.None);

        lines.ShouldNotBeEmpty();
        lines[^1].ShouldBe("Subject:");
    }
}
