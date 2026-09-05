using System.Globalization;

using Pawnsmith.Domain.Primitives;
using Pawnsmith.Infrastructure;
using Pawnsmith.Infrastructure.Pdf;

namespace Pawnsmith.Infrastructure.Tests.Pdf;

/// <summary>
/// Guards the one thing the sheet-rendering tests could never see: that the
/// localised strings actually resolve.
/// </summary>
/// <remarks>
/// The renderer used to fall back to an English default for every key, so a
/// resource that failed to resolve still produced a valid, non-empty PDF — with
/// the wrong language on it. Every existing test would have stayed green, and
/// the defect would have been read off a printed sheet.
/// <para>
/// The name of the embedded resource is a literal in <see cref="SheetStrings"/>
/// and the <c>.resx</c> lives in a folder, so the two can drift apart the day
/// someone moves a file. These tests are what makes that drift fail the build
/// instead of the print.
/// </para>
/// </remarks>
public class SheetStringsTests
{
    private static readonly string[] Cultures = ["fr-FR", "en-US"];

    [Fact]
    public void EveryFixedKeyResolvesInEverySupportedCulture()
    {
        foreach (string cultureName in Cultures)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);

            foreach (string key in SheetStrings.FixedKeys)
            {
                SheetStrings.Get(key, culture).ShouldNotBeNullOrWhiteSpace();
            }
        }
    }

    [Fact]
    public void EverySizeHasACatalogueNameInEverySupportedCulture()
    {
        // Size names are translation keys, never strings shown as they come
        // (chapter 10). Adding a size to the enumeration without adding its two
        // catalogue entries must fail here, not on paper.
        foreach (string cultureName in Cultures)
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);

            foreach (Size size in Enum.GetValues<Size>())
            {
                SheetStrings.Get(SheetStrings.SizeKey(size), culture).ShouldNotBeNullOrWhiteSpace();
            }
        }
    }

    [Fact]
    public void TheFrenchCatalogueIsActuallyLoaded()
    {
        // The neutral resource is embedded in the assembly, the French one in a
        // satellite assembly beside it. If the satellite were missing or not
        // copied, the neutral value would come back for both cultures and every
        // other assertion here would still pass. Comparing the two is what
        // proves the satellite resolved.
        string french = SheetStrings.Get("CalibrationCaption", CultureInfo.GetCultureInfo("fr-FR"));
        string english = SheetStrings.Get("CalibrationCaption", CultureInfo.GetCultureInfo("en-US"));

        french.ShouldNotBe(english);
        SheetStrings.Get(SheetStrings.SizeKey(Size.Huge), CultureInfo.GetCultureInfo("fr-FR"))
            .ShouldBe("Très Grande");
    }

    [Fact]
    public void AnUnknownKeyFailsLoudlyAndNamesItself()
    {
        ManifestException error = Should.Throw<ManifestException>(
            () => SheetStrings.Get("NoSuchKey", CultureInfo.GetCultureInfo("fr-FR")));

        error.Message.ShouldContain("NoSuchKey");
    }
}
