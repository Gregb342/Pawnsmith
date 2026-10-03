using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// The three repository operations T6 added: list, delete images, open an
/// image. Supports tests 6, 8, 14 and 15 of G.13.
/// </summary>
public class ProjectListingTests
{
    private static Calibration Calibration() => ProjectCalibration.WithPaperFormats("A4");

    private static IProjectRepository Repository(TempWorkspace workspace) =>
        new FileSystemProjectRepository(new ProjectRepositoryOptions(Path.Combine(workspace.Root, "projects")));

    [Fact]
    public async Task ProjectsAreListedByFolderInOrdinalOrderAndABrokenOneKeepsItsPlace()
    {
        using TempWorkspace workspace = new();
        IProjectRepository repository = Repository(workspace);

        await repository.CreateAsync("Zeta", Universe.Fantasy, Geometry.TabAndSocket, "A4", CancellationToken.None);
        CreatedProjectResult alpha = await repository.CreateAsync("alpha", Universe.Fantasy, Geometry.TabAndSocket, "A4", CancellationToken.None);
        await repository.CreateAsync("Beta", Universe.Fantasy, Geometry.TabAndSocket, "A4", CancellationToken.None);

        // Broken by hand, the way an edit gone wrong would.
        await File.WriteAllTextAsync(Path.Combine(alpha.Directory, "project.json"), "{ \"versionSchema\": 99 }");

        // A folder without project.json is not a project.
        Directory.CreateDirectory(Path.Combine(workspace.Root, "projects", "not-a-project"));

        IReadOnlyList<ProjectListing> listed = await repository.ListAsync(Calibration(), CancellationToken.None);

        listed.Select(listing => listing.Folder).ShouldBe(["alpha", "beta", "zeta"]);
        listed[0].Project.ShouldBeNull();
        listed[0].ErrorCode.ShouldBe("PROJECT_SCHEMA_TOO_RECENT");
        listed[1].Project!.Name.ShouldBe("Beta");
        listed[1].ErrorCode.ShouldBeNull();
    }

    [Fact]
    public async Task AMissingRootListsNothing()
    {
        using TempWorkspace workspace = new();

        (await Repository(workspace).ListAsync(Calibration(), CancellationToken.None)).ShouldBeEmpty();
    }

    [Fact]
    public async Task ImagesAreDeletedThroughThePort()
    {
        using TempWorkspace workspace = new();
        workspace.WritePng("a-front.png", 4, 4);
        workspace.WritePng("keep.png", 4, 4);
        IProjectRepository repository = new FileSystemProjectRepository(new ProjectRepositoryOptions(workspace.Root));

        int removed = await repository.DeleteImagesAsync(workspace.Root, ["images/a-front.png"], CancellationToken.None);

        removed.ShouldBe(1);
        File.Exists(Path.Combine(workspace.ImagesDirectory, "keep.png")).ShouldBeTrue();
    }

    [Fact]
    public async Task AnImageOpensForReadingAndAMissingOneIsNull()
    {
        using TempWorkspace workspace = new();
        byte[] png = TestPng.Create(4, 4);
        await File.WriteAllBytesAsync(Path.Combine(workspace.ImagesDirectory, "a-pair.png"), png);
        IProjectRepository repository = new FileSystemProjectRepository(new ProjectRepositoryOptions(workspace.Root));

        await using (Stream? stream = await repository.OpenImageAsync(workspace.Root, "images/a-pair.png", CancellationToken.None))
        {
            using var copy = new MemoryStream();
            await stream!.CopyToAsync(copy);
            copy.ToArray().ShouldBe(png);
        }

        (await repository.OpenImageAsync(workspace.Root, "images/absent.png", CancellationToken.None)).ShouldBeNull();
    }

    [Theory]
    [InlineData("../project.json")]
    [InlineData("images/../../etc/passwd")]
    [InlineData("/etc/passwd")]
    [InlineData("exports/sheet.pdf")]
    public async Task APathThatLeavesTheImagesIsRefused(string path)
    {
        using TempWorkspace workspace = new();
        IProjectRepository repository = new FileSystemProjectRepository(new ProjectRepositoryOptions(workspace.Root));

        ProjectException error = await Should.ThrowAsync<ProjectException>(() =>
            repository.OpenImageAsync(workspace.Root, path, CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.PathEscape);
    }
}
