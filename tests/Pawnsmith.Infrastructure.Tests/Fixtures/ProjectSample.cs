using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Infrastructure.Tests.Fixtures;

/// <summary>
/// A project that exercises every branch of the schema at once.
/// </summary>
/// <remarks>
/// It is deliberately awkward rather than tidy: one override set and one left
/// null, one blueprint with two candidates and one with none, a rejected
/// candidate alongside a valid one, a candidate with no cut-outs yet, an
/// accented name, and a seed above 2^53. Every one of those is a case the
/// mapping or the writer can get wrong on its own, and a tidy fixture would
/// exercise none of them.
/// </remarks>
internal static class ProjectSample
{
    private static readonly DateTimeOffset Created = new(2026, 8, 31, 9, 12, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Modified = new(2026, 9, 1, 14, 22, 11, TimeSpan.Zero);
    private static readonly DateTimeOffset Generated = new(2026, 9, 1, 14, 22, 7, TimeSpan.Zero);

    public static Candidate Candidate(
        string id = "b4c7e910-2f88-4d16-9a03-5e1c8b72d055",
        ulong seed = 10428836719284460113UL,
        CandidateStatus status = CandidateStatus.Valid,
        bool cutOut = true)
    {
        return new Candidate(
            Id: Guid.ParseExact(id, "D"),
            Seed: seed,
            FramingClauseUsed: "front view on the left, back view on the right, feet on the bottom edge",
            SubjectClauseUsed: "a goblin skirmisher, wielding a short spear held vertically",
            StyleClauseUsed: "ink outlines with a muted watercolour wash",
            Status: status,
            PairedImageFile: $"images/{id}-pair.png",
            FrontImageFile: cutOut ? $"images/{id}-front.png" : null,
            BackImageFile: cutOut ? $"images/{id}-back.png" : null,
            GeneratedAt: Generated);
    }

    public static Blueprint Blueprint()
    {
        Candidate elected = Candidate();

        return new Blueprint(
            Id: Guid.ParseExact("2d6b1f04-9c33-4a71-8e52-0b7d61a9c418", "D"),
            Race: "goblin",
            CharacterClass: "skirmisher",
            Size: Size.Medium,
            OptionalParameters: new Dictionary<string, string>
            {
                ["weapon"] = "short spear",
                ["armour"] = "leather scraps",
            },
            Details: "one ear torn, bone fetish tied to the belt",
            SubjectClause: "a goblin skirmisher, wielding a short spear held vertically",
            Quantity: 6,
            Candidates:
            [
                elected,
                // A rejected candidate, with a seed past 2^53 and no cut-outs
                // yet: the state a candidate is in between T4 and T5.
                Candidate(
                    id: "0a19d5c3-7b64-4e28-b0f7-3c2a91e8d740",
                    seed: 18446744073709551615UL,
                    status: CandidateStatus.Rejected,
                    cutOut: false),
            ],
            ElectedCandidateId: elected.Id);
    }

    /// <summary>A blueprint with no candidate at all, which is a normal state.</summary>
    public static Blueprint EmptyBlueprint()
    {
        return new Blueprint(
            Id: Guid.ParseExact("6e3f8a25-14bd-4c07-9f81-a5d206e3b9c1", "D"),
            Race: "ogre",
            CharacterClass: "bruiser",
            Size: Size.Large,
            OptionalParameters: new Dictionary<string, string>(),
            Details: string.Empty,
            SubjectClause: "an ogre bruiser, bare-chested, holding a crude club",
            Quantity: 1,
            Candidates: [],
            ElectedCandidateId: null);
    }

    public static Project Rich()
    {
        return new Project(
            ProjectId: Guid.ParseExact("8f1a3c2e-5b47-4d90-a1e6-72c9f0d4b833", "D"),
            Name: "Donjon de la Griffe Noire",
            Universe: Universe.Fantasy,
            Style: new Style(
                Name: "Encre et lavis",
                StyleClause: "ink outlines with a muted watercolour wash",
                NegativeClause: "photorealistic, 3d render, text, watermark",
                Palette: "muted earth tones"),
            Geometry: Geometry.TabAndSocket,
            PaperFormatName: "A4",
            // One member set, one left null: the everyday case, and the one that
            // catches a merge that treats the two the same way.
            CalibrationOverrides: new CalibrationOverrides(TabWidthMm: 11.5, TabHeightMm: null),
            Blueprints: [Blueprint(), EmptyBlueprint()],
            CreatedAt: Created,
            ModifiedAt: Modified);
    }
}
