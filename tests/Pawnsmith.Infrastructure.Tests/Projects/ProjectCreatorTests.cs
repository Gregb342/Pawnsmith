using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// The half of C.3.2 that needs a disk: collision suffixes and the folder that
/// gets created.
/// </summary>
/// <remarks>
/// C.12 numbers no test for this, because the CLI it exists for is untested by
/// design. The rules it applies are not the CLI though — they are the naming
/// rules of C.3.2 — and leaving real logic in Infrastructure uncovered would be
/// a defect whatever the numbering says.
/// </remarks>
public class ProjectCreatorTests
{
    private static readonly DateTimeOffset Instant = new(2026, 9, 9, 14, 30, 15, TimeSpan.Zero);

    private static ProjectCreator Creator(TempWorkspace workspace) =>
        new(new ProjectRepositoryOptions(workspace.Root), new FixedClock(Instant));

    private static async Task<CreatedProject> Create(TempWorkspace workspace, string name) =>
        await Creator(workspace).CreateAsync(
            name, Universe.Fantasy, Geometry.TabAndSocket, "A4", CancellationToken.None);

    [Fact]
    public async Task ANewProjectGetsAFolderAFileAndTheTwoSubfolders()
    {
        using TempWorkspace workspace = new();

        CreatedProject created = await Create(workspace, "Donjon de la Griffe Noire");

        Path.GetFileName(created.Directory).ShouldBe("donjon-de-la-griffe-noire");
        File.Exists(Path.Combine(created.Directory, ProjectFileWriter.FileName)).ShouldBeTrue();
        Directory.Exists(Path.Combine(created.Directory, "images")).ShouldBeTrue();
        Directory.Exists(Path.Combine(created.Directory, "exports")).ShouldBeTrue();
    }

    [Fact]
    public async Task ANewProjectIsEmptyButAlreadyValid()
    {
        using TempWorkspace workspace = new();

        CreatedProject created = await Create(workspace, "Donjon");

        created.Project.Blueprints.ShouldBeEmpty();
        created.Project.CalibrationOverrides.ShouldBe(CalibrationOverrides.None);
        created.Project.ProjectId.ShouldNotBe(Guid.Empty);
        created.Project.CreatedAt.ShouldBe(Instant);
        created.Project.ModifiedAt.ShouldBe(Instant);

        // Valid means loadable, and that is the only assertion of the three that
        // could not have been made by reading the record back.
        LoadedProject loaded = await new ProjectReader(new ProjectRepositoryOptions(workspace.Root))
            .LoadAsync(created.Directory, ProjectCalibration.WithPaperFormats("A4"), CancellationToken.None);

        loaded.Project.ProjectId.ShouldBe(created.Project.ProjectId);
        loaded.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task TwoProjectsOfTheSameNameGetSuffixedFolders()
    {
        // C.3.2, and the point at which it differs from an import: creating a
        // second project called the same thing is ordinary, so it is suffixed
        // rather than refused.
        using TempWorkspace workspace = new();

        CreatedProject first = await Create(workspace, "Donjon");
        CreatedProject second = await Create(workspace, "Donjon");
        CreatedProject third = await Create(workspace, "Donjon");

        Path.GetFileName(first.Directory).ShouldBe("donjon");
        Path.GetFileName(second.Directory).ShouldBe("donjon-2");
        Path.GetFileName(third.Directory).ShouldBe("donjon-3");

        // Same name, three identities. The folder name carries no meaning
        // (DEC-047), and this is what that costs nothing to guarantee.
        new[] { first, second, third }
            .Select(created => created.Project.ProjectId)
            .Distinct()
            .Count()
            .ShouldBe(3);
    }

    [Fact]
    public async Task AHostileNameCannotDecideWhereTheProjectLands()
    {
        // MEN-009. The name is free text and never reaches a path by
        // concatenation.
        using TempWorkspace workspace = new();

        CreatedProject created = await Create(workspace, "../../etc/passwd");

        Path.GetDirectoryName(created.Directory).ShouldBe(workspace.Root);
        Path.GetFileName(created.Directory).ShouldBe("etc-passwd");
    }

    [Fact]
    public async Task ANameThatSurvivesNothingStillGetsAFolder()
    {
        // A project named entirely in a script with no ASCII transliteration
        // still needs somewhere to live, and the second one still needs to be
        // told apart from the first.
        using TempWorkspace workspace = new();

        CreatedProject first = await Create(workspace, "русский");
        CreatedProject second = await Create(workspace, "日本語");

        Path.GetFileName(first.Directory).ShouldBe(ProjectFolderName.Fallback);
        Path.GetFileName(second.Directory).ShouldBe(ProjectFolderName.Fallback + "-2");
    }
}
