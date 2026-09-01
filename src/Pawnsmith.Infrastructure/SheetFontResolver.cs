using System.Reflection;

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
/// which matters here because the same PDF must come out of all three. It is
/// also why the font is embedded in the assembly rather than looked up on the
/// machine: the ASP.NET runtime image ships with no fonts at all.
/// </para>
/// <para>
/// The face is <b>DejaVu Sans</b>, under the Bitstream Vera licence — a free
/// licence which explicitly permits redistribution and embedding in documents.
/// That last point is what matters: a font used in a PDF is embedded in that
/// PDF, so every sheet Pawnsmith produces carries it. §A.2 makes licensing a
/// design criterion, and a proprietary face would have pushed that obligation
/// onto every downstream user.
/// </para>
/// <para>
/// The sheet uses exactly one font, at one weight, for two short strings: the
/// calibration caption and the page label. So this resolver answers every
/// request with the same face rather than pretending to offer a family.
/// </para>
/// </remarks>
internal sealed class SheetFontResolver : IFontResolver
{
    /// <summary>The name the renderer asks for.</summary>
    public const string FontFamilyName = "PawnsmithSheet";

    private const string FaceName = "PawnsmithSheet#Regular";

    private const string ResourceName = "Pawnsmith.Infrastructure.Fonts.DejaVuSans.ttf";

    private static readonly Lazy<byte[]> FontData = new(LoadFontData);

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
        return FontData.Value;
    }

    private static byte[] LoadFontData()
    {
        Assembly assembly = typeof(SheetFontResolver).Assembly;

        using Stream stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException(
                $"The embedded font '{ResourceName}' is missing from the assembly.");

        using MemoryStream buffer = new();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }
}
