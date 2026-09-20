using Pawnsmith.Application.Blueprints;
using Pawnsmith.Application.Tests.Fixtures;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Tests.Blueprints;

/// <summary>
/// The model half of DEC-070. The disk half is tested with
/// <c>ProjectImageFiles</c>, in the Infrastructure tests.
/// </summary>
public class BlueprintRemovalTests
{
    private static readonly Guid Other = new("6e3f8a25-14bd-4c07-9f81-a5d206e3b9c1");

    [Fact]
    public void RemovingABlueprintListsEveryFileItsCandidatesReferenced()
    {
        Candidate elected = ProjectFixture.Candidate();
        Candidate draft = ProjectFixture.Candidate(id: Other, cutOut: false);
        Blueprint blueprint = ProjectFixture.Blueprint(candidates: [elected, draft], electedCandidateId: elected.Id);
        Project project = ProjectFixture.Project(blueprints: [blueprint, ProjectFixture.Blueprint(id: Other, race: "ogre")]);

        RemovedBlueprint removed = BlueprintRemoval.Remove(project, ProjectFixture.BlueprintId);

        removed.Project.Blueprints.ShouldHaveSingleItem().Id.ShouldBe(Other);
        removed.Removed.Id.ShouldBe(ProjectFixture.BlueprintId);
        removed.ReferencedFiles.ShouldBe(
        [
            elected.PairedImageFile!,
            elected.FrontImageFile!,
            elected.BackImageFile!,
            draft.PairedImageFile!,
        ]);
    }

    [Fact]
    public void AnElectedCandidateDoesNotProtectItsBlueprint()
    {
        Candidate elected = ProjectFixture.Candidate();
        Blueprint blueprint = ProjectFixture.Blueprint(candidates: [elected], electedCandidateId: elected.Id);
        Project project = ProjectFixture.Project(blueprints: [blueprint]);

        Should.NotThrow(() => BlueprintRemoval.Remove(project, ProjectFixture.BlueprintId));
    }

    [Fact]
    public void AFileReferencedTwiceIsListedOnce()
    {
        Candidate first = ProjectFixture.Candidate();
        Candidate second = ProjectFixture.Candidate(id: Other) with { PairedImageFile = first.PairedImageFile };
        Blueprint blueprint = ProjectFixture.Blueprint(candidates: [first, second]);

        RemovedBlueprint removed = BlueprintRemoval.Remove(ProjectFixture.Project(blueprints: [blueprint]), ProjectFixture.BlueprintId);

        removed.ReferencedFiles.Count(path => path == first.PairedImageFile).ShouldBe(1);
    }

    [Fact]
    public void TheOtherBlueprintsKeepTheirOrder()
    {
        var third = Guid.NewGuid();
        Project project = ProjectFixture.Project(blueprints:
        [
            ProjectFixture.Blueprint(id: Other, race: "ogre"),
            ProjectFixture.Blueprint(),
            ProjectFixture.Blueprint(id: third, race: "troll"),
        ]);

        RemovedBlueprint removed = BlueprintRemoval.Remove(project, ProjectFixture.BlueprintId);

        removed.Project.Blueprints.Select(blueprint => blueprint.Id).ShouldBe([Other, third]);
    }

    [Fact]
    public void AnUnknownBlueprintIsRefusedWithACode()
    {
        Should.Throw<BlueprintRuleException>(() =>
                BlueprintRemoval.Remove(ProjectFixture.Project(), Guid.NewGuid()))
            .Code.ShouldBe(BlueprintRuleCode.BlueprintNotFound);
    }
}
