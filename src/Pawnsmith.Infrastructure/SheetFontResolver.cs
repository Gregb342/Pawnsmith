using PdfSharp.Fonts;

namespace Pawnsmith.Infrastructure;

/// <summary>
/// Supplies the single font the sheet needs.
/// </summary>
/// <remarks>
/// <para>
/// PDFsharp 6 has no fonts of its own and no access to system fonts: it asks
/// the application to resolve every typeface. That is deliberate on its part —
/// it makes rendering identical on Windows, in a Linux container and in CI,
/// which matters here because the same PDF must come out of all three.
/// </para>
/// <para>
/// The sheet uses exactly one font, at one weight, for two short strings: the
/// calibration caption and the page label. So this resolver answers every
/// request with the same face rather than pretending to offer a family.
/// </para>
/// <para>
/// <b>⚠ Licence reservation, to be settled before v1.</b> The font used here is
/// Segoe WP, embedded in the PDFsharp package. PDFsharp is MIT, but the font is
/// Microsoft's and its redistribution terms are its own — and a font used in a
/// PDF gets embedded in that PDF. For a project whose §A.2 makes licensing a
/// design criterion, that is not a detail to leave implicit. Replacing it with
/// an OFL face such as Liberation Sans or DejaVu Sans means changing this class
/// and nothing else.
/// </para>
/// </remarks>
internal sealed class SheetFontResolver : IFontResolver
{
    /// <summary>The name the renderer asks for.</summary>
    public const string FontFamilyName = "PawnsmithSheet";

    private const string FaceName = "PawnsmithSheet#Regular";

    public static SheetFontResolver Instance { get; } = new();

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        // One face for every request: neither weight nor slant is used on the
        // sheet, and answering with variations we do not have would only invite
        // PDFsharp to ask for them.
        return new FontResolverInfo(FaceName);
    }

    public byte[]? GetFont(string faceName)
    {
        return PdfSharp.WPFonts.FontDataHelper.SegoeWP;
    }
}
