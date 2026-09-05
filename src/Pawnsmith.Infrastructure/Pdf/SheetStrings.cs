using System.Globalization;
using System.Resources;

namespace Pawnsmith.Infrastructure.Pdf;

/// <summary>
/// The localised text printed on a sheet: the calibration caption, the page
/// label, the size names, and the two debug annotations.
/// </summary>
/// <remarks>
/// <para>
/// Chapter 10 of the bible puts the server's own strings in <c>.resx</c> files,
/// and B.6 has the export request carry the target culture, because <b>the PDF
/// contains text</b>: a French sheet says "Moyenne", not "Medium".
/// </para>
/// <para>
/// <b>A missing key throws rather than falling back.</b> The renderer used to
/// write <c>GetString(key, culture) ?? "{0} mm"</c> for every key, which meant a
/// resource that failed to resolve produced a sheet all the same — an English
/// caption on a sheet asked for in French. Nothing failed, no test noticed, and
/// the defect would have been found on paper. That is the failure mode this
/// whole project is built to avoid, so the fallbacks are gone.
/// </para>
/// <para>
/// The resource name is spelled out rather than derived from
/// <c>typeof(...).Namespace</c>. Deriving it would survive a folder move on its
/// own, which sounds like an improvement and is a hidden convention: the
/// <c>.resx</c> would have to sit beside this type forever, with nothing saying
/// so. The literal is checked by the tests below instead.
/// </para>
/// </remarks>
public static class SheetStrings
{
    private static readonly ResourceManager Manager = new(
        "Pawnsmith.Infrastructure.Pdf.SheetStrings",
        typeof(SheetStrings).Assembly);

    /// <summary>The keys the renderer asks for that do not depend on a size.</summary>
    /// <remarks>
    /// Exposed so a test can assert that every one of them resolves in every
    /// supported culture. Without it, the only way to notice a broken resource
    /// would be to read a printed sheet.
    /// </remarks>
    public static IReadOnlyList<string> FixedKeys { get; } =
    [
        "CalibrationCaption",
        "PageLabel",
        "DebugHead",
        "DebugFeet",
    ];

    /// <summary>The key holding the catalogue name of one pawn size.</summary>
    /// <remarks>
    /// Size names are translation keys, never strings shown as they come
    /// (chapter 10): the identifier is English and unaccented (DEC-037), and
    /// <c>Huge</c> reads "Très Grande" in French.
    /// </remarks>
    public static string SizeKey(Domain.Primitives.Size size) => $"Size_{size}";

    /// <summary>The text for one key, in one culture.</summary>
    /// <exception cref="ManifestException">The key does not resolve.</exception>
    public static string Get(string key, CultureInfo culture)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(culture);

        return Manager.GetString(key, culture)
            ?? throw new ManifestException(
                $"The sheet string '{key}' has no value for culture '{culture.Name}'. " +
                "Either the key is missing from SheetStrings.resx, or the resource file " +
                "no longer matches the name this type asks for.");
    }
}
