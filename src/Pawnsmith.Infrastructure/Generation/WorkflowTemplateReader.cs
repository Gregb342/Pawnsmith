using System.Text.Json;
using System.Text.Json.Nodes;

namespace Pawnsmith.Infrastructure.Generation;

/// <summary>
/// Reads <c>config/workflow.comfyui.json</c> into a <see cref="WorkflowTemplate"/>
/// (§E.5.1).
/// </summary>
/// <remarks>
/// <para>
/// The file is written by hand, like <c>calibration.json</c> and the files of
/// T3, so it gets their regime: comments and trailing commas tolerated, and an
/// unknown member at the top level <b>accepted and ignored</b> — which is how the
/// shipped example carries its <c>_readme</c> without the reader knowing about
/// it.
/// </para>
/// <para>
/// It is read as a tree of JSON nodes rather than deserialised into a record,
/// because its <c>workflow</c> member is ComfyUI's graph: an object whose keys
/// are node identifiers the application cannot know in advance, and which is
/// passed on as it is. A record would have to declare it as a node tree anyway.
/// </para>
/// </remarks>
public static class WorkflowTemplateReader
{
    /// <summary>The only schema version this build understands.</summary>
    public const int SupportedVersionSchema = 1;

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Reads and validates a workflow template file.</summary>
    /// <exception cref="GeneratorConfigException">
    /// <c>WORKFLOW_INVALID</c>, <c>WORKFLOW_UNKNOWN_TOKEN</c> or
    /// <c>WORKFLOW_SCHEMA_TOO_RECENT</c>.
    /// </exception>
    public static async Task<WorkflowTemplate> ReadAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (!File.Exists(path))
        {
            throw Invalid(path, "the file does not exist");
        }

        string json;

        try
        {
            json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException error)
        {
            throw new GeneratorConfigException(
                GeneratorConfigErrorCode.WorkflowInvalid,
                $"The workflow template '{path}' could not be read: {error.Message}",
                error);
        }

        return Parse(json, path);
    }

    /// <summary>Validates the text of a workflow template file.</summary>
    /// <param name="json">The file's content.</param>
    /// <param name="source">Where it came from, for the messages.</param>
    public static WorkflowTemplate Parse(string json, string source)
    {
        ArgumentNullException.ThrowIfNull(json);

        JsonObject root = ParseRoot(json, source);

        int versionSchema = RequireVersion(root, source);

        if (versionSchema > SupportedVersionSchema)
        {
            throw new GeneratorConfigException(
                GeneratorConfigErrorCode.WorkflowSchemaTooRecent,
                $"The workflow template '{source}' declares schema version {versionSchema}; " +
                $"only version {SupportedVersionSchema} is supported.");
        }

        if (versionSchema != SupportedVersionSchema)
        {
            throw Invalid(source, $"it declares schema version {versionSchema}; it must be {SupportedVersionSchema}");
        }

        IReadOnlyList<string> framingLines = RequireLines(root, "framingClause", source);
        string outputNodeId = RequireString(root, "outputNodeId", source);

        if (root["workflow"] is not JsonObject workflow)
        {
            throw Invalid(source, "the 'workflow' field is missing or is not an object");
        }

        return WorkflowTemplate.Create(framingLines, outputNodeId, workflow, source);
    }

    private static JsonObject ParseRoot(string json, string source)
    {
        JsonNode? node;

        try
        {
            node = JsonNode.Parse(json, nodeOptions: null, DocumentOptions);
        }
        catch (JsonException error)
        {
            // The line and position come from the parser, and are the most
            // useful thing to hand back to whoever wrote the file.
            throw new GeneratorConfigException(
                GeneratorConfigErrorCode.WorkflowInvalid,
                $"The workflow template '{source}' is not valid JSON: {error.Message}",
                error);
        }

        if (node is not JsonObject root)
        {
            throw Invalid(source, "its root is not a JSON object");
        }

        try
        {
            // A repeated key only fails when an object is first materialised,
            // and asking for its count does that. Doing it here makes the
            // failure a coded refusal; the graph is guarded the same way by
            // WorkflowTemplate.
            _ = root.Count;
        }
        catch (ArgumentException error)
        {
            throw new GeneratorConfigException(
                GeneratorConfigErrorCode.WorkflowInvalid,
                $"The workflow template '{source}' repeats a key: {error.Message}",
                error);
        }

        return root;
    }

    private static int RequireVersion(JsonObject root, string source)
    {
        if (root["versionSchema"] is JsonValue value && value.TryGetValue(out int version))
        {
            return version;
        }

        throw Invalid(source, "the 'versionSchema' field is missing or is not an integer");
    }

    private static string RequireString(JsonObject root, string field, string source)
    {
        if (root[field] is JsonValue value && value.TryGetValue(out string? text) && text.Length > 0)
        {
            return text;
        }

        throw Invalid(source, $"the '{field}' field is missing, empty, or not a string");
    }

    private static List<string> RequireLines(JsonObject root, string field, string source)
    {
        if (root[field] is not JsonArray array)
        {
            throw Invalid(source, $"the '{field}' field is missing or is not an array of lines");
        }

        List<string> lines = [];

        for (int index = 0; index < array.Count; index++)
        {
            if (array[index] is not JsonValue value || !value.TryGetValue(out string? line))
            {
                throw Invalid(source, $"line {index} of '{field}' is not a string");
            }

            lines.Add(line);
        }

        return lines;
    }

    private static GeneratorConfigException Invalid(string source, string why) =>
        new(GeneratorConfigErrorCode.WorkflowInvalid, $"The workflow template '{source}' cannot be used: {why}.");
}
