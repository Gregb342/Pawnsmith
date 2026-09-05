using System.Globalization;

using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Domain.Tests.Projects;

/// <summary>
/// Covers test 14 of C.12: a name carrying <c>../</c>, non-ASCII characters, a
/// Windows device name or an excessive length yields a folder name that is
/// safe, non-empty and bounded (MEN-009).
/// </summary>
/// <remarks>
/// These read like string-handling trivia and are not. This function stands
/// between free text — typed by the user, or arriving inside a third-party
/// archive — and a path written to disk. Every case below is an input somebody
/// can actually supply.
/// </remarks>
public class ProjectFolderNameTests
{
    // --- Le cas ordinaire ---------------------------------------------------

    [Theory]
    [InlineData("Donjon de la Griffe Noire", "donjon-de-la-griffe-noire")]
    [InlineData("Goblins 2", "goblins-2")]
    [InlineData("déjà-vu", "deja-vu")]
    [InlineData("Forêt d'Émeraude", "foret-d-emeraude")]
    public void AnOrdinaryNameBecomesAReadableSlug(string name, string expected)
    {
        ProjectFolderName.From(name).ShouldBe(expected);
    }

    // --- C.12 n° 14 : la traversée de chemin ------------------------------

    [Theory]
    [InlineData("../../logs")]
    [InlineData("..")]
    [InlineData("../../../etc/passwd")]
    [InlineData("..\\..\\Windows")]
    [InlineData("/etc/shadow")]
    [InlineData("C:\\Users\\Serge")]
    [InlineData("\\\\server\\share")]
    public void NoSeparatorAndNoDotSurvives(string hostile)
    {
        string folder = ProjectFolderName.From(hostile);

        folder.ShouldNotContain(".");
        folder.ShouldNotContain("/");
        folder.ShouldNotContain("\\");
        folder.ShouldNotContain(":");
        folder.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ATraversalAttemptCollapsesToSomethingHarmless()
    {
        // Worth pinning the exact value rather than only the properties: it is
        // the difference between "the dots were removed" and "the dots became
        // dashes and the path still has shape".
        ProjectFolderName.From("../../logs").ShouldBe("logs");
        ProjectFolderName.From("..").ShouldBe(ProjectFolderName.Fallback);
    }

    // --- C.12 n° 14 : les noms réservés de Windows ------------------------

    [Theory]
    [InlineData("CON")]
    [InlineData("con")]
    [InlineData("Nul")]
    [InlineData("PRN")]
    [InlineData("AUX")]
    [InlineData("COM1")]
    [InlineData("LPT9")]
    public void ADeviceNameIsNeverUsedAsAFolderName(string reserved)
    {
        // On Windows these are devices, not files: a folder called NUL cannot be
        // created and writing to it goes nowhere. Nothing is lost by falling
        // back, because the folder name has no meaning (DEC-047) and the name
        // the user typed stays in Project.Name.
        ProjectFolderName.From(reserved).ShouldBe(ProjectFolderName.Fallback);
    }

    [Fact]
    public void ANameThatMerelyContainsADeviceNameIsFine()
    {
        // Only the whole name is reserved. "console" is not a device, and
        // rejecting it would be a false positive on a perfectly good title.
        ProjectFolderName.From("Console de test").ShouldBe("console-de-test");
    }

    // --- C.12 n° 14 : la longueur ------------------------------------------

    [Fact]
    public void AnExcessiveLengthIsCutToTheBound()
    {
        string folder = ProjectFolderName.From(new string('a', 500));

        folder.Length.ShouldBe(ProjectFolderName.MaxLength);
    }

    [Fact]
    public void TruncationNeverLeavesATrailingDash()
    {
        // The cut can land in the middle of a run of dashes. Trimming again
        // afterwards is what stops "some-name-" coming out, which would then
        // read oddly next to the "-2" suffix the repository may append.
        string name = new string('a', ProjectFolderName.MaxLength - 1) + " " + new string('b', 20);

        string folder = ProjectFolderName.From(name);

        folder.ShouldNotEndWith("-");
        folder.Length.ShouldBeLessThanOrEqualTo(ProjectFolderName.MaxLength);
    }

    // --- C.12 n° 14 : rien d'utilisable ne subsiste ------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    [InlineData("日本語")]
    [InlineData("Кириллица")]
    public void AnEmptyResultFallsBackRatherThanReturningNothing(string name)
    {
        // A name written in a script with no ASCII transliteration still needs a
        // folder. The repository turns the second such project into "project-2".
        ProjectFolderName.From(name).ShouldBe(ProjectFolderName.Fallback);
    }

    // --- Les propriétés qui doivent tenir quelle que soit l'entrée ---------

    [Theory]
    [InlineData("Donjon de la Griffe Noire")]
    [InlineData("../../logs")]
    [InlineData("CON")]
    [InlineData("déjà-vu")]
    [InlineData("!!!")]
    [InlineData("  espaces  partout  ")]
    public void TheResultAlwaysHoldsTheSameInvariants(string name)
    {
        string folder = ProjectFolderName.From(name);

        folder.ShouldNotBeNullOrEmpty();
        folder.Length.ShouldBeLessThanOrEqualTo(ProjectFolderName.MaxLength);
        folder.ShouldNotStartWith("-");
        folder.ShouldNotEndWith("-");
        folder.ShouldNotContain("--");

        // Written with comparisons rather than a pattern: Shouldly takes an
        // expression tree here, and C# forbids `is` patterns inside one.
        folder.ShouldAllBe(character =>
            (character >= 'a' && character <= 'z')
            || (character >= '0' && character <= '9')
            || character == '-');
    }

    [Fact]
    public void TheSameNameGivesTheSameFolderWhateverTheProcessCulture()
    {
        // The Turkish "I" is the classic trap: "I".ToLower() is a dotless "ı"
        // there, which has no ASCII form and would become a dash. Two machines
        // would then disagree on the folder name for one project.
        CultureInfo original = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            string turkish = ProjectFolderName.From("ISLAND Invasion");

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            string french = ProjectFolderName.From("ISLAND Invasion");

            turkish.ShouldBe("island-invasion");
            turkish.ShouldBe(french);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
