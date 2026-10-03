using System.Globalization;

using Pawnsmith.Application.PhysicalValues;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Sheets;
using Pawnsmith.Application.Tests.Fixtures;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;
using Pawnsmith.Domain.Sheets;
using Pawnsmith.Domain.Units;

namespace Pawnsmith.Application.Tests.Sheets;

/// <summary>
/// The sheet of a project: report, PDF, and question C (DEC-082). Supports
/// tests 23 to 25 of G.13.
/// </summary>
public class ProjectSheetTests
{
    private const string Directory = "/projects/donjon";

    /// <summary>The framing clause the fixture's candidates froze.</summary>
    private const string Framing = "front view on the left, back view on the right";

    private readonly RecordingRenderer renderer = new();

    private static Project ElectedGoblins(int quantity = 6, Style? style = null, CalibrationOverrides? overrides = null, string paperFormat = "A4")
    {
        Blueprint goblins = ProjectFixture.Blueprint(
            quantity: quantity,
            candidates: [ProjectFixture.Candidate()],
            electedCandidateId: ProjectFixture.CandidateId);

        Blueprint ogre = ProjectFixture.Blueprint(id: Guid.NewGuid(), race: "ogre", size: Size.Large);

        return ProjectFixture.Project(overrides: overrides, blueprints: [goblins, ogre], style: style)
            with { PaperFormatName = paperFormat };
    }

    private ProjectSheet UseCase(Project project) =>
        new(new InMemoryProjectRepository(project), new UniformImageSizes(), _ => renderer);

    // --- G.13 n° 23 : capacité et occupation, sur la calibration effective -----------

    [Fact]
    public async Task TheReportGivesEachPageItsCapacityAndOccupation()
    {
        Calibration calibration = CalibrationFixture.Calibration();
        int capacity = CalibrationFixture.CapacityOnA4(calibration, Size.Medium, Geometry.TabAndSocket);

        SheetReport report = await UseCase(ElectedGoblins(quantity: capacity + 2))
            .ReportAsync(Directory, calibration, Framing, CancellationToken.None);

        report.Pages.ShouldBe(
        [
            new ReportedPage(1, Size.Medium, capacity, capacity),
            new ReportedPage(2, Size.Medium, capacity, 2),
        ]);
    }

    [Fact]
    public async Task TheCapacityIsThatOfTheEffectiveCalibrationNotOfTheFile()
    {
        Calibration calibration = CalibrationFixture.Calibration();
        var overrides = new CalibrationOverrides(TabWidthMm: null, TabHeightMm: 40.0);
        Project project = ElectedGoblins(quantity: 1, overrides: overrides);

        int fileCapacity = CalibrationFixture.CapacityOnA4(calibration, Size.Medium, Geometry.TabAndSocket);
        int effectiveCapacity = CalibrationFixture.CapacityOnA4(
            EffectiveCalibration.Resolve(calibration, project), Size.Medium, Geometry.TabAndSocket);

        // Without a difference, this test would prove nothing.
        effectiveCapacity.ShouldNotBe(fileCapacity);

        SheetReport report = await UseCase(project).ReportAsync(Directory, calibration, Framing, CancellationToken.None);

        report.Pages.Single().Capacity.ShouldBe(effectiveCapacity);
    }

    [Fact]
    public async Task ABlueprintWithoutElectionIsReportedAsSkipped()
    {
        SheetReport report = await UseCase(ElectedGoblins())
            .ReportAsync(Directory, CalibrationFixture.Calibration(), Framing, CancellationToken.None);

        report.Skipped.Single().Blueprint.Race.ShouldBe("ogre");
        report.Skipped.Single().Reason.ShouldBe(SkipReason.NoElectedCandidate);
    }

    [Fact]
    public async Task AProjectWithNothingToPrintReportsNoPage()
    {
        Project nothing = ProjectFixture.Project(blueprints: [ProjectFixture.Blueprint()]);

        SheetReport report = await UseCase(nothing)
            .ReportAsync(Directory, CalibrationFixture.Calibration(), Framing, CancellationToken.None);

        report.Pages.ShouldBeEmpty();
        report.Skipped.Count.ShouldBe(1);
    }

    // --- G.13 n° 24 : l'élu désaligné est exporté, et listé (DEC-082) ----------------------

    [Fact]
    public async Task AMisalignedElectedCandidateIsListedWithTheClausesThatMoved()
    {
        Project restyled = ElectedGoblins(style: ProjectFixture.Style("oil painting, heavy impasto"));

        SheetReport report = await UseCase(restyled)
            .ReportAsync(Directory, CalibrationFixture.Calibration(), "a new framing clause", CancellationToken.None);

        report.MisalignmentKnown.ShouldBeTrue();
        MisalignedElection misaligned = report.MisalignedElections.Single();
        misaligned.BlueprintId.ShouldBe(ProjectFixture.BlueprintId);
        misaligned.CandidateId.ShouldBe(ProjectFixture.CandidateId);
        misaligned.Clauses.ShouldBe([ClauseKind.Framing, ClauseKind.Style]);
    }

    [Fact]
    public async Task AMisalignedElectedCandidateIsStillPrinted()
    {
        Project restyled = ElectedGoblins(quantity: 2, style: ProjectFixture.Style("oil painting"));

        ProjectSheetPdf sheet = await UseCase(restyled).RenderAsync(
            Directory, CalibrationFixture.Calibration(), Framing, CultureInfo.GetCultureInfo("fr"), CancellationToken.None);

        sheet.Report.MisalignedElections.Count.ShouldBe(1);
        renderer.Layout!.Pages.Single().Units.Count.ShouldBe(2);
    }

    [Fact]
    public async Task AnAlignedProjectListsNothing()
    {
        SheetReport report = await UseCase(ElectedGoblins())
            .ReportAsync(Directory, CalibrationFixture.Calibration(), Framing, CancellationToken.None);

        report.MisalignedElections.ShouldBeEmpty();
        report.MisalignmentKnown.ShouldBeTrue();
    }

    [Fact]
    public async Task WithoutAFramingClauseTheMisalignmentIsUnknownNotAbsent()
    {
        Project restyled = ElectedGoblins(style: ProjectFixture.Style("oil painting"));

        SheetReport report = await UseCase(restyled)
            .ReportAsync(Directory, CalibrationFixture.Calibration(), framingClause: null, CancellationToken.None);

        report.MisalignmentKnown.ShouldBeFalse();
        report.MisalignedElections.ShouldBeEmpty();
    }

    // --- G.13 n° 25 : le PDF, dans la culture demandée -----------------------------------------

    [Fact]
    public async Task ThePdfIsRenderedInTheRequestedCulture()
    {
        ProjectSheetPdf sheet = await UseCase(ElectedGoblins()).RenderAsync(
            Directory, CalibrationFixture.Calibration(), Framing, CultureInfo.GetCultureInfo("fr"), CancellationToken.None);

        sheet.Pdf.ShouldBe(RecordingRenderer.Bytes);
        renderer.Culture!.Name.ShouldBe("fr");
        sheet.Report.Pages.Single().Used.ShouldBe(6);
    }

    [Fact]
    public async Task NothingToPrintIsRefusedRatherThanAnEmptyPage()
    {
        Project nothing = ProjectFixture.Project(blueprints: [ProjectFixture.Blueprint()]);

        SheetRuleException error = await Should.ThrowAsync<SheetRuleException>(() => UseCase(nothing).RenderAsync(
            Directory, CalibrationFixture.Calibration(), Framing, CultureInfo.InvariantCulture, CancellationToken.None));

        error.WireCode.ShouldBe("SHEET_EMPTY");
        renderer.Layout.ShouldBeNull();
    }

    // --- Refus codés ------------------------------------------------------------------------------

    [Fact]
    public async Task AnUnknownPaperFormatIsRefusedWithItsCode()
    {
        SheetRuleException error = await Should.ThrowAsync<SheetRuleException>(() => UseCase(ElectedGoblins(paperFormat: "Tabloid"))
            .ReportAsync(Directory, CalibrationFixture.Calibration(), Framing, CancellationToken.None));

        error.WireCode.ShouldBe("PAPER_FORMAT_UNKNOWN");
    }

    [Fact]
    public async Task AZeroCapacityIsRefusedWithItsCode()
    {
        Calibration tooTall = CalibrationFixture.Calibration() with
        {
            Sizes = new Dictionary<Size, PawnDimensions>(CalibrationFixture.Calibration().Sizes)
            {
                [Size.Medium] = new(25.4, 25.4, 200.0),
            },
        };

        SheetRuleException error = await Should.ThrowAsync<SheetRuleException>(() => UseCase(ElectedGoblins())
            .ReportAsync(Directory, tooTall, Framing, CancellationToken.None));

        error.WireCode.ShouldBe("SHEET_CAPACITY_EXCEEDED");
    }

    /// <summary>Every image measures 600 × 830 pixels: portrait, like one view of a T0a sheet.</summary>
    private sealed class UniformImageSizes : IImageSizeReader
    {
        public Task<IReadOnlyDictionary<string, SourceImageSize>> MeasureAsync(
            string imagesDirectory,
            IReadOnlyList<SheetItem> items,
            CancellationToken cancellationToken)
        {
            Dictionary<string, SourceImageSize> sizes = [];

            foreach (SheetItem item in items)
            {
                sizes[item.FrontImageFile] = new SourceImageSize(600, 830);
                sizes[item.BackImageFile] = new SourceImageSize(600, 830);
            }

            return Task.FromResult<IReadOnlyDictionary<string, SourceImageSize>>(sizes);
        }
    }

    /// <summary>Records what it was asked to render, and returns fixed bytes.</summary>
    private sealed class RecordingRenderer : ISheetRenderer
    {
        public static readonly byte[] Bytes = "%PDF-fake"u8.ToArray();

        public SheetLayout? Layout { get; private set; }

        public CultureInfo? Culture { get; private set; }

        public Task<byte[]> RenderAsync(SheetLayout layout, CultureInfo culture, CancellationToken cancellationToken)
        {
            Layout = layout;
            Culture = culture;
            return Task.FromResult(Bytes);
        }
    }
}
