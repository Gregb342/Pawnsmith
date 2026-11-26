using System.Text.Json;
using System.Text.Json.Nodes;

namespace Pawnsmith.Infrastructure.Generation;

/// <summary>
/// The framing clause shipped with the application, <c>config/framing-clause.json</c>,
/// given to a raw ComfyUI export when it is imported (§I.16.2, DEC-120).
/// </summary>
/// <remarks>
/// Read when an import needs it, not at start-up: a deployment that mounts
/// its own <c>config/</c> without this file still starts, and still imports
/// files already prepared, which carry their own clause. A missing or broken
/// file is a fault of the deployment, hence <c>WORKFLOW_INVALID</c>, a 503.
/// </remarks>
public static class FramingClauseFile
{
    /// <summary>The file name, in the configuration directory.</summary>
    public const string FileName = "framing-clause.json";

    /// <summary>The lines of the clause.</summary>
    /// <exception cref="GeneratorConfigException"><c>WORKFLOW_INVALID</c>: missing, not JSON, or without lines.</exception>
    public static async Task<IReadOnlyList<string>> ReadAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (!File.Exists(path))
        {
            throw Invalid(path, "the file does not exist");
        }

        JsonNode? root;

        try
        {
            root = JsonNode.Parse(await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false));
        }
        catch (JsonException error)
        {
            throw new GeneratorConfigException(
                GeneratorConfigErrorCode.WorkflowInvalid,
                $"The framing clause file '{path}' is not valid JSON: {error.Message}",
                error);
        }

        if (root?["framingClause"] is not JsonArray lines || lines.Count == 0)
        {
            throw Invalid(path, "the 'framingClause' field is missing or empty");
        }

        List<string> result = [];

        foreach (JsonNode? line in lines)
        {
            if (line is not JsonValue value || !value.TryGetValue(out string? text))
            {
                throw Invalid(path, "a line of 'framingClause' is not a string");
            }

            result.Add(text);
        }

        return result;
    }

    private static GeneratorConfigException Invalid(string path, string reason) =>
        new(GeneratorConfigErrorCode.WorkflowInvalid, $"The framing clause file '{path}' is refused: {reason}.");
}
