using System.Globalization;

using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// The domain half of test 15 of C.12: domain → document → domain preserves the
/// whole model. The JSON leg is added when the writer exists.
/// </summary>
public class ProjectMappingTests
{
    /// <summary>Compares two dictionaries by content, whatever their enumeration order.</summary>
    /// <remarks>
    /// Shouldly compares a dictionary as a sequence, and the round trip sorts the
    /// optional-parameter keys on purpose (test 54). Asserting on the sequence
    /// would therefore fail for the one reason we want, which tells us nothing
    /// about the contents.
    /// </remarks>
    private static void ShouldHaveSameEntries(
        IReadOnlyDictionary<string, string> actual,
        IReadOnlyDictionary<string, string> expected)
    {
        actual.Count.ShouldBe(expected.Count);

        foreach ((string key, string value) in expected)
        {
            actual.ShouldContainKeyAndValue(key, value);
        }
    }

    // --- C.12 n° 15 : l'aller-retour ne perd rien -------------------------

    [Fact]
    public void AProjectSurvivesTheRoundTripFieldForField()
    {
        Project source = ProjectSample.Rich();

        Project restored = source.ToDocument().ToDomain();

        // Compared member by member rather than with a single equality check:
        // a record compares its collections by reference, so `restored ==
        // source` would be false here for the right reason and tell us nothing
        // about the contents.
        restored.ProjectId.ShouldBe(source.ProjectId);
        restored.Name.ShouldBe(source.Name);
        restored.Universe.ShouldBe(source.Universe);
        restored.Geometry.ShouldBe(source.Geometry);
        restored.PaperFormatName.ShouldBe(source.PaperFormatName);
        restored.Style.ShouldBe(source.Style);
        restored.CalibrationOverrides.ShouldBe(source.CalibrationOverrides);
        restored.CreatedAt.ShouldBe(source.CreatedAt);
        restored.ModifiedAt.ShouldBe(source.ModifiedAt);
        restored.Blueprints.Count.ShouldBe(source.Blueprints.Count);
    }

    [Fact]
    public void EveryBlueprintAndEveryCandidateSurvivesToo()
    {
        Project source = ProjectSample.Rich();

        Project restored = source.ToDocument().ToDomain();

        foreach ((Blueprint expected, Blueprint actual) in source.Blueprints.Zip(restored.Blueprints))
        {
            actual.Id.ShouldBe(expected.Id);
            actual.Race.ShouldBe(expected.Race);
            actual.CharacterClass.ShouldBe(expected.CharacterClass);
            actual.Size.ShouldBe(expected.Size);
            actual.Details.ShouldBe(expected.Details);
            actual.SubjectClause.ShouldBe(expected.SubjectClause);
            actual.Quantity.ShouldBe(expected.Quantity);
            actual.ElectedCandidateId.ShouldBe(expected.ElectedCandidateId);
            ShouldHaveSameEntries(actual.OptionalParameters, expected.OptionalParameters);
            actual.Candidates.ShouldBe(expected.Candidates);
        }
    }

    [Fact]
    public void TheOrderOfBlueprintsAndCandidatesIsPreserved()
    {
        // Not cosmetic: B.5.1 paginates size groups in the order the manifest
        // lists them, and the manifest comes from the project. This order is the
        // order of the pages in the PDF.
        Project source = ProjectSample.Rich();

        Project restored = source.ToDocument().ToDomain();

        restored.Blueprints.Select(blueprint => blueprint.Id)
            .ShouldBe(source.Blueprints.Select(blueprint => blueprint.Id));
        restored.Blueprints[0].Candidates.Select(candidate => candidate.Id)
            .ShouldBe(source.Blueprints[0].Candidates.Select(candidate => candidate.Id));
    }

    // --- Les conversions qui peuvent mentir --------------------------------

    [Fact]
    public void TheLargestPossibleSeedSurvives()
    {
        // ulong.MaxValue is where a JSON number would have failed: past 2^53,
        // JSON.parse rounds silently, and a rounded seed is a generation that
        // cannot be replayed.
        Candidate source = ProjectSample.Candidate() with { Seed = ulong.MaxValue };

        CandidateDocument document = source.ToDocument();

        document.Seed.ShouldBe("18446744073709551615");
        document.ToDomain().Seed.ShouldBe(ulong.MaxValue);
    }

    [Fact]
    public void TimestampsAreWrittenInUtcWhateverTheOffsetTheyCarried()
    {
        // Same instant, expressed in Paris time. The file must not record the
        // offset: a project has no time zone.
        DateTimeOffset paris = new(2026, 9, 1, 16, 22, 7, TimeSpan.FromHours(2));
        Candidate source = ProjectSample.Candidate() with { GeneratedAt = paris };

        CandidateDocument document = source.ToDocument();

        document.GeneratedAt.ShouldBe("2026-09-01T14:22:07Z");
        document.ToDomain().GeneratedAt.ShouldBe(paris);
    }

    [Fact]
    public void DecimalOverridesAreWrittenAndReadWithAPointUnderAFrenchCulture()
    {
        // The most banal .NET defect of this slice, and invisible while
        // developing in English: a fr-FR session writing "11,5" produces a
        // project that is unreadable everywhere else.
        using CultureScope _ = new("fr-FR");

        CalibrationOverrides source = new(TabWidthMm: 11.5, TabHeightMm: null);

        CalibrationOverridesDocument document = source.ToDocument();

        document.TabWidthMm.ShouldBe(11.5);
        document.TabHeightMm.ShouldBeNull();
        document.ToDomain().ShouldBe(source);
    }

    [Fact]
    public void IdentifiersAreWrittenLowerCaseAndHyphenated()
    {
        Project source = ProjectSample.Rich();

        ProjectDocument document = source.ToDocument();

        document.ProjectId.ShouldBe(source.ProjectId.ToString("D", CultureInfo.InvariantCulture));
        document.ProjectId.ShouldNotContain("{");
        document.ProjectId.ShouldBe(document.ProjectId.ToLowerInvariant());
    }

    [Fact]
    public void AnIdentifierInAnotherSpellingIsRefused()
    {
        // Guid.Parse would accept braces, parentheses and the form without
        // hyphens. Accepting three spellings of one identifier would let two
        // projects differ byte for byte while meaning the same thing.
        CandidateDocument document = ProjectSample.Candidate().ToDocument()
            with { Id = "{b4c7e910-2f88-4d16-9a03-5e1c8b72d055}" };

        Should.Throw<FormatException>(() => document.ToDomain());
    }

    [Theory]
    [InlineData("medium")]
    [InlineData("MEDIUM")]
    [InlineData("1")]
    [InlineData("Enormous")]
    public void AnUnknownOrMisspeltEnumerationMemberIsRefusedAndListsTheKnownOnes(string value)
    {
        BlueprintDocument document = ProjectSample.Blueprint().ToDocument() with { Size = value };

        ManifestException error = Should.Throw<ManifestException>(() => document.ToDomain());

        error.Message.ShouldContain(nameof(Size));
        error.Message.ShouldContain("Gargantuan");
    }

    // --- C.12 n° 16 : la garantie est portée par les types ----------------

    [Fact]
    public void TheCandidateDocumentHasNoDerivedMember()
    {
        // The real mechanism behind "no derived value is serialised" is that
        // there is nothing to leave out: the type cannot express it (C.3.6).
        // The textual assertion on the produced file is a net, and it belongs
        // with the writer.
        string[] members = [.. typeof(CandidateDocument).GetProperties().Select(p => p.Name)];

        members.ShouldNotContain("ResolvedPrompt");
        members.ShouldNotContain("Misaligned");
        members.ShouldNotContain("PromptUtilise");
        members.ShouldContain("FramingClauseUsed");
        members.ShouldContain("SubjectClauseUsed");
        members.ShouldContain("StyleClauseUsed");
    }
}
