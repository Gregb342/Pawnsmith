using System.Globalization;

using Pawnsmith.Domain;

namespace Pawnsmith.Application;

/// <summary>
/// What T1 does, end to end: a request plus a calibration become a PDF.
/// </summary>
/// <remarks>
/// The use case orchestrates and nothing more. It holds no geometry — that is
/// the domain's — and no file handling — that is Infrastructure's, behind the
/// two ports it depends on.
/// </remarks>
public sealed class RenderSheetUseCase
{
    private readonly IImageSizeReader _imageSizeReader;
    private readonly ISheetRenderer _renderer;

    public RenderSheetUseCase(IImageSizeReader imageSizeReader, ISheetRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(imageSizeReader);
        ArgumentNullException.ThrowIfNull(renderer);

        _imageSizeReader = imageSizeReader;
        _renderer = renderer;
    }

    /// <summary>Produces the PDF for one request, with what the domain noticed.</summary>
    /// <param name="request">Geometry, paper format and items to lay out.</param>
    /// <param name="calibration">Physical values.</param>
    /// <param name="imagesDirectory">Directory the image file names are relative to.</param>
    /// <param name="culture">Culture of the text printed on the sheet (DEC-023).</param>
    /// <param name="cancellationToken">Cancels the work.</param>
    public async Task<RenderedSheet> ExecuteAsync(
        SheetRequest request,
        Calibration calibration,
        string imagesDirectory,
        CultureInfo culture,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(calibration);

        // Three steps, in this order and no other: the images have to be
        // measured before the domain can place them, and the domain has to
        // resolve everything before the renderer has anything to trace.
        IReadOnlyDictionary<string, SourceImageSize> imageSizes =
            await _imageSizeReader.MeasureAsync(imagesDirectory, request.Items, cancellationToken)
                .ConfigureAwait(false);

        SheetLayout layout = SheetLayoutBuilder.Build(request, calibration, imageSizes);

        byte[] pdf = await _renderer.RenderAsync(layout, culture, cancellationToken)
            .ConfigureAwait(false);

        // The layout travels back with the bytes rather than being thrown away:
        // it carries the width-limited items of DEC-042, which the caller has to
        // be able to report. A sheet that silently prints short pawns is exactly
        // what that decision refuses.
        return new RenderedSheet(pdf, layout);
    }
}

/// <summary>The produced PDF, and what the domain noticed while producing it.</summary>
/// <param name="Pdf">The PDF bytes.</param>
/// <param name="Layout">The resolved layout, including its diagnostics.</param>
public sealed record RenderedSheet(byte[] Pdf, SheetLayout Layout);
