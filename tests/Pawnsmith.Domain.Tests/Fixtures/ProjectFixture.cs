using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Domain.Tests.Fixtures;

/// <summary>
/// A small, coherent project used by the clause tests.
/// </summary>
/// <remarks>
/// Deliberately minimal: one blueprint, one candidate, clauses short enough to
/// read inside an assertion. The clause tests are about string rules, not about
/// realistic prompts, so a realistic fixture would only make failures harder to
/// read.
/// </remarks>
internal static class ProjectFixture
{
    public const string Framing = "front view on the left, back view on the right";
    public const string Subject = "a goblin skirmisher with a short spear";
    public const string StyleClause = "ink outlines with a muted watercolour wash";

    public static readonly Guid BlueprintId = new("2d6b1f04-9c33-4a71-8e52-0b7d61a9c418");
    public static readonly Guid CandidateId = new("b4c7e910-2f88-4d16-9a03-5e1c8b72d055");

    private static readonly DateTimeOffset Instant =
        new(2026, 9, 1, 14, 22, 7, TimeSpan.Zero);

    public static Style Style(string styleClause = StyleClause)
    {
        return new Style(
            Name: "Ink and wash",
            StyleClause: styleClause,
            NegativeClause: "photorealistic, text, watermark",
            Palette: "muted earth tones");
    }

    /// <summary>A candidate whose three frozen clauses are the fixture's own.</summary>
    public static Candidate Candidate(
        string framingClauseUsed = Framing,
        string subjectClauseUsed = Subject,
        string styleClauseUsed = StyleClause,
        CandidateStatus status = CandidateStatus.Valid)
    {
        return new Candidate(
            Id: CandidateId,
            Seed: 10428836719284460113UL,
            FramingClauseUsed: framingClauseUsed,
            SubjectClauseUsed: subjectClauseUsed,
            StyleClauseUsed: styleClauseUsed,
            Status: status,
            PairedImageFile: null,
            FrontImageFile: $"images/{CandidateId}-front.png",
            BackImageFile: $"images/{CandidateId}-back.png",
            GeneratedAt: Instant);
    }

    public static Blueprint Blueprint(
        string subjectClause = Subject,
        int quantity = 6,
        Size size = Size.Medium,
        Candidate? candidate = null)
    {
        Candidate only = candidate ?? Candidate();

        return new Blueprint(
            Id: BlueprintId,
            Race: "goblin",
            CharacterClass: "skirmisher",
            Size: size,
            OptionalParameters: new Dictionary<string, string> { ["weapon"] = "short spear" },
            Details: "one ear torn",
            SubjectClause: subjectClause,
            Quantity: quantity,
            Candidates: [only],
            ElectedCandidateId: only.Id);
    }

    public static Project Project(
        Blueprint? blueprint = null,
        Style? style = null,
        Geometry geometry = Geometry.TabAndSocket,
        string paperFormatName = "A4")
    {
        return new Project(
            ProjectId: new Guid("8f1a3c2e-5b47-4d90-a1e6-72c9f0d4b833"),
            Name: "Donjon de la Griffe Noire",
            Universe: Universe.Fantasy,
            Style: style ?? Style(),
            Geometry: geometry,
            PaperFormatName: paperFormatName,
            CalibrationOverrides: CalibrationOverrides.None,
            Blueprints: [blueprint ?? Blueprint()],
            CreatedAt: Instant,
            ModifiedAt: Instant);
    }
}
