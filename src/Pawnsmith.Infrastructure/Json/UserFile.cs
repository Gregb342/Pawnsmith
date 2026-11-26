using System.Text.Json;

namespace Pawnsmith.Infrastructure.Json;

/// <summary>
/// The files of the user directory: personal catalogue, personal styles,
/// generator address (§I.4.2). Written by the application, read back at the
/// next start-up.
/// </summary>
/// <remarks>
/// <para>
/// <b>Written whole, then swapped in.</b> The text goes to a temporary file
/// beside the target, and one <c>File.Move</c> with overwrite replaces the
/// target. A crash in the middle leaves the previous file, never half of the
/// new one — the same reasoning as the project save (C.7.3), with a simpler
/// tool because these files have no backup to keep.
/// </para>
/// <para>
/// Indented JSON, so that the user can read and fix these files by hand: they
/// are theirs.
/// </para>
/// </remarks>
public static class UserFile
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    /// <summary>Writes the document as JSON, creating the directory if needed.</summary>
    public static async Task WriteJsonAsync<T>(string path, T document, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

        string temporary = path + ".tmp";
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(document, Options);

        await File.WriteAllBytesAsync(temporary, bytes, cancellationToken).ConfigureAwait(false);
        File.Move(temporary, path, overwrite: true);
    }
}
