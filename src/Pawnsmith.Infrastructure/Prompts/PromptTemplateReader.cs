using System.Text.Json.Serialization;

using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Infrastructure.Prompts;

/// <summary>
/// Reads <c>config/prompt-template.{universe}.json</c> into the domain's
/// template (§D.5.2).
/// </summary>
/// <remarks>
/// The tokens are the domain's business: <see cref="PromptTemplate.Create"/>
/// knows the closed list and refuses an unknown one by name. This reader maps
/// that refusal to <c>TEMPLATE_UNKNOWN_TOKEN</c>, and every other fault of the
/// template to <c>TEMPLATE_INVALID</c> — two codes, because a user who typed
/// <c>{taille}</c> and a user who deleted a field are not told the same thing.
/// </remarks>
public static class PromptTemplateReader
{
    private const string What = "prompt template file";

    /// <summary>Reads and validates a template file for the given universe.</summary>
    /// <exception cref="PromptFileException">The file is missing, malformed, carries a bad token, is too recent, or is for another universe.</exception>
    public static async Task<PromptTemplate> ReadAsync(string path, Universe expected, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        TemplateDocument document = await PromptDataFile
            .ReadAsync<TemplateDocument>(path, What, PromptFileErrorCode.TemplateInvalid, cancellationToken)
            .ConfigureAwait(false);

        PromptDataFile.RequireSchema(
            document.VersionSchema, What, path,
            PromptFileErrorCode.TemplateSchemaTooRecent, PromptFileErrorCode.TemplateInvalid);

        Universe universe = PromptDataFile.RequireUniverse(
            document.Universe, expected, What, path, PromptFileErrorCode.TemplateInvalid);

        PromptDataFile.Require(document.SubjectHead, "subjectHead", What, path, PromptFileErrorCode.TemplateInvalid);
        PromptDataFile.Require(document.OptionalOrder, "optionalOrder", What, path, PromptFileErrorCode.TemplateInvalid);
        PromptDataFile.Require(document.UnknownValueFragment, "unknownValueFragment", What, path, PromptFileErrorCode.TemplateInvalid);

        try
        {
            return PromptTemplate.Create(
                universe,
                document.SubjectHead!,
                document.OptionalOrder!,
                document.UnknownValueFragment!);
        }
        catch (PromptTemplateException error)
        {
            PromptFileErrorCode code = error.Fault == PromptTemplateFault.UnknownToken
                ? PromptFileErrorCode.TemplateUnknownToken
                : PromptFileErrorCode.TemplateInvalid;

            throw new PromptFileException(code, $"The {What} '{path}' cannot be used: {error.Message}", error);
        }
    }

    private sealed record TemplateDocument
    {
        [JsonPropertyName("versionSchema")]
        public int VersionSchema { get; init; }

        [JsonPropertyName("universe")]
        public string? Universe { get; init; }

        [JsonPropertyName("subjectHead")]
        public string? SubjectHead { get; init; }

        [JsonPropertyName("optionalOrder")]
        public List<string>? OptionalOrder { get; init; }

        [JsonPropertyName("unknownValueFragment")]
        public string? UnknownValueFragment { get; init; }
    }
}
