using System.Text.Json.Serialization;

using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Infrastructure.Prompts;

/// <summary>
/// Reads <c>config/catalog.{universe}.json</c> into the domain's catalogue
/// (§D.4.5).
/// </summary>
/// <remarks>
/// <para>
/// The mapping is written out by hand, field by field (DEC-021). The file's
/// shape — which fields exist — is checked here; what makes a catalogue
/// coherent — no repeated key, no empty fragment — is checked by
/// <see cref="Catalog.Create"/>, and this reader only turns that refusal into
/// <c>CATALOG_INVALID</c>. One truth, one place.
/// </para>
/// <para>
/// <c>parameters</c> is an array and stays one on the way through: its order
/// is what the composition rule reads when the template says nothing
/// (test 22 of D.11).
/// </para>
/// </remarks>
public static class CatalogReader
{
    /// <summary>
    /// Schema version this reader understands. Version 2 added the labels of
    /// every parameter and entry (DEC-106); version 1 is no longer read — the
    /// only such file was the shipped one, rewritten in the same commit.
    /// </summary>
    public const int SupportedVersionSchema = 2;

    private const string What = "catalogue file";

    /// <summary>Reads and validates a catalogue file for the given universe.</summary>
    /// <exception cref="PromptFileException">The file is missing, malformed, incoherent, too recent, or for another universe.</exception>
    public static async Task<Catalog> ReadAsync(string path, Universe expected, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        (Universe universe, List<CatalogParameter> parameters) = await ReadParametersAsync(path, expected, cancellationToken).ConfigureAwait(false);

        try
        {
            return Catalog.Create(universe, parameters);
        }
        catch (CatalogException error)
        {
            throw new PromptFileException(
                PromptFileErrorCode.CatalogInvalid,
                $"The {What} '{path}' is not coherent: {error.Message}",
                error);
        }
    }

    /// <summary>
    /// The parameters of a catalogue file, read and shape-checked but not yet
    /// checked for coherence — shared with the personal catalogue, whose
    /// coherence is only known once merged with the shipped one (DEC-107).
    /// </summary>
    internal static async Task<(Universe Universe, List<CatalogParameter> Parameters)> ReadParametersAsync(
        string path,
        Universe expected,
        CancellationToken cancellationToken)
    {
        CatalogDocument document = await PromptDataFile
            .ReadAsync<CatalogDocument>(path, What, PromptFileErrorCode.CatalogInvalid, cancellationToken)
            .ConfigureAwait(false);

        PromptDataFile.RequireSchema(
            document.VersionSchema, SupportedVersionSchema, What, path,
            PromptFileErrorCode.CatalogSchemaTooRecent, PromptFileErrorCode.CatalogInvalid);

        Universe universe = PromptDataFile.RequireUniverse(
            document.Universe, expected, What, path, PromptFileErrorCode.CatalogInvalid);

        PromptDataFile.Require(document.Parameters, "parameters", What, path, PromptFileErrorCode.CatalogInvalid);

        List<CatalogParameter> parameters = [];

        foreach (ParameterDocument parameter in document.Parameters!)
        {
            PromptDataFile.Require(parameter.Key, "parameters[].key", What, path, PromptFileErrorCode.CatalogInvalid);
            PromptDataFile.Require(parameter.Entries, "parameters[].entries", What, path, PromptFileErrorCode.CatalogInvalid);

            List<CatalogEntry> entries = [];

            foreach (EntryDocument entry in parameter.Entries!)
            {
                PromptDataFile.Require(entry.Value, "parameters[].entries[].value", What, path, PromptFileErrorCode.CatalogInvalid);
                PromptDataFile.Require(entry.Fragment, "parameters[].entries[].fragment", What, path, PromptFileErrorCode.CatalogInvalid);

                entries.Add(new CatalogEntry(entry.Value!, entry.Fragment!, Labels(entry.Labels)));
            }

            parameters.Add(new CatalogParameter(parameter.Key!, Labels(parameter.Labels), entries));
        }

        return (universe, parameters);
    }

    // An absent object becomes an empty dictionary: the domain then names the
    // missing label, culture by culture, instead of the reader saying only that
    // something is missing. Copied with an ordinal comparer, like every
    // dictionary of free strings in the project.
    private static Dictionary<string, string> Labels(Dictionary<string, string>? labels) =>
        labels is null ? new(StringComparer.Ordinal) : new(labels, StringComparer.Ordinal);

    // --- Documents de sérialisation --------------------------------------
    // Calqués sur le fichier, distincts des types du domaine, qui ne portent
    // aucun attribut de sérialisation (A.3).

    internal sealed record CatalogDocument
    {
        [JsonPropertyName("versionSchema")]
        public int VersionSchema { get; init; }

        [JsonPropertyName("universe")]
        public string? Universe { get; init; }

        [JsonPropertyName("parameters")]
        public List<ParameterDocument>? Parameters { get; init; }
    }

    internal sealed record ParameterDocument
    {
        [JsonPropertyName("key")]
        public string? Key { get; init; }

        // Absent from a personal file, which takes the shipped labels.
        [JsonPropertyName("labels")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Dictionary<string, string>? Labels { get; init; }

        [JsonPropertyName("entries")]
        public List<EntryDocument>? Entries { get; init; }
    }

    internal sealed record EntryDocument
    {
        [JsonPropertyName("value")]
        public string? Value { get; init; }

        [JsonPropertyName("labels")]
        public Dictionary<string, string>? Labels { get; init; }

        [JsonPropertyName("fragment")]
        public string? Fragment { get; init; }
    }
}
