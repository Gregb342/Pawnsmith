using System.Text.Json;

using Pawnsmith.Domain.Projects;
using Pawnsmith.Infrastructure.Json;

namespace Pawnsmith.Infrastructure.Prompts;

/// <summary>
/// What the catalogue and template readers share: the JSON rules of a
/// hand-written data file, the schema check, and the universe check.
/// </summary>
/// <remarks>
/// <para>
/// These two files are written by the user, like <c>calibration.json</c> and
/// unlike <c>project.json</c>, so they get the same regime (C.6.3): comments
/// and trailing commas tolerated, and an unknown member <b>accepted and
/// ignored</b> — a file people annotate should not break on an annotation.
/// </para>
/// <para>
/// The universe is checked against what the <i>caller</i> expects, not against
/// the file name. The name is a convention the caller uses to find the file;
/// the field is the contract.
/// </para>
/// </remarks>
internal static class PromptDataFile
{
    /// <summary>Schema version both readers understand.</summary>
    public const int SupportedVersionSchema = 1;

    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Reads the file, turning the codeless failures of <see cref="JsonFile"/> into coded ones.</summary>
    public static async Task<T> ReadAsync<T>(
        string path,
        string what,
        PromptFileErrorCode invalid,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            return await JsonFile.ReadAsync<T>(path, Options, what, cancellationToken).ConfigureAwait(false);
        }
        catch (ManifestException error)
        {
            // JsonFile already names the file and says what went wrong; only
            // the code is missing.
            throw new PromptFileException(invalid, error.Message, error);
        }
    }

    public static void RequireSchema(int versionSchema, string what, string path, PromptFileErrorCode tooRecent, PromptFileErrorCode invalid)
    {
        if (versionSchema > SupportedVersionSchema)
        {
            throw new PromptFileException(
                tooRecent,
                $"The {what} '{path}' declares schema version {versionSchema}; " +
                $"only version {SupportedVersionSchema} is supported.");
        }

        if (versionSchema != SupportedVersionSchema)
        {
            throw new PromptFileException(
                invalid,
                $"The {what} '{path}' declares schema version {versionSchema}; " +
                $"it must be {SupportedVersionSchema}.");
        }
    }

    public static Universe RequireUniverse(string? declared, Universe expected, string what, string path, PromptFileErrorCode invalid)
    {
        if (string.IsNullOrWhiteSpace(declared))
        {
            throw new PromptFileException(invalid, $"The {what} '{path}' is missing the 'universe' field.");
        }

        if (!Enum.TryParse(declared, ignoreCase: false, out Universe universe))
        {
            throw new PromptFileException(
                invalid,
                $"The {what} '{path}' declares an unknown universe '{declared}'. " +
                $"Known universes are: {string.Join(", ", Enum.GetNames<Universe>())}.");
        }

        if (universe != expected)
        {
            throw new PromptFileException(
                PromptFileErrorCode.UniverseMismatch,
                $"The {what} '{path}' is for the universe {universe}, but {expected} was expected.");
        }

        return universe;
    }

    public static void Require(object? value, string field, string what, string path, PromptFileErrorCode invalid)
    {
        if (value is null)
        {
            throw new PromptFileException(invalid, $"The {what} '{path}' is missing the '{field}' field.");
        }
    }
}
