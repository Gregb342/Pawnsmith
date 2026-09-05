using System.Text;

using Pawnsmith.Domain.Projects;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// Covers tests 15 to 21, 23 and 54 of C.12: the form rules of C.3.3, and the
/// full round trip through JSON.
/// </summary>
public class ProjectJsonTests
{
    private static string Write(Project project) =>
        Encoding.UTF8.GetString(ProjectJson.Serialize(project.ToDocument()));

    // --- C.12 n° 15 : l'aller-retour complet ------------------------------

    [Fact]
    public void AProjectSurvivesDomainToDocumentToJsonAndBack()
    {
        Project source = ProjectSample.Rich();

        byte[] bytes = ProjectJson.Serialize(source.ToDocument());
        Project restored = ProjectJson.Deserialize(bytes).ToDomain();

        restored.ProjectId.ShouldBe(source.ProjectId);
        restored.Name.ShouldBe(source.Name);
        restored.Style.ShouldBe(source.Style);
        restored.CalibrationOverrides.ShouldBe(source.CalibrationOverrides);
        restored.CreatedAt.ShouldBe(source.CreatedAt);
        restored.Blueprints.Count.ShouldBe(source.Blueprints.Count);
        restored.Blueprints[0].Candidates.ShouldBe(source.Blueprints[0].Candidates);
        // Same entries, not the same enumeration order: the writer sorts the
        // keys on purpose (test 54).
        restored.Blueprints[0].OptionalParameters.Count
            .ShouldBe(source.Blueprints[0].OptionalParameters.Count);
        restored.Blueprints[0].OptionalParameters.ShouldContainKeyAndValue("weapon", "short spear");
        restored.Blueprints[0].OptionalParameters.ShouldContainKeyAndValue("armour", "leather scraps");
    }

    // --- C.12 n° 16 : aucune valeur dérivée dans le fichier ---------------

    [Fact]
    public void TheFileContainsNoDerivedKey()
    {
        // A net rather than the mechanism: the document types cannot express
        // these members at all (C.3.6). Worth having anyway, because it is the
        // acceptance criterion of the slice and it reads as one.
        string json = Write(ProjectSample.Rich());

        json.ShouldNotContain("resolvedPrompt");
        json.ShouldNotContain("misaligned");
        json.ShouldNotContain("promptUtilise");
    }

    [Fact]
    public void TheRootKeysAreWrittenInTheOrderOfTheSchema()
    {
        // Fixed order is what makes two saves comparable, and the order is the
        // one C.3.4 tabulates. It comes from the declaration order of the
        // record, so this test is what notices a tidy-up reordering it.
        string json = Write(ProjectSample.Rich());

        int[] positions =
        [
            json.IndexOf("\"versionSchema\"", StringComparison.Ordinal),
            json.IndexOf("\"projectId\"", StringComparison.Ordinal),
            json.IndexOf("\"name\"", StringComparison.Ordinal),
            json.IndexOf("\"universe\"", StringComparison.Ordinal),
            json.IndexOf("\"geometry\"", StringComparison.Ordinal),
            json.IndexOf("\"paperFormat\"", StringComparison.Ordinal),
            json.IndexOf("\"style\"", StringComparison.Ordinal),
            json.IndexOf("\"calibrationOverrides\"", StringComparison.Ordinal),
            json.IndexOf("\"blueprints\"", StringComparison.Ordinal),
            json.IndexOf("\"createdAt\"", StringComparison.Ordinal),
            json.IndexOf("\"modifiedAt\"", StringComparison.Ordinal),
        ];

        positions.ShouldAllBe(position => position >= 0);
        positions.ShouldBe(positions.Order());
    }

    // --- C.12 n° 17 : les décimales sous une culture française ------------

    [Fact]
    public void DecimalsAreWrittenWithAPointUnderAFrenchCulture()
    {
        using CultureScope _ = new("fr-FR");

        string json = Write(ProjectSample.Rich());

        json.ShouldContain("\"tabWidthMm\": 11.5");
        json.ShouldNotContain("11,5");
    }

    [Fact]
    public void DecimalsAreReadBackCorrectlyUnderAFrenchCulture()
    {
        byte[] bytes = ProjectJson.Serialize(ProjectSample.Rich().ToDocument());

        using CultureScope _ = new("fr-FR");

        ProjectJson.Deserialize(bytes).CalibrationOverrides.TabWidthMm.ShouldBe(11.5);
    }

    // --- C.12 n° 18 : UTF-8 sans BOM, accents et emoji --------------------

    [Fact]
    public void TheBytesCarryNoByteOrderMark()
    {
        // A BOM breaks byte-for-byte comparison and surprises Unix tooling.
        byte[] bytes = ProjectJson.Serialize(ProjectSample.Rich().ToDocument());

        bytes[..3].ShouldNotBe(new byte[] { 0xEF, 0xBB, 0xBF });
        bytes[0].ShouldBe((byte)'{');
    }

    [Fact]
    public void AccentsAndEmojiAreWrittenLiterallyAndSurviveTheRoundTrip()
    {
        // The default encoder would write é here: deterministic, so no
        // test would complain, and yet it destroys the readability DEC-011
        // justifies the whole format with.
        Project source = ProjectSample.Rich() with { Name = "Donjon de la Griffe Noire — épée 🗡" };

        string json = Write(source);

        // Accents and the em dash come out literally, which is the readability
        // C.3.3 is after.
        json.ShouldContain("Griffe Noire — épée");
        json.ShouldNotContain("\\u00e9");

        // The emoji does NOT, and that is a limit of the library rather than a
        // choice: a character outside the Basic Multilingual Plane is written as
        // a surrogate pair by every available encoder, and the one covering all
        // Unicode ranges also escapes apostrophes — far worse in a French
        // project. Test 18 asks that emoji survive the round trip, and they do.
        json.ShouldContain("\\uD83D\\uDDE1");

        byte[] bytes = ProjectJson.Serialize(source.ToDocument());
        ProjectJson.Deserialize(bytes).ToDomain().Name.ShouldBe(source.Name);
    }

    [Fact]
    public void LinesAreSeparatedByLineFeedsAlone()
    {
        string json = Write(ProjectSample.Rich());

        json.ShouldContain("\n");
        json.ShouldNotContain("\r");
    }

    [Fact]
    public void TheFileEndsWithASingleNewline()
    {
        string json = Write(ProjectSample.Rich());

        json.ShouldEndWith("}\n");
    }

    // --- C.12 n° 19 : deux écritures identiques ---------------------------

    [Fact]
    public void TwoSerialisationsOfAnUnchangedProjectAreByteForByteIdentical()
    {
        Project project = ProjectSample.Rich();

        byte[] first = ProjectJson.Serialize(project.ToDocument());
        byte[] second = ProjectJson.Serialize(project.ToDocument());

        second.ShouldBe(first);
    }

    [Fact]
    public void TwoSerialisationsAgreeEvenWhenTheOptionalParametersWereBuiltInAnotherOrder()
    {
        // The enumeration order of a .NET dictionary is not guaranteed, so
        // without an explicit sort this is not satisfiable in a deterministic
        // way - and test 19 would be flaky rather than wrong, which is worse.
        Blueprint ascending = ProjectSample.Blueprint() with
        {
            OptionalParameters = new Dictionary<string, string>
            {
                ["armour"] = "leather scraps",
                ["weapon"] = "short spear",
            },
        };

        Blueprint descending = ProjectSample.Blueprint() with
        {
            OptionalParameters = new Dictionary<string, string>
            {
                ["weapon"] = "short spear",
                ["armour"] = "leather scraps",
            },
        };

        Project withAscending = ProjectSample.Rich() with { Blueprints = [ascending] };
        Project withDescending = ProjectSample.Rich() with { Blueprints = [descending] };

        ProjectJson.Serialize(withAscending.ToDocument())
            .ShouldBe(ProjectJson.Serialize(withDescending.ToDocument()));
    }

    // --- C.12 n° 54 : les clés triées, en ordinal -------------------------

    [Fact]
    public void OptionalParameterKeysAreWrittenSortedWhateverTheProcessCulture()
    {
        Blueprint blueprint = ProjectSample.Blueprint() with
        {
            OptionalParameters = new Dictionary<string, string>
            {
                ["weapon"] = "spear",
                ["Armour"] = "leather",
                ["colour"] = "green",
            },
        };

        using CultureScope _ = new("fr-FR");

        Project project = ProjectSample.Rich() with { Blueprints = [blueprint] };
        string json = Write(project);

        // Ordinal, so upper case sorts before lower case. A culture-aware sort
        // would put "Armour" and "armour" together and vary with the installed
        // ICU version, so two machines would write two different files.
        int armour = json.IndexOf("\"Armour\"", StringComparison.Ordinal);
        int colour = json.IndexOf("\"colour\"", StringComparison.Ordinal);
        int weapon = json.IndexOf("\"weapon\"", StringComparison.Ordinal);

        armour.ShouldBeGreaterThan(0);
        armour.ShouldBeLessThan(colour);
        colour.ShouldBeLessThan(weapon);
    }

    // --- C.12 n° 20 et 21 : graine et horodatages -------------------------

    [Fact]
    public void TheSeedIsWrittenAsAStringAndTheLargestOneSurvives()
    {
        string json = Write(ProjectSample.Rich());

        json.ShouldContain("\"seed\": \"18446744073709551615\"");

        Project restored = ProjectJson.Deserialize(ProjectJson.Serialize(ProjectSample.Rich().ToDocument()))
            .ToDomain();

        restored.Blueprints[0].Candidates[1].Seed.ShouldBe(ulong.MaxValue);
    }

    [Fact]
    public void TimestampsAreWrittenInUtcAndReadBackUnderAShiftedTimeZone()
    {
        string json = Write(ProjectSample.Rich());
        json.ShouldContain("\"createdAt\": \"2026-08-31T09:12:00Z\"");

        // The parse must not consult the local time zone: the same file read in
        // Tokyo and in Paris has to yield the same instant.
        Project restored = ProjectJson.Deserialize(ProjectJson.Serialize(ProjectSample.Rich().ToDocument()))
            .ToDomain();

        restored.CreatedAt.ShouldBe(new DateTimeOffset(2026, 8, 31, 9, 12, 0, TimeSpan.Zero));
        restored.CreatedAt.Offset.ShouldBe(TimeSpan.Zero);
    }

    // --- C.12 n° 23 : les ordres significatifs -----------------------------

    [Fact]
    public void TheOrderOfBlueprintsAndCandidatesIsNeverSorted()
    {
        // Deliberately the reverse of the fixture's order. Sorting here would
        // reorder the pages of the produced PDF (B.5.1).
        Project source = ProjectSample.Rich() with
        {
            Blueprints = [ProjectSample.EmptyBlueprint(), ProjectSample.Blueprint()],
        };

        Project restored = ProjectJson.Deserialize(ProjectJson.Serialize(source.ToDocument())).ToDomain();

        restored.Blueprints.Select(blueprint => blueprint.Id)
            .ShouldBe(source.Blueprints.Select(blueprint => blueprint.Id));
        restored.Blueprints[1].Candidates.Select(candidate => candidate.Id)
            .ShouldBe(source.Blueprints[1].Candidates.Select(candidate => candidate.Id));
    }

    // --- Ce que le lecteur refuse ------------------------------------------

    [Fact]
    public void AnUnknownMemberIsRefusedRatherThanIgnored()
    {
        // The opposite of what calibration.json does, on purpose: that file is
        // hand-written and gets annotated, this one is written only by the
        // application, so an unknown member means corruption or a manual edit
        // whose intent cannot be honoured - and ignoring it would make it
        // disappear at the next save (DEC-048).
        string json = Encoding.UTF8.GetString(ProjectJson.Serialize(ProjectSample.Rich().ToDocument()))
            .Replace("\"versionSchema\": 1,", "\"versionSchema\": 1,\n  \"colour\": \"blue\",", StringComparison.Ordinal);

        Should.Throw<System.Text.Json.JsonException>(
            () => ProjectJson.Deserialize(Encoding.UTF8.GetBytes(json)));
    }
}
