using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Sheets;
using Pawnsmith.Domain.Units;
using Pawnsmith.Infrastructure;

namespace Pawnsmith.Infrastructure.Imaging;

/// <summary>
/// Reads the pixel dimensions of PNG files from their header.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing is decoded here.</b> A PNG carries its width and height in the
/// first chunk of the file, at a fixed offset, so 24 bytes are enough. Decoding
/// the image to ask it its size would mean holding every picture of a sheet in
/// memory to learn two numbers.
/// </para>
/// <para>
/// It is also where MEN-005 is held for the sheet (DEC-095). Rendering the sheet
/// makes PDFsharp <i>decode</i> every elected image, and the memory that takes is
/// written in the header: width × height × 4 bytes. A PNG of a few kilobytes,
/// imported from someone else's archive, can announce 60 000 × 60 000 pixels —
/// fourteen gigabytes, one HTTP request away since T6. A side longer than
/// <see cref="MaxImageDimensionPx"/> is therefore refused here, on the header,
/// before anything is decoded. The background remover of T5 will need its own
/// bound; this one is the sheet's.
/// </para>
/// <para>
/// Written by hand rather than taken from a library, per A.2: twenty lines beat
/// a dependency when the doubt is between the two.
/// </para>
/// </remarks>
public sealed class FileImageSizeReader : IImageSizeReader
{
    /// <summary>The eight bytes every PNG file starts with.</summary>
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Signature, then the IHDR chunk header, then width and height.</summary>
    private const int HeaderLength = 24;

    /// <summary>
    /// The longest side accepted, in pixels. The value is T4's own
    /// <c>MaxImageDimensionPx</c>, the bound on what the generator may write: a
    /// legitimate image — a generated pair cut in two, then cut out at the same
    /// size — never exceeds it. At the bound, one image decodes to 256 MiB.
    /// </summary>
    public int MaxImageDimensionPx { get; init; } = 8192;

    public async Task<IReadOnlyDictionary<string, SourceImageSize>> MeasureAsync(
        string imagesDirectory,
        IReadOnlyList<SheetItem> items,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);

        Dictionary<string, SourceImageSize> sizes = [];

        foreach (SheetItem item in items)
        {
            foreach (string fileName in new[] { item.FrontImageFile, item.BackImageFile })
            {
                if (sizes.ContainsKey(fileName))
                {
                    // Several items may share an image; measure it once.
                    continue;
                }

                sizes[fileName] = await MeasureOneAsync(
                    Path.Combine(imagesDirectory, fileName),
                    fileName,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        return sizes;
    }

    private async Task<SourceImageSize> MeasureOneAsync(
        string path,
        string fileName,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            throw new ManifestException($"Image '{fileName}' was not found at '{path}'.");
        }

        byte[] header = new byte[HeaderLength];

        await using (FileStream stream = File.OpenRead(path))
        {
            int read = await stream.ReadAtLeastAsync(
                header,
                HeaderLength,
                throwOnEndOfStream: false,
                cancellationToken).ConfigureAwait(false);

            if (read < HeaderLength)
            {
                throw new ManifestException(
                    $"Image '{fileName}' is too short to be a PNG file ({read} bytes).");
            }
        }

        if (!header.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
        {
            throw new ManifestException(
                $"Image '{fileName}' is not a PNG file: its signature does not match. " +
                "T1 expects the cut-out PNGs described in B.1.");
        }

        // Bytes 16 to 23 are the IHDR width and height, big-endian, which is
        // the byte order the PNG format specifies regardless of the machine.
        int widthPx = ReadBigEndianInt32(header, 16);
        int heightPx = ReadBigEndianInt32(header, 20);

        if (widthPx <= 0 || heightPx <= 0)
        {
            throw new ManifestException(
                $"Image '{fileName}' declares impossible dimensions: {widthPx} × {heightPx}.");
        }

        if (widthPx > MaxImageDimensionPx || heightPx > MaxImageDimensionPx)
        {
            throw new ManifestException(
                $"Image '{fileName}' declares {widthPx} × {heightPx} pixels, more than the {MaxImageDimensionPx} " +
                "allowed on a side; it is refused before being decoded (MEN-005, DEC-095).");
        }

        return new SourceImageSize(widthPx, heightPx);
    }

    private static int ReadBigEndianInt32(byte[] buffer, int offset)
    {
        return (buffer[offset] << 24)
            | (buffer[offset + 1] << 16)
            | (buffer[offset + 2] << 8)
            | buffer[offset + 3];
    }
}
