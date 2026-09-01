using System.Globalization;
using System.Resources;

using Pawnsmith.Application;
using Pawnsmith.Domain;

using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace Pawnsmith.Infrastructure;

/// <summary>
/// Draws a resolved layout into a PDF, using PDFsharp (DEC-019).
/// </summary>
/// <remarks>
/// <para>
/// <b>This class decides nothing.</b> Every position, dimension and polygon it
/// draws was settled by the domain, in millimetres. If a calculation appears
/// here beyond <see cref="Points"/>, it is a defect.
/// </para>
/// <para>
/// <b>All millimetre-to-point conversion goes through <see cref="Points"/>.</b>
/// One function, called everywhere, never inlined. Scattered conversions are
/// the classic way this kind of work goes wrong: one forgotten call and a pawn
/// comes out at 35% of its size, which looks plausible enough to print.
/// </para>
/// </remarks>
public sealed class PdfSharpSheetRenderer : ISheetRenderer
{
    /// <summary>
    /// A PDF point is 1/72 inch, and an inch is 25.4 mm.
    /// </summary>
    /// <remarks>
    /// This is a unit definition, not a physical value to be calibrated: it is
    /// how the PDF format defines its own coordinate system.
    /// </remarks>
    private const double PointsPerMillimetre = 72.0 / 25.4;

    private static readonly ResourceManager Strings =
        new("Pawnsmith.Infrastructure.SheetStrings", typeof(PdfSharpSheetRenderer).Assembly);

    /// <summary>
    /// PDFsharp holds its font resolver in a global, and refuses to have it
    /// replaced once a document has been rendered. Installing it once, on first
    /// use, is the only way to stay correct when several renders run in the
    /// same process — as they do under the test runner.
    /// </summary>
    private static readonly object FontResolverGate = new();

    private static bool _fontResolverInstalled;

    /// <summary>Font of the two strings printed on the sheet.</summary>
    private static readonly XFont SheetFont = CreateSheetFont();

    private static XFont CreateSheetFont()
    {
        InstallFontResolver();

        return new XFont(SheetFontResolver.FontFamilyName, 7);
    }

    private static void InstallFontResolver()
    {
        lock (FontResolverGate)
        {
            if (_fontResolverInstalled)
            {
                return;
            }

            GlobalFontSettings.FontResolver = SheetFontResolver.Instance;
            _fontResolverInstalled = true;
        }
    }

    private readonly string _imagesDirectory;
    private readonly bool _annotateOrientation;

    /// <param name="imagesDirectory">Directory the layout's file names are relative to.</param>
    /// <param name="annotateOrientation">
    /// Prints "head" and "feet" inside each panel. <b>Diagnostics only, off by
    /// default, never on a sheet meant to be cut.</b> See
    /// <see cref="DrawOrientationAnnotations"/> for why it exists.
    /// </param>
    public PdfSharpSheetRenderer(string imagesDirectory, bool annotateOrientation = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imagesDirectory);

        _imagesDirectory = imagesDirectory;
        _annotateOrientation = annotateOrientation;
    }

    /// <summary>The single conversion from millimetres to PDF points.</summary>
    private static double Points(double millimetres)
    {
        return millimetres * PointsPerMillimetre;
    }

    public Task<byte[]> RenderAsync(
        SheetLayout layout,
        CultureInfo culture,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(culture);

        using PdfDocument document = new();
        // Producer is set by PDFsharp itself and is read-only here.
        document.Info.Title = "Pawnsmith";
        document.Info.Creator = "Pawnsmith";
        document.Info.CreationDate = DateTime.Now;

        XColor strokeColor = ParseColor(layout.Strokes.ColorHex);

        foreach (SheetPage page in layout.Pages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DrawPage(document, page, layout.Strokes, strokeColor, culture);
        }

        using MemoryStream buffer = new();
        document.Save(buffer, closeStream: false);

        return Task.FromResult(buffer.ToArray());
    }

    private void DrawPage(
        PdfDocument document,
        SheetPage page,
        StrokeSettings strokes,
        XColor strokeColor,
        CultureInfo culture)
    {
        PdfPage pdfPage = document.AddPage();

        // Page size is fixed explicitly in points: no automatic scaling at
        // print time is allowed (B.6).
        pdfPage.Width = XUnit.FromPoint(Points(page.PaperFormat.WidthMm));
        pdfPage.Height = XUnit.FromPoint(Points(page.PaperFormat.HeightMm));

        using var graphics = XGraphics.FromPdfPage(pdfPage);

        foreach (PlacedUnit unit in page.Units)
        {
            DrawImage(graphics, unit.BackImage);
            DrawImage(graphics, unit.FrontImage);
            DrawCutOutline(graphics, unit, strokes, strokeColor);
            DrawFoldLines(graphics, unit, strokes, strokeColor);

            if (_annotateOrientation)
            {
                DrawOrientationAnnotations(graphics, unit, culture);
            }
        }

        DrawCalibrationMark(graphics, page, strokes, strokeColor, culture);
        DrawPageLabel(graphics, page, culture);
    }

    /// <summary>
    /// Draws one image, turning it by a half turn when the layout says so.
    /// </summary>
    /// <remarks>
    /// The rotation is applied about the centre of the image's own rectangle,
    /// which is why the rectangle is the same whether the image is turned or
    /// not. Forgetting this is what puts a character on its head after folding.
    /// </remarks>
    private void DrawImage(XGraphics graphics, PlacedImage image)
    {
        string path = Path.Combine(_imagesDirectory, image.FileName);

        // PDFsharp preserves the alpha channel of a PNG on its own.
        using var bitmap = XImage.FromFile(path);

        XRect rectangle = new(
            Points(image.XMm),
            Points(image.YMm),
            Points(image.WidthMm),
            Points(image.HeightMm));

        if (image.Rotation == ImageRotation.None)
        {
            graphics.DrawImage(bitmap, rectangle);
            return;
        }

        XPoint centre = new(
            rectangle.X + (rectangle.Width / 2),
            rectangle.Y + (rectangle.Height / 2));

        XGraphicsState saved = graphics.Save();
        graphics.TranslateTransform(centre.X, centre.Y);
        graphics.RotateTransform(180);
        graphics.TranslateTransform(-centre.X, -centre.Y);
        graphics.DrawImage(bitmap, rectangle);
        graphics.Restore(saved);
    }

    private static void DrawCutOutline(
        XGraphics graphics,
        PlacedUnit unit,
        StrokeSettings strokes,
        XColor color)
    {
        XPen pen = new(color, Points(strokes.CutWidthMm));

        // The polygon is not explicitly closed (DEC-038), so the path is closed
        // here rather than by repeating the first vertex.
        XPoint[] points = [.. unit.CutOutlineMm.Select(p => new XPoint(Points(p.XMm), Points(p.YMm)))];

        graphics.DrawPolygon(pen, points);
    }

    private static void DrawFoldLines(
        XGraphics graphics,
        PlacedUnit unit,
        StrokeSettings strokes,
        XColor color)
    {
        XPen pen = new(color, Points(strokes.FoldWidthMm))
        {
            DashStyle = XDashStyle.Custom,
            // PDFsharp expresses a dash pattern as multiples of the pen width,
            // whereas the calibration gives it in millimetres.
            DashPattern = [.. strokes.FoldDashPatternMm.Select(
                dash => Points(dash) / Points(strokes.FoldWidthMm))],
        };

        foreach (FoldLine fold in unit.FoldLines)
        {
            graphics.DrawLine(
                pen,
                Points(fold.StartMm.XMm),
                Points(fold.StartMm.YMm),
                Points(fold.EndMm.XMm),
                Points(fold.EndMm.YMm));
        }
    }

    /// <summary>
    /// Draws the 100 mm reference mark, its end ticks and its caption (B.5.4).
    /// </summary>
    private static void DrawCalibrationMark(
        XGraphics graphics,
        SheetPage page,
        StrokeSettings strokes,
        XColor color,
        CultureInfo culture)
    {
        CalibrationMark mark = page.CalibrationMark;
        XPen pen = new(color, Points(strokes.CutWidthMm));

        graphics.DrawLine(
            pen,
            Points(mark.StartMm.XMm),
            Points(mark.StartMm.YMm),
            Points(mark.EndMm.XMm),
            Points(mark.EndMm.YMm));

        foreach (PointMm end in new[] { mark.StartMm, mark.EndMm })
        {
            graphics.DrawLine(
                pen,
                Points(end.XMm),
                Points(end.YMm - (mark.TickHeightMm / 2)),
                Points(end.XMm),
                Points(end.YMm + (mark.TickHeightMm / 2)));
        }

        string caption = string.Format(
            culture,
            Strings.GetString("CalibrationCaption", culture) ?? "{0} mm",
            mark.NominalLengthMm);

        graphics.DrawString(
            caption,
            SheetFont,
            new XSolidBrush(color),
            new XPoint(
                Points((mark.StartMm.XMm + mark.EndMm.XMm) / 2),
                Points(mark.EndMm.YMm + mark.TickHeightMm + 3)),
            XStringFormats.Center);
    }

    /// <summary>
    /// Draws the page label: the size and the page number, both localised.
    /// </summary>
    /// <remarks>
    /// The size name is a translation key, never the raw enumeration value
    /// (B.6): a French sheet says "Moyenne", not "Medium".
    /// </remarks>
    private static void DrawPageLabel(XGraphics graphics, SheetPage page, CultureInfo culture)
    {
        string sizeName = Strings.GetString($"Size_{page.Size}", culture) ?? page.Size.ToString();

        string label = string.Format(
            culture,
            Strings.GetString("PageLabel", culture) ?? "{0} {1}/{2}",
            sizeName,
            page.PageNumber,
            page.PageCount);

        graphics.DrawString(
            label,
            SheetFont,
            XBrushes.Gray,
            new XPoint(Points(page.PaperFormat.WidthMm / 2), Points(6)),
            XStringFormats.Center);
    }

    /// <summary>
    /// Prints "head" and "feet" inside each panel. Diagnostics only.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The single most likely defect of this slice is a back panel that ends up
    /// upside down after folding, and it is invisible on screen: both panels
    /// look like a silhouette in a box. These labels make the orientation
    /// readable without folding anything — on a correct sheet, the two "feet"
    /// labels face each other across the fold line, and the two "head" labels
    /// sit at the outer ends of the unit.
    /// </para>
    /// <para>
    /// They are drawn from the placement the domain produced, not from a second
    /// calculation, so they report what will actually be printed rather than
    /// what ought to be.
    /// </para>
    /// <para>
    /// <b>This never appears in the application.</b> It is off by default and
    /// only the throwaway CLI of B.7 turns it on, with --debug.
    /// </para>
    /// </remarks>
    private static void DrawOrientationAnnotations(
        XGraphics graphics,
        PlacedUnit unit,
        CultureInfo culture)
    {
        string head = Strings.GetString("DebugHead", culture) ?? "head";
        string feet = Strings.GetString("DebugFeet", culture) ?? "feet";
        XSolidBrush brush = new(XColor.FromArgb(200, 40, 40));

        // The front panel is upright: head at the top of its box, feet at the
        // bottom, which is the boundary with the appendix.
        Annotate(graphics, unit.FrontImage, head, atTop: true, brush);
        Annotate(graphics, unit.FrontImage, feet, atTop: false, brush);

        // The back panel is turned by a half turn, so its head is at the bottom
        // of its box on the page and its feet at the top.
        Annotate(graphics, unit.BackImage, feet, atTop: true, brush);
        Annotate(graphics, unit.BackImage, head, atTop: false, brush);
    }

    private static void Annotate(
        XGraphics graphics,
        PlacedImage image,
        string text,
        bool atTop,
        XBrush brush)
    {
        double yMm = atTop
            ? image.YMm + (image.HeightMm * 0.10)
            : image.YMm + (image.HeightMm * 0.94);

        graphics.DrawString(
            text,
            SheetFont,
            brush,
            new XPoint(Points(image.XMm + (image.WidthMm / 2)), Points(yMm)),
            XStringFormats.Center);
    }

    private static XColor ParseColor(string hex)
    {
        string digits = hex.TrimStart('#');

        if (digits.Length != 6 || !int.TryParse(
                digits,
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out int value))
        {
            throw new ManifestException(
                $"Stroke colour '{hex}' is not a six-digit hexadecimal RGB value such as #B0B0B0.");
        }

        return XColor.FromArgb((value >> 16) & 0xFF, (value >> 8) & 0xFF, value & 0xFF);
    }
}
