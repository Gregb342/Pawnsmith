using System.Text.Json.Nodes;

namespace Pawnsmith.Infrastructure.Tests.Generation;

/// <summary>
/// Workflow template files for the tests, written as text so that each test
/// shows exactly what the reader receives.
/// </summary>
internal static class WorkflowFixture
{
    /// <summary>A minimal graph in API format: two encoders, a sampler, a save node.</summary>
    public const string Graph = """
        {
          "4": { "class_type": "CLIPTextEncode", "inputs": { "text": "{{POSITIVE}}", "clip": ["2", 0] } },
          "5": { "class_type": "CLIPTextEncode", "inputs": { "text": "{{NEGATIVE}}", "clip": ["2", 0] } },
          "7": { "class_type": "KSampler", "inputs": { "seed": "{{SEED}}", "steps": 8, "cfg": 1.0 } },
          "9": { "class_type": "SaveImage", "inputs": { "filename_prefix": "pawnsmith", "images": ["8", 0] } }
        }
        """;

    public const string FramingFirstLine = "front view on the left and back view on the right,";
    public const string FramingSecondLine = "full body, plain grey background";

    /// <summary>A whole template file around <paramref name="graph"/>.</summary>
    public static string File(
        string graph = Graph,
        int versionSchema = 1,
        string outputNodeId = "9",
        string framing = $"\"{FramingFirstLine}\", \"{FramingSecondLine}\"")
    {
        return $$"""
            {
              "versionSchema": {{versionSchema}},
              "framingClause": [ {{framing}} ],
              "outputNodeId": "{{outputNodeId}}",
              "workflow": {{graph}}
            }
            """;
    }

    /// <summary>The value at a node's input in a substituted graph.</summary>
    public static JsonNode? Input(JsonObject graph, string nodeId, string input) =>
        graph[nodeId]!["inputs"]![input];
}
