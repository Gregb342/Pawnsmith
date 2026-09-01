using System.Text.Json;
using System.Text.Json.Serialization;

using Pawnsmith.Domain;

namespace Pawnsmith.Infrastructure;

/// <summary>
/// The manifest of B.3, once read and validated.
/// </summary>
/// <param name="Request">What the domain needs: geometry, paper, items.</param>
/// <param name="ImagesDirectory">Directory the image file names are relative to.</param>
/// <param name="Culture">Culture of the text printed on the sheet.</param>
public sealed record Manifest(SheetRequest Request, string ImagesDirectory, string Culture);

/// <summary>
/// Reads and validates the input manifest.
/// </summary>
/// <remarks>
/// B.3 lists what must be checked when the file is opened: images present and
/// readable, size known to the calibration, quantity at least one, paper format
/// known, schema version recognised. Every one of them fails with a message
/// naming the item and the offending value.
/// <para>
/// The images themselves are checked here for existence only. Their dimensions
/// are read later, by <see cref="FileImageSizeReader"/>: a file that exists but
/// is not a PNG is a different failure, reported at that point.
/// </para>
/// </remarks>
public static class ManifestReader
{
    private const int SupportedVersionSchema = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>Reads and validates a manifest against a calibration.</summary>
    /// <param name="path">Path to the manifest file.</param>
    /// <param name="calibration">Calibration the manifest is validated against.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <exception cref="ManifestException">The manifest is unusable, with the reason.</exception>
    public static async Task<Manifest> ReadAsync(
        string path,
        Calibration calibration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(calibration);

        ManifestDocument document = await JsonFile
            .ReadAsync<ManifestDocument>(path, Options, "manifest", cancellationToken)
            .ConfigureAwait(false);

        if (document.VersionSchema != SupportedVersionSchema)
        {
            throw new ManifestException(
                $"Manifest '{path}' declares schema version {document.VersionSchema}; " +
                $"only version {SupportedVersionSchema} is supported.");
        }

        Geometry geometry = ParseGeometry(document.Geometry, path);
        PaperFormat paper = ResolvePaperFormat(document.PaperFormat, calibration, path);
        string imagesDirectory = ResolveImagesDirectory(document.ImagesDirectory, path);

        IReadOnlyList<SheetItem> items = ReadItems(document, calibration, imagesDirectory, path);

        return new Manifest(
            new SheetRequest(geometry, paper, items),
            imagesDirectory,
            document.Culture ?? "en-US");
    }

    private static IReadOnlyList<SheetItem> ReadItems(
        ManifestDocument document,
        Calibration calibration,
        string imagesDirectory,
        string path)
    {
        if (document.Items is null || document.Items.Count == 0)
        {
            throw new ManifestException($"Manifest '{path}' lists no item.");
        }

        List<SheetItem> items = [];

        foreach (ItemDocument item in document.Items)
        {
            string name = item.Name ?? "(unnamed)";
            Size size = ParseSize(item.Size, name, calibration, path);

            if (item.Quantity < 1)
            {
                throw new ManifestException(
                    $"Manifest '{path}': item '{name}' has a quantity of {item.Quantity}; " +
                    "it must be at least 1.");
            }

            string front = RequireImage(item.RectoFile, name, "rectoFile", imagesDirectory, path);
            string back = RequireImage(item.VersoFile, name, "versoFile", imagesDirectory, path);

            items.Add(new SheetItem(name, size, item.Quantity, front, back));
        }

        return items;
    }

    private static Geometry ParseGeometry(string? value, string path)
    {
        if (Enum.TryParse(value, ignoreCase: false, out Geometry geometry))
        {
            return geometry;
        }

        throw new ManifestException(
            $"Manifest '{path}' declares an unknown geometry '{value}'. " +
            $"Known geometries are: {string.Join(", ", Enum.GetNames<Geometry>())}.");
    }

    private static Size ParseSize(string? value, string itemName, Calibration calibration, string path)
    {
        if (!Enum.TryParse(value, ignoreCase: false, out Size size))
        {
            throw new ManifestException(
                $"Manifest '{path}': item '{itemName}' declares an unknown size '{value}'. " +
                $"Known sizes are: {string.Join(", ", Enum.GetNames<Size>())}.");
        }

        if (!calibration.Sizes.ContainsKey(size))
        {
            throw new ManifestException(
                $"Manifest '{path}': item '{itemName}' uses size '{size}', which the " +
                "calibration file does not define.");
        }

        return size;
    }

    private static PaperFormat ResolvePaperFormat(string? name, Calibration calibration, string path)
    {
        if (name is not null && calibration.PaperFormats.TryGetValue(name, out PaperFormat? paper))
        {
            return paper;
        }

        throw new ManifestException(
            $"Manifest '{path}' asks for paper format '{name}', which the calibration file " +
            $"does not define. Known formats are: {string.Join(", ", calibration.PaperFormats.Keys)}.");
    }

    private static string ResolveImagesDirectory(string? directory, string path)
    {
        string manifestDirectory = Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".";
        string resolved = Path.GetFullPath(
            Path.Combine(manifestDirectory, directory ?? "."));

        if (!Directory.Exists(resolved))
        {
            throw new ManifestException(
                $"Manifest '{path}' points at an images directory that does not exist: '{resolved}'.");
        }

        return resolved;
    }

    private static string RequireImage(
        string? fileName,
        string itemName,
        string field,
        string imagesDirectory,
        string path)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ManifestException(
                $"Manifest '{path}': item '{itemName}' has no '{field}'.");
        }

        string full = Path.Combine(imagesDirectory, fileName);

        if (!File.Exists(full))
        {
            throw new ManifestException(
                $"Manifest '{path}': item '{itemName}' refers to '{field}' = '{fileName}', " +
                $"which was not found at '{full}'.");
        }

        return fileName;
    }

    private sealed record ManifestDocument
    {
        [JsonPropertyName("versionSchema")]
        public int VersionSchema { get; init; }

        [JsonPropertyName("geometry")]
        public string? Geometry { get; init; }

        [JsonPropertyName("paperFormat")]
        public string? PaperFormat { get; init; }

        [JsonPropertyName("culture")]
        public string? Culture { get; init; }

        [JsonPropertyName("imagesDirectory")]
        public string? ImagesDirectory { get; init; }

        [JsonPropertyName("items")]
        public List<ItemDocument>? Items { get; init; }
    }

    private sealed record ItemDocument
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("size")]
        public string? Size { get; init; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; init; }

        [JsonPropertyName("rectoFile")]
        public string? RectoFile { get; init; }

        [JsonPropertyName("versoFile")]
        public string? VersoFile { get; init; }
    }
}
