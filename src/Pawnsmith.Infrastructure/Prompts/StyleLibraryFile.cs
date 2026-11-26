using System.Text.Json.Serialization;

using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;
using Pawnsmith.Infrastructure.Json;

namespace Pawnsmith.Infrastructure.Prompts;

/// <summary>
/// The files of the style library (§I.7.2, DEC-110): the shipped
/// <c>config/styles.{universe}.json</c>, read once, and the user's
/// <c>styles.{universe}.json</c> in the user directory, read and written.
/// </summary>
/// <remarks>
/// One format for both, so that a personal style can be moved into the
/// shipped file by copy. The regime is that of the other hand-written data
/// files (C.6.3): comments and trailing commas tolerated, unknown members
/// ignored.
/// </remarks>
public sealed class StyleLibraryFile(string userDirectory) : IPersonalStyleStore
{
    /// <summary>Schema version this reader understands.</summary>
    public const int SupportedVersionSchema = 1;

    private const string What = "style library file";

    /// <summary>Reads and checks the shipped library.</summary>
    /// <exception cref="PromptFileException"><c>STYLES_INVALID</c>, <c>STYLES_SCHEMA_TOO_RECENT</c> or <c>UNIVERSE_MISMATCH</c>.</exception>
    public static async Task<IReadOnlyList<StylePreset>> ReadShippedAsync(string path, Universe expected, CancellationToken cancellationToken)
    {
        IReadOnlyList<StylePreset> presets = await ReadAsync(path, expected, StyleOrigin.Shipped, cancellationToken).ConfigureAwait(false);

        try
        {
            return StylePresets.Check(presets);
        }
        catch (StylePresetException error)
        {
            throw new PromptFileException(PromptFileErrorCode.StylesInvalid, $"The {What} '{path}' is not coherent: {error.Message}", error);
        }
    }

    public async Task<IReadOnlyList<StylePreset>> ReadAsync(Universe universe, CancellationToken cancellationToken)
    {
        string path = PathOf(universe);

        // Absent until the user saves a first style.
        return File.Exists(path)
            ? await ReadAsync(path, universe, StyleOrigin.Personal, cancellationToken).ConfigureAwait(false)
            : [];
    }

    public Task WriteAsync(Universe universe, IReadOnlyList<StylePreset> personal, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(personal);

        var document = new LibraryDocument
        {
            VersionSchema = SupportedVersionSchema,
            Universe = universe.ToString(),
            Styles = [.. personal.Select(preset => new StyleDocument
            {
                Id = preset.Id,

                // Sorted by culture: the same styles must give the same file.
                Names = preset.Names
                    .OrderBy(name => name.Key, StringComparer.Ordinal)
                    .ToDictionary(name => name.Key, name => name.Value, StringComparer.Ordinal),
                StyleClause = preset.StyleClause,
                NegativeClause = preset.NegativeClause,
            })],
        };

        return UserFile.WriteJsonAsync(PathOf(universe), document, cancellationToken);
    }

    private static async Task<IReadOnlyList<StylePreset>> ReadAsync(string path, Universe expected, StyleOrigin origin, CancellationToken cancellationToken)
    {
        LibraryDocument document = await PromptDataFile
            .ReadAsync<LibraryDocument>(path, What, PromptFileErrorCode.StylesInvalid, cancellationToken)
            .ConfigureAwait(false);

        PromptDataFile.RequireSchema(
            document.VersionSchema, SupportedVersionSchema, What, path,
            PromptFileErrorCode.StylesSchemaTooRecent, PromptFileErrorCode.StylesInvalid);

        PromptDataFile.RequireUniverse(document.Universe, expected, What, path, PromptFileErrorCode.StylesInvalid);
        PromptDataFile.Require(document.Styles, "styles", What, path, PromptFileErrorCode.StylesInvalid);

        List<StylePreset> presets = [];

        foreach (StyleDocument style in document.Styles!)
        {
            PromptDataFile.Require(style.Id, "styles[].id", What, path, PromptFileErrorCode.StylesInvalid);
            PromptDataFile.Require(style.StyleClause, "styles[].styleClause", What, path, PromptFileErrorCode.StylesInvalid);

            presets.Add(new StylePreset(
                style.Id!,
                style.Names is null ? new Dictionary<string, string>(StringComparer.Ordinal) : new Dictionary<string, string>(style.Names, StringComparer.Ordinal),
                style.StyleClause!,
                style.NegativeClause ?? string.Empty,
                origin));
        }

        return presets;
    }

    private string PathOf(Universe universe) =>
        Path.Combine(userDirectory, $"styles.{universe.ToString().ToLowerInvariant()}.json");

    // --- Documents de sérialisation, calqués sur le fichier -----------------

    private sealed record LibraryDocument
    {
        [JsonPropertyName("versionSchema")]
        public int VersionSchema { get; init; }

        [JsonPropertyName("universe")]
        public string? Universe { get; init; }

        [JsonPropertyName("styles")]
        public List<StyleDocument>? Styles { get; init; }
    }

    private sealed record StyleDocument
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("names")]
        public Dictionary<string, string>? Names { get; init; }

        [JsonPropertyName("styleClause")]
        public string? StyleClause { get; init; }

        [JsonPropertyName("negativeClause")]
        public string? NegativeClause { get; init; }
    }
}
