using System.Text.Json;
using System.Text.Json.Nodes;

namespace Pawnsmith.Infrastructure.Generation;

/// <summary>
/// Turns a workflow file chosen in the interface into a workflow template
/// (§I.16.2, DEC-120): a raw ComfyUI "Export (API)" gets its three tokens and
/// its envelope; a file already prepared (§E.5) is checked and kept as is.
/// </summary>
/// <remarks>
/// <para>
/// <b>Only the structure of the graph is read.</b> An API export is an object
/// of nodes; each node has a <c>class_type</c> and <c>inputs</c>; an input is
/// either a value or a link, written <c>["4", 0]</c> — the identifier of
/// another node and the index of its output. The tokens are placed by
/// following those links, never by guessing from a model's name or a text.
/// </para>
/// <list type="bullet">
/// <item><c>{{SEED}}</c>: the one input named <c>seed</c> or <c>noise_seed</c>
/// that holds a number.</item>
/// <item><c>{{POSITIVE}}</c>: follow the sampler's <c>positive</c> input — or,
/// with no sampler input of that name, a guider's <c>conditioning</c> — back
/// through the <c>conditioning</c> inputs of the nodes in between
/// (<c>FluxGuidance</c>…), to the node that holds a <c>text</c> or
/// <c>prompt</c> string.</item>
/// <item><c>{{NEGATIVE}}</c>: the same from <c>negative</c>, only if it reaches
/// a text other than the positive one. A <c>ConditioningZeroOut</c> of the
/// positive leads back to the positive text, and gets no token.</item>
/// <item>The output: the one <c>SaveImage</c> node.</item>
/// </list>
/// <para>
/// <b>Anything ambiguous is refused, never chosen.</b> Two outputs, two seeds,
/// an encoder with two texts: the user prepares that file by hand, as §E.5
/// describes. A wrong guess would cost a batch of images of the wrong prompt;
/// a refusal costs a minute.
/// </para>
/// </remarks>
public static class WorkflowPreparer
{
    /// <summary>The source name used in messages: the file has no path of its own yet.</summary>
    private const string Source = "the imported workflow";

    /// <summary>Inputs that hold a node's text, in a text encoder.</summary>
    private static readonly string[] TextInputs = ["text", "prompt"];

    /// <summary>Inputs that hold a seed, in a sampler or a noise node.</summary>
    private static readonly string[] SeedInputs = ["seed", "noise_seed"];

    /// <summary>Inputs that hold a value passed on by a primitive node a text links to.</summary>
    private static readonly string[] PrimitiveValueInputs = ["value", "text", "string"];

    /// <summary>How many nodes a trace may cross: a graph with a loop must not hang the request.</summary>
    private const int MaxHops = 32;

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Prepares <paramref name="json"/>, or refuses it with a code.</summary>
    /// <param name="json">The file the user chose.</param>
    /// <param name="framingLines">
    /// Gives the framing clause for a raw export (DEC-120); a prepared file
    /// keeps its own. A function, called only for a raw export: a deployment
    /// without the shipped clause file still imports prepared files.
    /// </param>
    /// <returns>The template document, ready to be saved, already checked by <see cref="WorkflowTemplate.Create"/>.</returns>
    /// <exception cref="WorkflowImportException">The file cannot be prepared.</exception>
    public static JsonObject Prepare(string json, Func<IReadOnlyList<string>> framingLines)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentNullException.ThrowIfNull(framingLines);

        JsonObject root = ParseRoot(json);

        if (root.ContainsKey("workflow"))
        {
            return KeepPrepared(json, root);
        }

        if (root.ContainsKey("nodes") && root.ContainsKey("links"))
        {
            throw Refuse(
                WorkflowImportErrorCode.Invalid,
                "This is the ordinary ComfyUI export (nodes, links). Use \"Export (API)\" instead.");
        }

        Dictionary<string, Node> nodes = ReadNodes(root);

        string output = FindOutput(nodes);
        (string Node, string Input) seed = FindSeed(nodes);
        (string Node, string Input) positive = FindPositive(nodes);
        (string Node, string Input)? negative = FindNegative(nodes, positive);

        nodes[seed.Node].Inputs[seed.Input] = WorkflowTemplate.SeedToken;
        nodes[positive.Node].Inputs[positive.Input] = WorkflowTemplate.PositiveToken;

        if (negative is { } site)
        {
            nodes[site.Node].Inputs[site.Input] = WorkflowTemplate.NegativeToken;
        }

        var document = new JsonObject
        {
            ["versionSchema"] = WorkflowTemplateReader.SupportedVersionSchema,
            ["framingClause"] = new JsonArray([.. framingLines().Select(line => (JsonNode?)JsonValue.Create(line))]),
            ["outputNodeId"] = output,
            ["workflow"] = root,
        };

        // The same check as any template read from disk: three tokens, each
        // a whole value, the output node present. A raw export that already
        // carried "{{POSITIVE}}" somewhere fails here, as it should.
        Check(document.ToJsonString());

        return document;
    }

    private static JsonObject ParseRoot(string json)
    {
        JsonNode? node;

        try
        {
            node = JsonNode.Parse(json, nodeOptions: null, DocumentOptions);
        }
        catch (JsonException error)
        {
            throw Refuse(WorkflowImportErrorCode.Invalid, $"The file is not valid JSON: {error.Message}", error);
        }

        if (node is not JsonObject root)
        {
            throw Refuse(WorkflowImportErrorCode.Invalid, "The root of the file is not a JSON object.");
        }

        try
        {
            // A repeated key only fails when an object is first enumerated;
            // enumerating here makes it a coded refusal (as in the reader).
            _ = root.Count;
        }
        catch (ArgumentException error)
        {
            throw Refuse(WorkflowImportErrorCode.Invalid, $"The file repeats a key: {error.Message}", error);
        }

        return root;
    }

    /// <summary>A file already in the §E.5 envelope: checked, kept as it is, its framing clause included.</summary>
    private static JsonObject KeepPrepared(string json, JsonObject root)
    {
        Check(json);
        return root;
    }

    private static void Check(string templateJson)
    {
        try
        {
            WorkflowTemplateReader.Parse(templateJson, Source);
        }
        catch (GeneratorConfigException error)
        {
            throw Refuse(WorkflowImportErrorCode.Invalid, error.Message, error);
        }
    }

    /// <summary>One node of the export: its type and its inputs, as the graph holds them.</summary>
    private sealed record Node(string ClassType, JsonObject Inputs);

    private static Dictionary<string, Node> ReadNodes(JsonObject root)
    {
        Dictionary<string, Node> nodes = new(StringComparer.Ordinal);

        try
        {
            foreach ((string id, JsonNode? value) in root)
            {
                if (value is not JsonObject node
                    || node["class_type"] is not JsonValue type
                    || !type.TryGetValue(out string? classType)
                    || node["inputs"] is not JsonObject inputs)
                {
                    throw Refuse(
                        WorkflowImportErrorCode.Invalid,
                        $"The entry '{id}' is not a node of an API export (class_type and inputs).");
                }

                _ = inputs.Count;
                nodes[id] = new Node(classType, inputs);
            }
        }
        catch (ArgumentException error)
        {
            throw Refuse(WorkflowImportErrorCode.Invalid, $"The file repeats a key: {error.Message}", error);
        }

        if (nodes.Count == 0)
        {
            throw Refuse(WorkflowImportErrorCode.Invalid, "The file holds no node.");
        }

        return nodes;
    }

    private static string FindOutput(Dictionary<string, Node> nodes)
    {
        string[] outputs = [.. nodes.Where(node => node.Value.ClassType == "SaveImage").Select(node => node.Key)];

        return outputs.Length switch
        {
            0 => throw Refuse(WorkflowImportErrorCode.NoOutput, "The workflow has no SaveImage node: nothing says which image to bring back."),
            1 => outputs[0],
            _ => throw Refuse(
                WorkflowImportErrorCode.Ambiguous,
                $"The workflow has {outputs.Length} SaveImage nodes ({string.Join(", ", outputs)}); exactly one is expected."),
        };
    }

    private static (string Node, string Input) FindSeed(Dictionary<string, Node> nodes)
    {
        List<(string Node, string Input)> seeds = [];

        foreach ((string id, Node node) in nodes)
        {
            foreach (string input in SeedInputs)
            {
                // A number, not a link: a seed fed by another node is not a
                // place where a value can be written.
                if (node.Inputs[input] is JsonValue value && value.GetValueKind() == JsonValueKind.Number)
                {
                    seeds.Add((id, input));
                }
            }
        }

        return seeds.Count switch
        {
            0 => throw Refuse(WorkflowImportErrorCode.NoSeed, "The workflow has no input 'seed' or 'noise_seed' holding a number."),
            1 => seeds[0],
            _ => throw Refuse(
                WorkflowImportErrorCode.Ambiguous,
                $"The workflow has {seeds.Count} seeds ({string.Join(", ", seeds.Select(seed => $"{seed.Node}.{seed.Input}"))}); exactly one is expected."),
        };
    }

    private static (string Node, string Input) FindPositive(Dictionary<string, Node> nodes)
    {
        List<string> starts = Links(nodes, "positive");

        if (starts.Count == 0)
        {
            // No sampler input named "positive": a custom sampler fed by a
            // guide - BasicGuider and its kind - takes one conditioning.
            starts = [.. nodes
                .Where(node => node.Value.ClassType.EndsWith("Guider", StringComparison.Ordinal))
                .SelectMany(node => LinkTarget(node.Value.Inputs["conditioning"]) is { } target ? [target] : Array.Empty<string>())];
        }

        HashSet<(string Node, string Input)> texts = [];

        foreach (string start in starts)
        {
            if (Trace(nodes, start) is { } text)
            {
                texts.Add(text);
            }
        }

        return texts.Count switch
        {
            0 => throw Refuse(WorkflowImportErrorCode.NoPrompt, "The positive conditioning of the workflow leads to no text input."),
            1 => texts.Single(),
            _ => throw Refuse(WorkflowImportErrorCode.Ambiguous, $"The positive conditioning leads to {texts.Count} different texts; exactly one is expected."),
        };
    }

    private static (string Node, string Input)? FindNegative(Dictionary<string, Node> nodes, (string Node, string Input) positive)
    {
        HashSet<(string Node, string Input)> texts = [];

        foreach (string start in Links(nodes, "negative"))
        {
            // A negative that leads back to the positive text - a
            // ConditioningZeroOut of it - has nothing of its own to replace.
            if (Trace(nodes, start) is { } text && text != positive)
            {
                texts.Add(text);
            }
        }

        return texts.Count switch
        {
            0 => null,
            1 => texts.Single(),
            _ => throw Refuse(WorkflowImportErrorCode.Ambiguous, $"The negative conditioning leads to {texts.Count} different texts; at most one is expected."),
        };
    }

    /// <summary>The nodes the input <paramref name="name"/> of any node links to.</summary>
    private static List<string> Links(Dictionary<string, Node> nodes, string name) =>
        [.. nodes.Values.SelectMany(node => LinkTarget(node.Inputs[name]) is { } target ? [target] : Array.Empty<string>())];

    /// <summary>The node a link <c>["id", index]</c> points to, or null when the value is not a link.</summary>
    private static string? LinkTarget(JsonNode? value) =>
        value is JsonArray { Count: 2 } link
        && link[0] is JsonValue id
        && id.TryGetValue(out string? target)
            ? target
            : null;

    /// <summary>
    /// Follows a conditioning back to the text that produced it: the node and
    /// the input to write the token into, or null when no text is reached.
    /// </summary>
    private static (string Node, string Input)? Trace(Dictionary<string, Node> nodes, string start)
    {
        string? current = start;

        for (int hop = 0; hop < MaxHops && current is not null; hop++)
        {
            if (!nodes.TryGetValue(current, out Node? node))
            {
                return null;
            }

            string[] texts = [.. TextInputs.Where(input => node.Inputs.ContainsKey(input))];

            if (texts.Length > 1)
            {
                throw Refuse(WorkflowImportErrorCode.Ambiguous, $"The node '{current}' ({node.ClassType}) has more than one text input.");
            }

            if (texts.Length == 1)
            {
                return TextSite(nodes, current, node, texts[0]);
            }

            // An encoder with no "text" and no "prompt" holds its text under
            // other names - CLIPTextEncodeFlux has two, clip_l and t5xxl.
            // Which one the prompt belongs in is a choice, not a fact of the
            // graph: refused.
            if (node.ClassType.Contains("TextEncode", StringComparison.Ordinal))
            {
                throw Refuse(
                    WorkflowImportErrorCode.Ambiguous,
                    $"The text encoder '{current}' ({node.ClassType}) has no single text input; prepare this workflow by hand.");
            }

            current = LinkTarget(node.Inputs["conditioning"]);
        }

        return null;
    }

    /// <summary>The text input itself, or the primitive node it links to.</summary>
    private static (string Node, string Input)? TextSite(Dictionary<string, Node> nodes, string id, Node node, string input)
    {
        JsonNode? value = node.Inputs[input];

        if (value is JsonValue text && text.GetValueKind() == JsonValueKind.String)
        {
            return (id, input);
        }

        // A text fed by a primitive node: the value lives there.
        if (LinkTarget(value) is { } source
            && nodes.TryGetValue(source, out Node? primitive)
            && PrimitiveValueInputs.FirstOrDefault(name => primitive.Inputs[name] is JsonValue held && held.GetValueKind() == JsonValueKind.String) is { } held)
        {
            return (source, held);
        }

        return null;
    }

    private static WorkflowImportException Refuse(WorkflowImportErrorCode code, string message) => new(code, message);

    private static WorkflowImportException Refuse(WorkflowImportErrorCode code, string message, Exception inner) => new(code, message, inner);
}
