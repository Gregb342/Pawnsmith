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
    private const string What = "catalogue file";

    /// <summary>Reads and validates a catalogue file for the given universe.</summary>
    /// <exception cref="PromptFileException">The file is missing, malformed, incoherent, too recent, or for another universe.</exception>
    public static async Task<Catalog> ReadAsync(string path, Universe expected, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        CatalogDocument document = await PromptDataFile
            .ReadAsync<CatalogDocument>(path, What, PromptFileErrorCode.CatalogInvalid, cancellationToken)
            .ConfigureAwait(false);

        PromptDataFile.RequireSchema(
            document.VersionSchema, What, path,
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

                entries.Add(new CatalogEntry(entry.Value!, entry.Fragment!));
            }

            parameters.Add(new CatalogParameter(parameter.Key!, entries));
        }

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

    // --- Documents de sérialisation --------------------------------------
    // Calqués sur le fichier, distincts des types du domaine, qui ne portent
    // aucun attribut de sérialisation (A.3).

    private sealed record CatalogDocument
    {
        [JsonPropertyName("versionSchema")]
        public int VersionSchema { get; init; }

        [JsonPropertyName("universe")]
        public string? Universe { get; init; }

        [JsonPropertyName("parameters")]
        public List<ParameterDocument>? Parameters { get; init; }
    }

    private sealed record ParameterDocument
    {
        [JsonPropertyName("key")]
        public string? Key { get; init; }

        [JsonPropertyName("entries")]
        public List<EntryDocument>? Entries { get; init; }
    }

    private sealed record EntryDocument
    {
        [JsonPropertyName("value")]
        public string? Value { get; init; }

        [JsonPropertyName("fragment")]
        public string? Fragment { get; init; }
    }
}
