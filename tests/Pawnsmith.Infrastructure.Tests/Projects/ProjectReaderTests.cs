using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Sheets;
using Pawnsmith.Domain.Units;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// Covers tests 30 to 32 of C.12 and the loading half of test 53: what a load
/// tolerates, and what it says about it.
/// </summary>
public class ProjectReaderTests
{
    /// <summary>Writes a project into a folder under a projects root, and returns that folder.</summary>
    private static async Task<string> Persist(TempWorkspace workspace, Project project, string folderName = "donjon")
    {
        string directory = Path.Combine(workspace.Root, folderName);
        Directory.CreateDirectory(Path.Combine(directory, "images"));

        await ProjectFileWriter.WriteAsync(
            directory,
            ProjectJson.Serialize(project.ToDocument()),
            CancellationToken.None);

        return directory;
    }

    /// <summary>Puts every image the project references on the disk, so nothing is diagnosed.</summary>
    private static void PlaceImages(string projectDirectory, Project project)
    {
        IEnumerable<string> paths = project.Blueprints
            .SelectMany(blueprint => blueprint.Candidates)
            .SelectMany(candidate => new[]
            {
                candidate.PairedImageFile,
                candidate.FrontImageFile,
                candidate.BackImageFile,
            })
            .OfType<string>();

        foreach (string relative in paths)
        {
            File.WriteAllBytes(Path.Combine(projectDirectory, relative), [0x89, 0x50, 0x4E, 0x47]);
        }
    }

    private static ProjectReader Reader(TempWorkspace workspace) =>
        new(new ProjectRepositoryOptions(workspace.Root));

    private static Calibration Calibration() => ProjectCalibration.WithPaperFormats("A4");

    // --- Le cas ordinaire ---------------------------------------------------

    [Fact]
    public async Task AWholeProjectLoadsWithNothingToReport()
    {
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich();
        string directory = await Persist(workspace, source);
        PlaceImages(directory, source);

        LoadedProject loaded = await Reader(workspace)
            .LoadAsync(directory, Calibration(), CancellationToken.None);

        loaded.Diagnostics.ShouldBeEmpty();
        loaded.Project.Name.ShouldBe(source.Name);
        loaded.Project.Blueprints.Count.ShouldBe(source.Blueprints.Count);
    }

    // --- C.12 n° 30 : une image absente n'échoue pas le chargement ---------

    [Fact]
    public async Task AReferencedImageMissingFromTheDiskIsReportedAndNothingMore()
    {
        // The deliberate departure from the manifest of T1, where a missing
        // image is fatal. A project is a workspace: refusing to open it would
        // make it unrepairable, since the only way to drop the offending
        // candidate would be to hand-edit the JSON (C.7.2).
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich();
        string directory = await Persist(workspace, source);

        LoadedProject loaded = await Reader(workspace)
            .LoadAsync(directory, Calibration(), CancellationToken.None);

        loaded.Project.ShouldNotBeNull();
        loaded.Diagnostics.ShouldAllBe(d => d.Kind == ProjectDiagnosticKind.MissingImageFile);
        loaded.Diagnostics.ShouldContain(d => d.Field.EndsWith("frontImageFile", StringComparison.Ordinal));
    }

    // --- C.12 n° 31 : un format de papier inconnu -------------------------

    [Fact]
    public async Task AnUnknownPaperFormatIsReportedAndTheProjectStillLoads()
    {
        // A project created where the calibration declares A4Paysage (DEC-036)
        // has to open on a machine that has no such entry.
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich() with { PaperFormatName = "A4Paysage" };
        string directory = await Persist(workspace, source);
        PlaceImages(directory, source);

        LoadedProject loaded = await Reader(workspace)
            .LoadAsync(directory, Calibration(), CancellationToken.None);

        loaded.Project.PaperFormatName.ShouldBe("A4Paysage");
        loaded.Diagnostics.ShouldContain(d => d.Kind == ProjectDiagnosticKind.UnknownPaperFormat);
    }

    // --- C.12 n° 32 : un dossier inconnu ne gêne pas -----------------------

    [Fact]
    public async Task AnUnknownFolderInTheProjectDoesNotPreventLoading()
    {
        // It will never travel in an archive - the whitelist of C.8.2 sees to
        // that - but its presence is the user's business, not ours.
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich();
        string directory = await Persist(workspace, source);
        PlaceImages(directory, source);
        Directory.CreateDirectory(Path.Combine(directory, "notes"));
        File.WriteAllText(Path.Combine(directory, "notes", "todo.txt"), "revoir l'orc");

        LoadedProject loaded = await Reader(workspace)
            .LoadAsync(directory, Calibration(), CancellationToken.None);

        loaded.Diagnostics.ShouldBeEmpty();
    }

    // --- C.12 n° 53 : la surcharge incompatible, et sa géométrie ----------

    [Fact]
    public async Task AnOverrideWiderThanThePawnLoadsAndIsReported()
    {
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich() with
        {
            Geometry = Geometry.TabAndSocket,
            CalibrationOverrides = new CalibrationOverrides(TabWidthMm: 40.0, TabHeightMm: null),
        };
        string directory = await Persist(workspace, source);
        PlaceImages(directory, source);

        LoadedProject loaded = await Reader(workspace)
            .LoadAsync(directory, Calibration(), CancellationToken.None);

        ProjectDiagnostic diagnostic = loaded.Diagnostics
            .Where(d => d.Kind == ProjectDiagnosticKind.OverrideExceedsPawnWidth)
            .ShouldHaveSingleItem();

        diagnostic.Message.ShouldContain("40");
        diagnostic.Field.ShouldBe("calibrationOverrides.tabWidthMm");
    }

    [Theory]
    [InlineData(Geometry.FoldedTent)]
    [InlineData(Geometry.NoSupport)]
    public async Task TheSameOverrideIsReportedNowhereWithoutATab(Geometry geometry)
    {
        // The correction that this whole check exists for. Neither of these
        // geometries has a tab, so the value is never read and no value could
        // ever clear the warning - the false positive C.5.4 names as the failure
        // mode to avoid.
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich() with
        {
            Geometry = geometry,
            CalibrationOverrides = new CalibrationOverrides(TabWidthMm: 40.0, TabHeightMm: null),
        };
        string directory = await Persist(workspace, source);
        PlaceImages(directory, source);

        LoadedProject loaded = await Reader(workspace)
            .LoadAsync(directory, Calibration(), CancellationToken.None);

        loaded.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void ItIsTheSheetCalculationThatRefuses()
    {
        // The other half of test 53, and the layer correction it carries.
        // Merging the overrides validates nothing - it copies fields. The truth
        // "a tab cannot be wider than the pawn" stays written where DEC-038 put
        // it, in the cut outline, which is reached when the sheet is calculated.
        Calibration effective = new(
            Calibration().VersionSchema,
            Calibration().Paper,
            Calibration().Sizes,
            new GeometrySettings(
                Calibration().Geometry.FoldedTent,
                new TabAndSocketSettings(TabWidthMm: 40.0, TabHeightMm: 10.0)),
            Calibration().Layout,
            Calibration().Print,
            Calibration().Strokes,
            Calibration().PaperFormats);

        ArgumentOutOfRangeException error = Should.Throw<ArgumentOutOfRangeException>(
            () => UnfoldedUnit.Create(
                Size.Medium,
                effective.Sizes[Size.Medium],
                Geometry.TabAndSocket,
                effective.Geometry));

        error.Message.ShouldContain("cannot exceed pawn width");
    }

    // --- Ce que le chargement refuse ---------------------------------------

    [Fact]
    public async Task AMissingProjectFileIsRefusedWithItsOwnCode()
    {
        using TempWorkspace workspace = new();
        string directory = Path.Combine(workspace.Root, "vide");
        Directory.CreateDirectory(directory);

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Reader(workspace).LoadAsync(directory, Calibration(), CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.NotFound);
    }

    [Fact]
    public async Task AFolderOutsideTheProjectsRootIsRefused()
    {
        using TempWorkspace workspace = new();
        using TempWorkspace elsewhere = new();
        Project source = ProjectSample.Rich();
        string directory = await Persist(elsewhere, source);

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Reader(workspace).LoadAsync(directory, Calibration(), CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.PathEscape);
    }

    [Fact]
    public async Task ATraversalOutOfTheRootIsRefused()
    {
        using TempWorkspace workspace = new();
        string escaping = Path.Combine(workspace.Root, "..", "ailleurs");

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Reader(workspace).LoadAsync(escaping, Calibration(), CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.PathEscape);
    }

    [Fact]
    public async Task ASiblingFolderWhoseNameStartsLikeTheRootIsRefused()
    {
        // The trap a plain StartsWith walks into: "/data/projects-evil" begins
        // with "/data/projects", so comparing the two strings says it is inside
        // the root. It is not - it is a sibling. Comparing against the root plus
        // a separator is what settles it, and nothing else here would have
        // noticed the difference.
        using TempWorkspace workspace = new();
        string sibling = workspace.Root + "-evil";
        Directory.CreateDirectory(sibling);

        try
        {
            Project source = ProjectSample.Rich();
            await ProjectFileWriter.WriteAsync(
                sibling,
                ProjectJson.Serialize(source.ToDocument()),
                CancellationToken.None);

            ProjectException error = await Should.ThrowAsync<ProjectException>(
                () => Reader(workspace).LoadAsync(sibling, Calibration(), CancellationToken.None));

            error.Code.ShouldBe(ProjectErrorCode.PathEscape);
        }
        finally
        {
            Directory.Delete(sibling, recursive: true);
        }
    }

    [Fact]
    public async Task TheProjectsRootItselfIsNotAProject()
    {
        // The other edge of the same comparison: the root is not below itself in
        // the "a project folder lives in it" sense, but it must not be refused as
        // an escape either. It simply holds no project file.
        using TempWorkspace workspace = new();

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Reader(workspace).LoadAsync(workspace.Root, Calibration(), CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.NotFound);
    }

    [Fact]
    public async Task AFileWorthMoreThanTheBoundIsRefusedBeforeBeingRead()
    {
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich();
        string directory = await Persist(workspace, source);

        ProjectReader reader = new(new ProjectRepositoryOptions(workspace.Root)
        {
            MaxProjectFileBytes = 10,
        });

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => reader.LoadAsync(directory, Calibration(), CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.TooLarge);
        error.WireCode.ShouldBe("PROJECT_TOO_LARGE");
    }

    [Fact]
    public async Task AnUnknownMemberInTheFileIsRefusedAndNamed()
    {
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich();
        string directory = await Persist(workspace, source);
        string path = Path.Combine(directory, ProjectFileWriter.FileName);

        string tampered = (await File.ReadAllTextAsync(path))
            .Replace("\"versionSchema\": 1,", "\"versionSchema\": 1,\n  \"couleur\": \"bleu\",", StringComparison.Ordinal);
        await File.WriteAllTextAsync(path, tampered);

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Reader(workspace).LoadAsync(directory, Calibration(), CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.Invalid);
        error.Message.ShouldContain("couleur");
    }

    [Fact]
    public async Task AFileThatIsNotJsonAtAllIsRefusedRatherThanCrashing()
    {
        using TempWorkspace workspace = new();
        string directory = Path.Combine(workspace.Root, "casse");
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, ProjectFileWriter.FileName), "ceci n'est pas du JSON");

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Reader(workspace).LoadAsync(directory, Calibration(), CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.Invalid);
    }
}
