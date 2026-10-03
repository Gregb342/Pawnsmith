using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// The paired image of a new candidate reaches the disk under a name Pawnsmith
/// chose, inside the project folder, and never half-written (task 7 of E.17).
/// </summary>
public class PairedImageWriteTests
{
    private static readonly Guid CandidateId = new("b4c7e910-2f88-4d16-9a03-5e1c8b72d055");

    [Fact]
    public async Task TheImageIsWrittenUnderItsCandidateNameAndThePathIsReturned()
    {
        using TempWorkspace workspace = new();
        byte[] png = TestPng.Create(16, 8);

        string relative = await ProjectImageFiles.WritePairedAsync(workspace.Root, CandidateId, png, CancellationToken.None);

        relative.ShouldBe("images/b4c7e910-2f88-4d16-9a03-5e1c8b72d055-pair.png");
        (await File.ReadAllBytesAsync(Path.Combine(workspace.Root, "images", "b4c7e910-2f88-4d16-9a03-5e1c8b72d055-pair.png")))
            .ShouldBe(png);
    }

    [Fact]
    public async Task NoTemporaryFileOutlivesTheWrite()
    {
        using TempWorkspace workspace = new();

        await ProjectImageFiles.WritePairedAsync(workspace.Root, CandidateId, TestPng.Create(4, 4), CancellationToken.None);

        Directory.GetFiles(workspace.ImagesDirectory).Select(Path.GetFileName)
            .ShouldBe(["b4c7e910-2f88-4d16-9a03-5e1c8b72d055-pair.png"]);
    }

    [Fact]
    public async Task AMissingImagesFolderIsCreated()
    {
        using TempWorkspace workspace = new();
        Directory.Delete(workspace.ImagesDirectory);

        await ProjectImageFiles.WritePairedAsync(workspace.Root, CandidateId, TestPng.Create(4, 4), CancellationToken.None);

        Directory.Exists(workspace.ImagesDirectory).ShouldBeTrue();
    }

    [Fact]
    public async Task AnExistingFileIsNeverOverwrittenAndNoTemporaryIsLeft()
    {
        using TempWorkspace workspace = new();
        byte[] original = TestPng.Create(4, 4, grey: 0x10);
        await ProjectImageFiles.WritePairedAsync(workspace.Root, CandidateId, original, CancellationToken.None);

        await Should.ThrowAsync<IOException>(() =>
            ProjectImageFiles.WritePairedAsync(workspace.Root, CandidateId, TestPng.Create(4, 4, grey: 0xF0), CancellationToken.None));

        (await File.ReadAllBytesAsync(Path.Combine(workspace.ImagesDirectory, $"{CandidateId}-pair.png"))).ShouldBe(original);
        Directory.GetFiles(workspace.ImagesDirectory).Length.ShouldBe(1);
    }

    [Fact]
    public async Task AMissingProjectFolderIsNotFound()
    {
        string absent = Path.Combine(Path.GetTempPath(), "pawnsmith-tests", Guid.NewGuid().ToString("N"));

        ProjectException error = await Should.ThrowAsync<ProjectException>(() =>
            ProjectImageFiles.WritePairedAsync(absent, CandidateId, TestPng.Create(4, 4), CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.NotFound);
        Directory.Exists(absent).ShouldBeFalse();
    }

    [Fact]
    public async Task AnImagesFolderThatIsASymbolicLinkIsRefused()
    {
        if (!SymbolicLinks.AreSupported)
        {
            // Same position as the MEN-008 test: this machine cannot create a
            // link, so the CI on Ubuntu is what certifies this case.
            Console.WriteLine("Symbolic links unavailable here; the CI exercises this test.");
            return;
        }

        using TempWorkspace workspace = new();
        using TempWorkspace elsewhere = new();
        Directory.Delete(workspace.ImagesDirectory);
        Directory.CreateSymbolicLink(workspace.ImagesDirectory, elsewhere.ImagesDirectory);

        ProjectException error = await Should.ThrowAsync<ProjectException>(() =>
            ProjectImageFiles.WritePairedAsync(workspace.Root, CandidateId, TestPng.Create(4, 4), CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.PathEscape);
        Directory.GetFiles(elsewhere.ImagesDirectory).ShouldBeEmpty();
    }
}
