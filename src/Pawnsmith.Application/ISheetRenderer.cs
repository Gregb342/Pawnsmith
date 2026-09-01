using System.Globalization;

using Pawnsmith.Domain;

namespace Pawnsmith.Application;

/// <summary>
/// Turns a resolved layout into a PDF.
/// </summary>
/// <remarks>
/// <b>The renderer decides nothing.</b> It receives a
/// <see cref="SheetLayout"/> in which every position, dimension and polygon is
/// already settled, in millimetres, and traces it. Any arithmetic beyond the
/// single millimetres-to-points conversion is a defect.
/// <para>
/// The culture travels with the request (DEC-023) because the sheet carries
/// text: the calibration caption and the page label. The API stays
/// language-agnostic and returns error codes, never translated messages.
/// </para>
/// <para>
/// This is the only port T1 implements. The others in chapter 7 of the bible
/// are design intent, not code to write today.
/// </para>
/// </remarks>
public interface ISheetRenderer
{
    /// <summary>Renders the layout and returns the PDF bytes.</summary>
    /// <param name="layout">The fully resolved layout.</param>
    /// <param name="culture">Culture of the text printed on the sheet.</param>
    /// <param name="cancellationToken">Cancels a long render.</param>
    Task<byte[]> RenderAsync(
        SheetLayout layout,
        CultureInfo culture,
        CancellationToken cancellationToken);
}
