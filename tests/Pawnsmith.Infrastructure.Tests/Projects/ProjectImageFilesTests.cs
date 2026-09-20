using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// The disk half of DEC-070: exactly the listed files go, nothing else, and
/// nothing outside the folder.
/// </summary>
public class ProjectImageFilesTests
{
    [Fact]
    public void ExactlyTheListedFilesAreDeleted()
    {
        using TempWorkspace workspace = new();
        workspace.WritePng("a-front.png", 4, 4);
        workspace.WritePng("a-back.png", 4, 4);
        workspace.WritePng("keep.png", 4, 4);

        int removed = ProjectImageFiles.Delete(workspace.Root, ["images/a-front.png", "images/a-back.png"]);

        removed.ShouldBe(2);
        File.Exists(Path.Combine(workspace.ImagesDirectory, "a-front.png")).ShouldBeFalse();
        File.Exists(Path.Combine(workspace.ImagesDirectory, "a-back.png")).ShouldBeFalse();
        File.Exists(Path.Combine(workspace.ImagesDirectory, "keep.png")).ShouldBeTrue();
    }

    [Fact]
    public void AFileAlreadyGoneIsNotAnError()
    {
        using TempWorkspace workspace = new();

        int removed = ProjectImageFiles.Delete(workspace.Root, ["images/never-existed.png"]);

        removed.ShouldBe(0);
    }

    [Fact]
    public void APathThatEscapesTheFolderIsRefusedBeforeAnythingIsDeleted()
    {
        using TempWorkspace workspace = new();
        workspace.WritePng("a-front.png", 4, 4);

        ProjectException error = Should.Throw<ProjectException>(() =>
            ProjectImageFiles.Delete(workspace.Root, ["images/../../elsewhere.png", "images/a-front.png"]));

        error.Code.ShouldBe(ProjectErrorCode.PathEscape);

        // The refusal came first: the legitimate file after it is untouched.
        File.Exists(Path.Combine(workspace.ImagesDirectory, "a-front.png")).ShouldBeTrue();
    }

    [Fact]
    public void APathOutsideImagesIsRefused()
    {
        using TempWorkspace workspace = new();
        File.WriteAllText(Path.Combine(workspace.Root, "project.json"), "{}");

        Should.Throw<ProjectException>(() => ProjectImageFiles.Delete(workspace.Root, ["project.json"]))
            .Code.ShouldBe(ProjectErrorCode.PathEscape);

        File.Exists(Path.Combine(workspace.Root, "project.json")).ShouldBeTrue();
    }
}
