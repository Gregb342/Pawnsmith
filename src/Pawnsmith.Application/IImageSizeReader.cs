using Pawnsmith.Domain;

namespace Pawnsmith.Application;

/// <summary>
/// Measures the pixel dimensions of the source images named by a request.
/// </summary>
/// <remarks>
/// The domain needs the aspect ratio of every image to place it, but must never
/// open a file. This port is the seam: Infrastructure reads the PNG headers,
/// the domain receives numbers.
/// <para>
/// It is deliberately narrow. It does not load, decode or return pixels — the
/// renderer opens the files again for that. Reading a header twice costs
/// nothing next to holding every decoded image of a sheet in memory.
/// </para>
/// </remarks>
public interface IImageSizeReader
{
    /// <summary>
    /// Measures every image named by the items, keyed by file name.
    /// </summary>
    /// <param name="imagesDirectory">Directory the file names are relative to.</param>
    /// <param name="items">Items whose images are to be measured.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    Task<IReadOnlyDictionary<string, SourceImageSize>> MeasureAsync(
        string imagesDirectory,
        IReadOnlyList<SheetItem> items,
        CancellationToken cancellationToken);
}
