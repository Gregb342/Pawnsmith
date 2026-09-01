using System.Globalization;

using Pawnsmith.Domain;

namespace Pawnsmith.Infrastructure.Tests;

/// <summary>
/// Covers tests 15 and 16 of B.8: a minimal manifest produces an openable,
/// non-empty PDF, and its page count matches the pagination.
/// </summary>
public class PdfSharpSheetRendererTests
{
    /// <summary>A real 1×1 transparent PNG, base64-encoded.</summary>
    /// <remarks>
    /// The header-only fixture used by the reader tests is enough to measure an
    /// image, but not to draw one: PDFsharp decodes what it draws. This is the
    /// smallest genuine PNG that will do.
    /// </remarks>
    private const string OnePixelPngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

    private static void WriteRealPng(TempWorkspace workspace, string name)
    {
        File.WriteAllBytes(
            Path.Combine(workspace.ImagesDirectory, name),
            Convert.FromBase64String(OnePixelPngBase64));
    }

    private static SheetLayout Layout(TempWorkspace workspace, int quantity, out int expectedPages)
    {
        WriteRealPng(workspace, "goblin-front.png");
        WriteRealPng(workspace, "goblin-back.png");

        SheetItem item = new("goblin", Size.Medium, quantity, "goblin-front.png", "goblin-back.png");
        SheetRequest request = new(Geometry.TabAndSocket, A4, [item]);

        IReadOnlyDictionary<string, SourceImageSize> sizes = new Dictionary<string, SourceImageSize>
        {
            ["goblin-front.png"] = new(1, 1),
            ["goblin-back.png"] = new(1, 1),
        };

        SheetLayout layout = SheetLayoutBuilder.Build(request, Calibration, sizes);
        expectedPages = layout.Pages.Count;

        return layout;
    }

    // --- B.8 n° 15 : un manifeste minimal produit un PDF ouvrable ---------

    [Fact]
    public async Task AMinimalSheetProducesANonEmptyPdf()
    {
        using TempWorkspace workspace = new();
        SheetLayout layout = Layout(workspace, quantity: 1, out _);
        PdfSharpSheetRenderer renderer = new(workspace.ImagesDirectory);

        byte[] pdf = await renderer.RenderAsync(
            layout,
            CultureInfo.GetCultureInfo("fr-FR"),
            CancellationToken.None);

        pdf.Length.ShouldBeGreaterThan(0);

        // Every PDF starts with %PDF- and ends with %%EOF. Checking both is a
        // cheap way of asserting the file is complete rather than truncated.
        System.Text.Encoding.ASCII.GetString(pdf, 0, 5).ShouldBe("%PDF-");
        System.Text.Encoding.ASCII.GetString(pdf, pdf.Length - 6, 5).ShouldBe("%%EOF");
    }

    [Theory]
    [InlineData("fr-FR")]
    [InlineData("en-US")]
    public async Task TheSheetRendersInEitherCulture(string cultureName)
    {
        using TempWorkspace workspace = new();
        SheetLayout layout = Layout(workspace, quantity: 1, out _);
        PdfSharpSheetRenderer renderer = new(workspace.ImagesDirectory);

        byte[] pdf = await renderer.RenderAsync(
            layout,
            CultureInfo.GetCultureInfo(cultureName),
            CancellationToken.None);

        pdf.Length.ShouldBeGreaterThan(0);
    }

    // --- B.8 n° 16 : le nombre de pages correspond à la pagination --------

    [Theory]
    [InlineData(1, 1)]
    [InlineData(12, 1)]
    [InlineData(13, 2)]
    [InlineData(25, 3)]
    public async Task ThePdfHasExactlyTheNumberOfPagesThatWerePlanned(int quantity, int expected)
    {
        using TempWorkspace workspace = new();
        SheetLayout layout = Layout(workspace, quantity, out int planned);
        planned.ShouldBe(expected);

        PdfSharpSheetRenderer renderer = new(workspace.ImagesDirectory);
        byte[] pdf = await renderer.RenderAsync(
            layout,
            CultureInfo.GetCultureInfo("fr-FR"),
            CancellationToken.None);

        CountPages(pdf).ShouldBe(expected);
    }

    [Fact]
    public async Task AMissingImageFileFailsTheRender()
    {
        using TempWorkspace workspace = new();
        SheetLayout layout = Layout(workspace, quantity: 1, out _);
        File.Delete(Path.Combine(workspace.ImagesDirectory, "goblin-back.png"));

        PdfSharpSheetRenderer renderer = new(workspace.ImagesDirectory);

        await Should.ThrowAsync<Exception>(
            () => renderer.RenderAsync(layout, CultureInfo.InvariantCulture, CancellationToken.None));
    }

    /// <summary>
    /// Counts the pages of a PDF by reopening it.
    /// </summary>
    /// <remarks>
    /// Reading the file back rather than trusting the layout is the point of
    /// the test: it asserts what came out, not what went in.
    /// </remarks>
    private static int CountPages(byte[] pdf)
    {
        using MemoryStream stream = new(pdf);
        using PdfSharp.Pdf.PdfDocument document =
            PdfSharp.Pdf.IO.PdfReader.Open(stream, PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import);

        return document.PageCount;
    }

    private static readonly PaperFormat A4 = new("A4", 210.0, 297.0);

    private static readonly Calibration Calibration = new(
        VersionSchema: 1,
        Paper: new PaperNote(250, "Fixture."),
        Sizes: new Dictionary<Size, PawnDimensions>
        {
            [Size.Medium] = new(25.4, 25.4, 50.0),
        },
        Geometry: new GeometrySettings(
            new FoldedTentSettings(8.0),
            new TabAndSocketSettings(12.0, 10.0)),
        Layout: new LayoutSettings(10.0, 3.0, 1.5, 14.0),
        Print: new PrintSettings(1.0),
        Strokes: new StrokeSettings(0.25, 0.25, "#B0B0B0", [2.0, 2.0]),
        PaperFormats: new Dictionary<string, PaperFormat> { ["A4"] = A4 });
}
