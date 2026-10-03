using Pawnsmith.Application.Sheets;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Prompts;
using Pawnsmith.Domain.Sheets;

namespace Pawnsmith.Api.Contracts;

/// <summary>What the sheet will be, before it is printed (§G.8, §15.4).</summary>
/// <param name="Pages">Per page: its size, its capacity in cells, how many are used.</param>
/// <param name="Skipped">Blueprints left off the sheet, and why — never a message (DEC-084).</param>
/// <param name="MisalignedElections">Elected candidates printed although misaligned, with the clauses that moved (DEC-082).</param>
/// <param name="MisalignmentKnown">False when no workflow is configured: unknown is not aligned.</param>
/// <param name="WidthLimited">Images that print short because their width limits them (DEC-042).</param>
public sealed record SheetReportDto(
    IReadOnlyList<PageDto> Pages,
    IReadOnlyList<SkippedDto> Skipped,
    IReadOnlyList<MisalignedElectionDto> MisalignedElections,
    bool MisalignmentKnown,
    IReadOnlyList<WidthLimitedDto> WidthLimited);

public sealed record PageDto(int Number, Size Size, int Capacity, int Used);

public sealed record SkippedDto(Guid BlueprintId, SkipReason Reason);

public sealed record MisalignedElectionDto(Guid BlueprintId, Guid CandidateId, IReadOnlyList<ClauseKind> Clauses);

/// <param name="HeightUsage">Share of the available height actually printed, from 0 to 1.</param>
public sealed record WidthLimitedDto(string Name, Size Size, double PrintedHeightMm, double AvailableHeightMm, double HeightUsage);

/// <summary>The manual mapping of the report (DEC-021).</summary>
public static class SheetMapping
{
    public static SheetReportDto ToDto(this SheetReport report) => new(
        Pages: [.. report.Pages.Select(page => new PageDto(page.Number, page.Size, page.Capacity, page.Used))],
        Skipped: [.. report.Skipped.Select(skipped => new SkippedDto(skipped.Blueprint.Id, skipped.Reason))],
        MisalignedElections: [.. report.MisalignedElections.Select(misaligned =>
            new MisalignedElectionDto(misaligned.BlueprintId, misaligned.CandidateId, misaligned.Clauses))],
        MisalignmentKnown: report.MisalignmentKnown,
        WidthLimited: [.. report.WidthLimited.Select(item =>
            new WidthLimitedDto(item.ItemName, item.Size, item.PrintedHeightMm, item.AvailableHeightMm, item.HeightUsage))]);
}
