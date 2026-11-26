using System.Text.Json;
using System.Text.Json.Serialization;

using Pawnsmith.Infrastructure.Json;

namespace Pawnsmith.Api.Hosting;

/// <summary>
/// The generator address chosen in the interface: <c>generator.json</c> in the
/// user directory (§I.5.1, DEC-108).
/// </summary>
/// <remarks>
/// <para>
/// <b>The file wins over the configuration.</b> It holds the user's last
/// explicit choice; <c>Pawnsmith:Generator:Url</c> applies only while the file
/// does not exist. A file holding <c>null</c> is a choice too — "no generator" —
/// and is kept as such.
/// </para>
/// <para>
/// Only the address, never the workflow: the workflow is ComfyUI's export, a
/// configuration file, not a setting of the screen.
/// </para>
/// </remarks>
public sealed class GeneratorAddressFile(string userDirectory)
{
    public string Path { get; } = System.IO.Path.Combine(userDirectory, "generator.json");

    /// <summary>Whether a choice was saved, and which.</summary>
    /// <returns><c>(false, null)</c> without a file; <c>(true, address)</c> with one, the address possibly <c>null</c>.</returns>
    /// <exception cref="InvalidOperationException">The file exists and is not the document this class writes.</exception>
    public async Task<(bool Saved, string? Address)> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(Path))
        {
            return (false, null);
        }

        try
        {
            await using FileStream stream = File.OpenRead(Path);
            Document? document = await JsonSerializer.DeserializeAsync<Document>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            return (true, document?.Address);
        }
        catch (JsonException error)
        {
            // A start-up failure that names the file: the user can fix or
            // delete it, which brings back the configuration's address.
            throw new InvalidOperationException($"The generator address file '{Path}' is not valid JSON: {error.Message}", error);
        }
    }

    public Task WriteAsync(string? address, CancellationToken cancellationToken) =>
        UserFile.WriteJsonAsync(Path, new Document { Address = address }, cancellationToken);

    private sealed record Document
    {
        [JsonPropertyName("address")]
        public string? Address { get; init; }
    }
}
