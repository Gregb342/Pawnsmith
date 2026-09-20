using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Sheets;
using Pawnsmith.Domain.Tests.Fixtures;

namespace Pawnsmith.Domain.Tests.Sheets;

/// <summary>
/// Covers tests 29 and 30 of D.11: DEC-069, the one bridge between the
/// project of T2 and the engine of T1.
/// </summary>
public class ProjectSheetRequestTests
{
    private static readonly Guid OgreId = new("6e3f8a25-14bd-4c07-9f81-a5d206e3b9c1");

    private static Blueprint Elected(Size size = Size.Medium, int quantity = 6) =>
        ProjectFixture.Blueprint(size: size, quantity: quantity);

    private static Blueprint Unelected(Size size = Size.Large) =>
        ProjectFixture.Blueprint(size: size) with { Id = OgreId, Race = "ogre", CharacterClass = "bruiser", Candidates = [], ElectedCandidateId = null };

    private static Project Project(params Blueprint[] blueprints) =>
        ProjectFixture.Project() with { Blueprints = blueprints };

    // --- Le cas ordinaire ---------------------------------------------------

    [Fact]
    public void AnElectedBlueprintBecomesOneItemWithItsCutOutsAndQuantity()
    {
        ProjectSheetRequest built = ProjectSheetRequestBuilder.From(Project(Elected()), CalibrationFixture.A4);

        SheetItem item = built.Request.Items.ShouldHaveSingleItem();
        item.Name.ShouldBe("goblin skirmisher");
        item.Size.ShouldBe(Size.Medium);
        item.Quantity.ShouldBe(6);
        item.FrontImageFile.ShouldBe($"images/{ProjectFixture.CandidateId}-front.png");
        item.BackImageFile.ShouldBe($"images/{ProjectFixture.CandidateId}-back.png");
        built.Request.Geometry.ShouldBe(Geometry.TabAndSocket);
        built.Request.PaperFormat.ShouldBe(CalibrationFixture.A4);
        built.Skipped.ShouldBeEmpty();
    }

    // --- D.11 n° 29 : sans élu, aucune cellule, et un diagnostic le nomme ---

    [Fact]
    public void ABlueprintWithoutAnElectedCandidateContributesNothingAndIsNamed()
    {
        ProjectSheetRequest built = ProjectSheetRequestBuilder.From(Project(Elected(), Unelected()), CalibrationFixture.A4);

        built.Request.Items.ShouldHaveSingleItem().Name.ShouldBe("goblin skirmisher");

        SkippedBlueprint skipped = built.Skipped.ShouldHaveSingleItem();
        skipped.Blueprint.Id.ShouldBe(OgreId);
        skipped.Reason.ShouldBe(SkipReason.NoElectedCandidate);
        skipped.Message.ShouldContain("ogre bruiser");
    }

    [Fact]
    public void TheExportIsNeverBlockedForAMissingElection()
    {
        // Every blueprint unelected: an empty request, not an exception. What
        // the caller does with an empty request is its business.
        ProjectSheetRequest built = ProjectSheetRequestBuilder.From(Project(Unelected()), CalibrationFixture.A4);

        built.Request.Items.ShouldBeEmpty();
        built.Skipped.Count.ShouldBe(1);
    }

    // --- D.11 n° 30 : groupe de taille sans élu, aucune page ----------------

    [Fact]
    public void ASizeGroupWithNoElectedBlueprintProducesNoPage()
    {
        // The Large group exists in the project and nowhere in the pages. The
        // engine plans pages only for the sizes it receives; nothing had to
        // know about pages for that to hold.
        ProjectSheetRequest built = ProjectSheetRequestBuilder.From(
            Project(Elected(Size.Medium), Unelected(Size.Large)), CalibrationFixture.A4);

        IReadOnlyList<PagePlan> pages = Pagination.Plan(built.Request, CalibrationFixture.Calibration);

        pages.ShouldAllBe(page => page.Size == Size.Medium);
        pages.ShouldNotContain(page => page.Cells.Count == 0);
    }

    // --- Ordre et raisons ----------------------------------------------------

    [Fact]
    public void ItemsKeepTheBlueprintOrder()
    {
        // The order of the blueprints is the order of the pages (C.3.4).
        Blueprint first = Elected(Size.Large) with { Id = Guid.NewGuid(), Race = "ogre" };
        Blueprint second = Elected(Size.Medium);

        ProjectSheetRequest built = ProjectSheetRequestBuilder.From(Project(first, second), CalibrationFixture.A4);

        built.Request.Items.Select(item => item.Size).ShouldBe([Size.Large, Size.Medium]);
    }

    [Fact]
    public void AnElectedCandidateWithoutCutOutsIsSkippedWithItsOwnReason()
    {
        Candidate notCutOut = ProjectFixture.Candidate() with { FrontImageFile = null, BackImageFile = null };
        Blueprint blueprint = ProjectFixture.Blueprint(candidate: notCutOut);

        ProjectSheetRequest built = ProjectSheetRequestBuilder.From(Project(blueprint), CalibrationFixture.A4);

        built.Request.Items.ShouldBeEmpty();
        built.Skipped.ShouldHaveSingleItem().Reason.ShouldBe(SkipReason.ElectedCandidateNotCutOut);
    }

    [Fact]
    public void ADanglingElectionIsSkippedNotCrashed()
    {
        Blueprint blueprint = ProjectFixture.Blueprint() with { ElectedCandidateId = Guid.NewGuid() };

        ProjectSheetRequest built = ProjectSheetRequestBuilder.From(Project(blueprint), CalibrationFixture.A4);

        built.Skipped.ShouldHaveSingleItem().Reason.ShouldBe(SkipReason.ElectedCandidateNotFound);
    }
}
