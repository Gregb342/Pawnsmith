using System.Text.Json;

namespace Pawnsmith.Infrastructure;

/// <summary>
/// Reads a JSON file and turns every failure into a message that names the
/// file and says what went wrong.
/// </summary>
/// <remarks>
/// Shared by the calibration and manifest readers so that a missing file reads
/// the same way whichever one it was, rather than surfacing a raw
/// <see cref="FileNotFoundException"/> from one and a JSON exception from the
/// other.
/// </remarks>
internal static class JsonFile
{
    public static async Task<T> ReadAsync<T>(
        string path,
        JsonSerializerOptions options,
        string what,
        CancellationToken cancellationToken)
        where T : class
    {
        if (!File.Exists(path))
        {
            throw new ManifestException($"The {what} '{path}' does not exist.");
        }

        string json;

        try
        {
            json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (IOException error)
        {
            throw new ManifestException($"The {what} '{path}' could not be read: {error.Message}", error);
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, options)
                ?? throw new ManifestException($"The {what} '{path}' is empty.");
        }
        catch (JsonException error)
        {
            // The line and position come from the parser and are the most
            // useful thing to hand back to whoever wrote the file.
            throw new ManifestException(
                $"The {what} '{path}' is not valid JSON: {error.Message}",
                error);
        }
    }
}
