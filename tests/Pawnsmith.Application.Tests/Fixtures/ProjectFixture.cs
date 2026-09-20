using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Tests.Fixtures;

/// <summary>
/// A small, coherent project for the Application tests.
/// </summary>
/// <remarks>
/// Grown from the T2 fixture, which carried nothing but overrides: the T3
/// use cases read blueprints and candidates, so the fixture now builds them,
/// with the least interesting value that is still valid everywhere else.
/// </remarks>
internal static class ProjectFixture
{
    public const string Subject = "a goblin skirmisher, wielding a short spear, one ear torn";
    public const string StyleClause = "ink outlines with a muted watercolour wash";

    public static readonly Guid BlueprintId = new("2d6b1f04-9c33-4a71-8e52-0b7d61a9c418");
    public static readonly Guid CandidateId = new("b4c7e910-2f88-4d16-9a03-5e1c8b72d055");

    public static readonly DateTimeOffset Instant =
        new(2026, 9, 1, 14, 22, 7, TimeSpan.Zero);

    public static Style Style(string styleClause = StyleClause)
    {
        return new Style(
            Name: "Ink and wash",
            StyleClause: styleClause,
            NegativeClause: "photorealistic, text, watermark",
            Palette: "muted earth tones");
    }

    /// <summary>A candidate with both cut-outs, unless told otherwise.</summary>
    public static Candidate Candidate(
        Guid? id = null,
        string subjectClauseUsed = Subject,
        CandidateStatus status = CandidateStatus.Valid,
        bool cutOut = true)
    {
        Guid candidateId = id ?? CandidateId;

        return new Candidate(
            Id: candidateId,
            Seed: 10428836719284460113UL,
            FramingClauseUsed: "front view on the left, back view on the right",
            SubjectClauseUsed: subjectClauseUsed,
            StyleClauseUsed: StyleClause,
            Status: status,
            PairedImageFile: $"images/{candidateId}-pair.png",
            FrontImageFile: cutOut ? $"images/{candidateId}-front.png" : null,
            BackImageFile: cutOut ? $"images/{candidateId}-back.png" : null,
            GeneratedAt: Instant);
    }

    public static Blueprint Blueprint(
        Guid? id = null,
        string race = "goblin",
        string characterClass = "skirmisher",
        Size size = Size.Medium,
        string subjectClause = Subject,
        int quantity = 6,
        IReadOnlyList<Candidate>? candidates = null,
        Guid? electedCandidateId = null)
    {
        return new Blueprint(
            Id: id ?? BlueprintId,
            Race: race,
            CharacterClass: characterClass,
            Size: size,
            OptionalParameters: new Dictionary<string, string>(StringComparer.Ordinal) { ["weapon"] = "spear" },
            Details: "one ear torn",
            SubjectClause: subjectClause,
            Quantity: quantity,
            Candidates: candidates ?? [],
            ElectedCandidateId: electedCandidateId);
    }

    public static Project Project(
        CalibrationOverrides? overrides = null,
        IReadOnlyList<Blueprint>? blueprints = null,
        Style? style = null)
    {
        return new Project(
            ProjectId: new Guid("8f1a3c2e-5b47-4d90-a1e6-72c9f0d4b833"),
            Name: "Donjon de la Griffe Noire",
            Universe: Universe.Fantasy,
            Style: style ?? Style(),
            Geometry: Geometry.TabAndSocket,
            PaperFormatName: "A4",
            CalibrationOverrides: overrides ?? CalibrationOverrides.None,
            Blueprints: blueprints ?? [],
            CreatedAt: Instant,
            ModifiedAt: Instant);
    }
}
