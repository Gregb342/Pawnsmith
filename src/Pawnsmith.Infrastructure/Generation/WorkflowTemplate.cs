using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Infrastructure.Generation;

/// <summary>
/// The ComfyUI workflow graph with its tokens located, and the framing clause
/// that travels with it (§E.5, DEC-076).
/// </summary>
/// <remarks>
/// <para>
/// <b>The graph is modified as a graph, node by node, never as text.</b> A
/// <c>string.Replace</c> on the JSON text would break the document the first
/// time a prompt contains a quotation mark, and — the trap that already cost T3
/// a fix — it would rescan an inserted value for the next token: a prompt
/// containing the literal text <c>{{SEED}}</c> would receive the seed.
/// </para>
/// <para>
/// So the positions of the tokens are found <b>once</b>, when the template is
/// built, and a substitution replaces the nodes at those positions in a copy of
/// the graph, and only them. It never looks at what it has just inserted.
/// </para>
/// <para>
/// <b>Only the tokens and the output node are interpreted.</b> Everything else
/// in the graph — node types, model files, steps — is passed to ComfyUI as it
/// is, and ComfyUI validates it: a wrong graph is refused by the generator with
/// <c>GENERATOR_REJECTED</c>, with its own message. Re-validating ComfyUI's
/// schema here would be a second, always-late copy of it.
/// </para>
/// </remarks>
public sealed class WorkflowTemplate
{
    /// <summary>Replaced by the resolved prompt. Exactly once.</summary>
    public const string PositiveToken = "{{POSITIVE}}";

    /// <summary>Replaced by the project's negative clause. At most once.</summary>
    public const string NegativeToken = "{{NEGATIVE}}";

    /// <summary>Replaced by the candidate's seed, as a JSON number. Exactly once.</summary>
    public const string SeedToken = "{{SEED}}";

    /// <summary>Anything shaped like a token: two braces, a name, two braces.</summary>
    private static readonly Regex TokenShape = new(@"\{\{([^{}]*)\}\}", RegexOptions.CultureInvariant);

    private readonly JsonObject workflow;
    private readonly IReadOnlyList<TokenSite> sites;

    private WorkflowTemplate(string framingClause, string outputNodeId, JsonObject workflow, IReadOnlyList<TokenSite> sites)
    {
        FramingClause = framingClause;
        OutputNodeId = outputNodeId;
        this.workflow = workflow;
        this.sites = sites;
    }

    /// <summary>The framing clause, normalised. Never empty.</summary>
    /// <remarks>
    /// It lives here because DEC-029 makes the workflow template its only
    /// point of access. It leaves this type as a plain string: the use case
    /// receives it as a function argument (§C.5.2), and no port exists to fetch
    /// it.
    /// </remarks>
    public string FramingClause { get; }

    /// <summary>The node whose image is brought back.</summary>
    public string OutputNodeId { get; }

    /// <summary>Whether the graph has a place for the negative clause.</summary>
    public bool HasNegativeToken => sites.Any(site => site.Token == NegativeToken);

    /// <summary>Checks a workflow graph and locates its tokens.</summary>
    /// <param name="framingLines">The lines of the framing clause, joined by <c>\n</c>.</param>
    /// <param name="outputNodeId">Key, in <paramref name="workflow"/>, of the node whose image is brought back.</param>
    /// <param name="workflow">The graph in ComfyUI's API format. Not modified, and not kept by reference.</param>
    /// <param name="source">Where it came from, for the messages.</param>
    /// <exception cref="GeneratorConfigException"><c>WORKFLOW_INVALID</c> or <c>WORKFLOW_UNKNOWN_TOKEN</c>.</exception>
    public static WorkflowTemplate Create(
        IReadOnlyList<string> framingLines,
        string outputNodeId,
        JsonObject workflow,
        string source)
    {
        ArgumentNullException.ThrowIfNull(framingLines);
        ArgumentNullException.ThrowIfNull(outputNodeId);
        ArgumentNullException.ThrowIfNull(workflow);

        // The line feed of the assembly (C.5.3): a line break written in the
        // file reaches the model as a line break, and nothing else.
        string framingClause = ResolvedPrompt.Normalize(string.Join("\n", framingLines));

        if (framingClause.Length == 0)
        {
            throw Invalid(source, "the framing clause is empty. It is what makes the image cuttable (DEC-029)");
        }

        // A private copy: the caller's object could change under us otherwise,
        // and the token positions found below would then point at nothing.
        var graph = (JsonObject)workflow.DeepClone();

        List<TokenSite> sites = [];

        try
        {
            RefuseInterfaceFormat(graph, source);
            Scan(graph, [], sites, source);
        }
        catch (ArgumentException error)
        {
            // JsonNode.Parse accepts a repeated key and only fails when that
            // object is first enumerated — which the scan does, everywhere.
            // Catching it here makes it a coded refusal rather than an argument
            // exception escaping from the middle of a load. Nothing else in the
            // scan throws ArgumentException.
            throw new GeneratorConfigException(
                GeneratorConfigErrorCode.WorkflowInvalid,
                $"The workflow template '{source}' repeats a key: {error.Message}",
                error);
        }

        RequireCount(sites, PositiveToken, minimum: 1, maximum: 1, source);
        RequireCount(sites, SeedToken, minimum: 1, maximum: 1, source);
        RequireCount(sites, NegativeToken, minimum: 0, maximum: 1, source);

        if (graph[outputNodeId] is not JsonObject)
        {
            throw Invalid(source, $"the output node '{outputNodeId}' is not a node of the workflow");
        }

        return new WorkflowTemplate(framingClause, outputNodeId, graph, sites);
    }

    /// <summary>
    /// A copy of the graph with its tokens replaced. The template itself never
    /// changes.
    /// </summary>
    /// <remarks>
    /// The prompt is inserted as a JSON value, and the serialiser escapes it.
    /// Escaping is the encoding of the transport, not a transformation of the
    /// prompt: ComfyUI decodes it back, byte for byte, to the text the candidate
    /// freezes (DEC-077).
    /// </remarks>
    /// <param name="prompt">Goes to <c>{{POSITIVE}}</c>, exactly as given.</param>
    /// <param name="negativePrompt">Goes to <c>{{NEGATIVE}}</c> if the graph has one; ignored otherwise.</param>
    /// <param name="seed">Goes to <c>{{SEED}}</c>, as a number.</param>
    public JsonObject Substitute(string prompt, string negativePrompt, ulong seed)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(negativePrompt);

        var copy = (JsonObject)workflow.DeepClone();

        foreach (TokenSite site in sites)
        {
            JsonNode replacement = site.Token switch
            {
                PositiveToken => JsonValue.Create(prompt),
                NegativeToken => JsonValue.Create(negativePrompt),
                SeedToken => JsonValue.Create(seed),
                _ => throw new InvalidOperationException($"No replacement for the token {site.Token}."),
            };

            Replace(copy, site.Path, replacement);
        }

        return copy;
    }

    /// <summary>Walks the graph, recording every token and refusing every malformed one.</summary>
    private static void Scan(JsonNode? node, List<PathStep> path, List<TokenSite> sites, string source)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (KeyValuePair<string, JsonNode?> property in obj)
                {
                    if (property.Key.Contains("{{", StringComparison.Ordinal) || property.Key.Contains("}}", StringComparison.Ordinal))
                    {
                        throw Invalid(source, $"the key '{property.Key}' at {Describe(path)} holds braces; a token is a value, never a key");
                    }

                    Scan(property.Value, [.. path, new PathStep(property.Key, Index: null)], sites, source);
                }

                break;

            case JsonArray array:
                for (int index = 0; index < array.Count; index++)
                {
                    Scan(array[index], [.. path, new PathStep(Name: null, index)], sites, source);
                }

                break;

            case JsonValue value when value.TryGetValue(out string? text):
                Classify(text, path, sites, source);
                break;

            default:
                // Numbers, booleans and nulls carry no token.
                break;
        }
    }

    /// <summary>Decides what a string value is: a token, ordinary text, or a fault.</summary>
    private static void Classify(string text, List<PathStep> path, List<TokenSite> sites, string source)
    {
        if (text is PositiveToken or NegativeToken or SeedToken)
        {
            sites.Add(new TokenSite(text, [.. path]));
            return;
        }

        if (!text.Contains("{{", StringComparison.Ordinal) && !text.Contains("}}", StringComparison.Ordinal))
        {
            return;
        }

        // Something brace-shaped that is not a whole known token. An unknown
        // name is the most useful thing to report, so it is looked for first.
        foreach (Match match in TokenShape.Matches(text))
        {
            string token = match.Value;

            if (token is not (PositiveToken or NegativeToken or SeedToken))
            {
                throw new GeneratorConfigException(
                    GeneratorConfigErrorCode.WorkflowUnknownToken,
                    $"The workflow template '{source}' uses the unknown token {token} at {Describe(path)}. " +
                    $"The only tokens are {PositiveToken}, {SeedToken} and {NegativeToken}; " +
                    "anything else would reach the generator as literal text.");
            }
        }

        // Only known tokens are left, so one of them is inside a longer text —
        // or there are stray braces with no name between them.
        throw Invalid(
            source,
            $"the value at {Describe(path)} mixes a token with other text. A token must be the whole value, " +
            "because the prompt that is sent must be exactly the resolved prompt (DEC-049, DEC-076)");
    }

    /// <summary>Refuses the interface export, which <c>/prompt</c> does not accept.</summary>
    private static void RefuseInterfaceFormat(JsonObject graph, string source)
    {
        if (graph["nodes"] is JsonArray && graph["links"] is JsonArray)
        {
            throw Invalid(
                source,
                "the workflow is in ComfyUI's interface format (it has 'nodes' and 'links'). " +
                "Export it again with 'Export (API)'");
        }
    }

    private static void RequireCount(List<TokenSite> sites, string token, int minimum, int maximum, string source)
    {
        int count = sites.Count(site => site.Token == token);

        if (count < minimum || count > maximum)
        {
            string expected = minimum == maximum ? $"exactly {minimum}" : $"at most {maximum}";

            throw Invalid(source, $"the token {token} appears {count} time(s); it must appear {expected}");
        }
    }

    private static void Replace(JsonObject root, IReadOnlyList<PathStep> path, JsonNode replacement)
    {
        JsonNode parent = root;

        for (int step = 0; step < path.Count - 1; step++)
        {
            parent = path[step].Name is { } name ? parent[name]! : parent[path[step].Index!.Value]!;
        }

        PathStep last = path[^1];

        if (last.Name is { } key)
        {
            parent[key] = replacement;
        }
        else
        {
            parent[last.Index!.Value] = replacement;
        }
    }

    private static string Describe(IReadOnlyList<PathStep> path) =>
        path.Count == 0
            ? "the root"
            : "workflow" + string.Concat(path.Select(step => step.Name is { } name ? $"['{name}']" : $"[{step.Index}]"));

    private static GeneratorConfigException Invalid(string source, string why) =>
        new(GeneratorConfigErrorCode.WorkflowInvalid, $"The workflow template '{source}' cannot be used: {why}.");

    /// <summary>One step from a node to one of its children: a key in an object, or an index in an array.</summary>
    private sealed record PathStep(string? Name, int? Index);

    /// <summary>Where a token sits in the graph, recorded once at load time.</summary>
    private sealed record TokenSite(string Token, IReadOnlyList<PathStep> Path);
}
