using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Tests.Fixtures;

/// <summary>
/// The smallest project that carries overrides, for the merge tests.
/// </summary>
/// <remarks>
/// Nothing but the overrides matters here, so everything else is filled with
/// the least interesting value that is still valid. A richer fixture would
/// suggest the merge reads more of the project than it does — it reads exactly
/// one member.
/// </remarks>
internal static class ProjectFixture
{
    private static readonly DateTimeOffset Instant =
        new(2026, 9, 1, 14, 22, 7, TimeSpan.Zero);

    public static Project Project(CalibrationOverrides overrides)
    {
        return new Project(
            ProjectId: new Guid("8f1a3c2e-5b47-4d90-a1e6-72c9f0d4b833"),
            Name: "Donjon de la Griffe Noire",
            Universe: Universe.Fantasy,
            Style: new Style(
                Name: "Ink and wash",
                StyleClause: "ink outlines with a muted watercolour wash",
                NegativeClause: "photorealistic, text, watermark",
                Palette: "muted earth tones"),
            Geometry: Geometry.TabAndSocket,
            PaperFormatName: "A4",
            CalibrationOverrides: overrides,
            Blueprints: [],
            CreatedAt: Instant,
            ModifiedAt: Instant);
    }
}
