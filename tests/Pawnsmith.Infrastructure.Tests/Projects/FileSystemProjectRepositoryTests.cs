using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// The one door in front of the five operations, exercised through the port.
/// </summary>
/// <remarks>
/// The five operations are each covered where they live, and repeating those
/// tests here would only prove that composition composes. What is tested is what
/// only exists at this seam: that the whole cycle runs through the interface, and
/// that the two mappings across the layer boundary do not lose anything.
/// </remarks>
public class FileSystemProjectRepositoryTests
{
    private static readonly DateTimeOffset Instant = new(2026, 9, 9, 15, 0, 0, TimeSpan.Zero);

    private static IProjectRepository Repository(TempWorkspace workspace) =>
        new FileSystemProjectRepository(
            new ProjectRepositoryOptions(workspace.Root),
            new FixedClock(Instant));

    private static Calibration Calibration() => ProjectCalibration.WithPaperFormats("A4");

    [Fact]
    public async Task TheWholeCycleRunsThroughThePort()
    {
        // Create, save, export, import, load — the five operations of chapter 7,
        // in the order somebody would actually use them, and never touching an
        // infrastructure type. That is the point: a use case in Application can
        // reach a project without knowing which of five classes does what.
        using TempWorkspace workspace = new();
        IProjectRepository repository = Repository(workspace);

        CreatedProjectResult created = await repository.CreateAsync(
            "Donjon de la Griffe Noire",
            Universe.Fantasy,
            Geometry.TabAndSocket,
            "A4",
            CancellationToken.None);

        Project renamed = created.Project with { Name = "Donjon rebaptisé" };
        Project saved = await repository.SaveAsync(created.Directory, renamed, CancellationToken.None);

        saved.Name.ShouldBe("Donjon rebaptisé");

        string archive = await repository.ExportArchiveAsync(
            created.Directory,
            ArchiveProfileKind.Backup,
            Path.Combine(workspace.Root, "archives"),
            CancellationToken.None);

        ImportedProjectResult imported = await repository.ImportArchiveAsync(
            archive, "Donjon restauré", Calibration(), CancellationToken.None);

        imported.Project.ProjectId.ShouldBe(created.Project.ProjectId);
        imported.Project.Name.ShouldBe("Donjon rebaptisé");

        LoadedProjectResult loaded = await repository.LoadAsync(
            imported.Directory, Calibration(), CancellationToken.None);

        loaded.Project.ProjectId.ShouldBe(created.Project.ProjectId);
        loaded.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheShareProfileCrossesTheBoundaryAsItself()
    {
        // The profile is mapped by hand between two enumerations that must not
        // reference each other (A.3). Four lines, and this is what keeps them
        // honest: a mapping that sent every profile to Backup would still
        // produce an archive, and only the absence of exports/ tells them apart.
        using TempWorkspace workspace = new();
        IProjectRepository repository = Repository(workspace);

        CreatedProjectResult created = await repository.CreateAsync(
            "Donjon", Universe.Fantasy, Geometry.TabAndSocket, "A4", CancellationToken.None);

        File.WriteAllBytes(
            Path.Combine(created.Directory, "exports", "planche.pdf"), "%PDF-1.7"u8.ToArray());

        string backup = await repository.ExportArchiveAsync(
            created.Directory, ArchiveProfileKind.Backup,
            Path.Combine(workspace.Root, "backups"), CancellationToken.None);

        string share = await repository.ExportArchiveAsync(
            created.Directory, ArchiveProfileKind.Share,
            Path.Combine(workspace.Root, "shares"), CancellationToken.None);

        Entries(backup).ShouldContain("exports/planche.pdf");
        Entries(share).ShouldNotContain("exports/planche.pdf");
    }

    [Fact]
    public async Task ADiagnosticSurvivesTheCrossingWithItsKindAndItsField()
    {
        // The diagnostics lose their infrastructure type at the boundary and
        // carry their kind as a name. This is the test that keeps the three
        // fields from being quietly dropped or reordered on the way.
        using TempWorkspace workspace = new();
        IProjectRepository repository = Repository(workspace);

        CreatedProjectResult created = await repository.CreateAsync(
            "Donjon", Universe.Fantasy, Geometry.TabAndSocket, "A4", CancellationToken.None);

        LoadedProjectResult loaded = await repository.LoadAsync(
            created.Directory,
            ProjectCalibration.WithPaperFormats("Letter"),
            CancellationToken.None);

        ProjectMismatch mismatch = loaded.Diagnostics.ShouldHaveSingleItem();

        mismatch.Kind.ShouldBe(nameof(ProjectDiagnosticKind.UnknownPaperFormat));
        mismatch.Field.ShouldBe("paperFormat");
        mismatch.Message.ShouldContain("A4");
    }

    private static IReadOnlyList<string> Entries(string archivePath)
    {
        using System.IO.Compression.ZipArchive zip =
            System.IO.Compression.ZipFile.OpenRead(archivePath);

        return [.. zip.Entries.Select(entry => entry.FullName)];
    }
}
