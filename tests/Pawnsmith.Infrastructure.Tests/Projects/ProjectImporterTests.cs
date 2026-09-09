using System.Security.Cryptography;

using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// Covers tests 41, 42 and 50 to 52 of C.12, and the import half of test 53.
/// </summary>
/// <remarks>
/// Everything an import refuses <i>before</i> writing is tested against
/// <see cref="ArchiveInspector"/>. What is left here is the half that writes:
/// the round trips, the destination rules, and the promise that a failure leaves
/// nothing behind.
/// </remarks>
public class ProjectImporterTests
{
    private static Calibration Calibration() => ProjectCalibration.WithPaperFormats("A4");

    private static ProjectRepositoryOptions Options(TempWorkspace workspace) => new(workspace.Root);

    private static ProjectImporter Importer(TempWorkspace workspace) => new(Options(workspace));

    private static ProjectReader Reader(TempWorkspace workspace) => new(Options(workspace));

    /// <summary>Lays down a real project folder, files included.</summary>
    private static async Task<string> Populated(TempWorkspace workspace, Project project, string folder = "donjon")
    {
        string directory = Path.Combine(workspace.Root, folder);
        Directory.CreateDirectory(Path.Combine(directory, "images"));
        Directory.CreateDirectory(Path.Combine(directory, "exports"));

        await ProjectFileWriter.WriteAsync(
            directory, ProjectJson.Serialize(project.ToDocument()), CancellationToken.None);

        foreach (string relative in ShareFilter.ReferencedImages(project.ToDocument()))
        {
            // Distinct content per file, so that a round trip that mixed two of
            // them up would show as a different hash rather than pass.
            File.WriteAllBytes(
                Path.Combine(directory, relative),
                [.. ArchiveBuilder.Png(), .. System.Text.Encoding.UTF8.GetBytes(relative)]);
        }

        File.WriteAllBytes(Path.Combine(directory, "exports", "planche.pdf"), "%PDF-1.7"u8.ToArray());

        return directory;
    }

    private static async Task<string> Exported(
        TempWorkspace workspace,
        string projectDirectory,
        ArchiveProfile profile)
    {
        string destination = Path.Combine(workspace.Root, "archives");

        return await new ProjectExporter().ExportAsync(
            projectDirectory, profile, destination, CancellationToken.None);
    }

    /// <summary>Asserts two projects hold the same thing, field for field.</summary>
    /// <remarks>
    /// Not <c>ShouldBe</c> on the records themselves, and the reason is a trap
    /// worth knowing: <see cref="Project"/> holds its blueprints in an
    /// <c>IReadOnlyList</c>, and a record compares a member of interface type by
    /// reference. Two projects with identical contents in differently-typed
    /// lists therefore compare unequal — which is what the first run of these
    /// tests showed.
    /// <para>
    /// Comparing the serialised documents is the stronger answer rather than a
    /// workaround. It covers the whole model at once, collections and their
    /// order included, and the writer is already known to be deterministic —
    /// test 19 asserts that two saves of an unchanged project are identical byte
    /// for byte. So byte equality here means exactly "no loss", which is the
    /// acceptance criterion being measured.
    /// </para>
    /// </remarks>
    private static void ShouldHoldTheSameAs(Project actual, Project expected) =>
        ProjectJson.Serialize(actual.ToDocument())
            .ShouldBe(ProjectJson.Serialize(expected.ToDocument()));

    /// <summary>Every file of a folder, by relative path, with the hash of its bytes.</summary>
    private static Dictionary<string, string> Fingerprints(string directory)
    {
        return Directory
            .EnumerateFiles(directory, "*", SearchOption.AllDirectories)
            .ToDictionary(
                path => Path.GetRelativePath(directory, path).Replace(Path.DirectorySeparatorChar, '/'),
                path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),
                StringComparer.Ordinal);
    }

    // --- C.12 n° 41 : l'aller-retour Backup, à l'empreinte ----------------

    [Fact]
    public async Task ABackupRoundTripLosesNothing()
    {
        // The acceptance criterion of T2, and it is measured rather than
        // asserted in general terms: every file of the imported folder is
        // compared to its source by hash.
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich();
        string directory = await Populated(workspace, source);
        string archive = await Exported(workspace, directory, ArchiveProfile.Backup);

        ImportedProject imported = await Importer(workspace)
            .ImportAsync(archive, "Donjon restauré", Calibration(), CancellationToken.None);

        Dictionary<string, string> before = Fingerprints(directory);
        Dictionary<string, string> after = Fingerprints(imported.Directory);

        after.ShouldBe(before, ignoreOrder: true);
        ShouldHoldTheSameAs(imported.Project, source);
        imported.Diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public async Task AnImportedProjectLoadsFromItsNewFolder()
    {
        // "Complete" has to mean loadable, not merely present: an import that
        // wrote every file but left the folder unreadable would satisfy a
        // hash comparison and nothing else.
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich();
        string archive = await Exported(
            workspace, await Populated(workspace, source), ArchiveProfile.Backup);

        ImportedProject imported = await Importer(workspace)
            .ImportAsync(archive, "Donjon restauré", Calibration(), CancellationToken.None);

        LoadedProject reloaded = await Reader(workspace)
            .LoadAsync(imported.Directory, Calibration(), CancellationToken.None);

        ShouldHoldTheSameAs(reloaded.Project, source);
        reloaded.Diagnostics.ShouldBeEmpty();
    }

    // --- C.12 n° 42 : l'aller-retour Share ---------------------------------

    [Fact]
    public async Task AShareRoundTripKeepsEverythingItDidNotDeliberatelyDrop()
    {
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich();
        string archive = await Exported(
            workspace, await Populated(workspace, source), ArchiveProfile.Share);

        ImportedProject imported = await Importer(workspace)
            .ImportAsync(archive, "Donjon partagé", Calibration(), CancellationToken.None);

        Blueprint blueprint = imported.Project.Blueprints[0];

        // What was deliberately removed: the rejected candidate, every paired
        // image, and exports/.
        blueprint.Candidates.ShouldHaveSingleItem().Status.ShouldBe(CandidateStatus.Valid);
        blueprint.Candidates[0].PairedImageFile.ShouldBeNull();
        Directory.Exists(Path.Combine(imported.Directory, "exports")).ShouldBeFalse();

        // What was not: the empty blueprint, the election, the clauses.
        imported.Project.Blueprints.Count.ShouldBe(source.Blueprints.Count);
        blueprint.ElectedCandidateId.ShouldBe(source.Blueprints[0].ElectedCandidateId);
        imported.Project.Style.ShouldBe(source.Style);
    }

    [Fact]
    public async Task AnImportedShareHasNoDanglingReference()
    {
        // The invariant of DEC-050, checked from the receiving end: every file
        // the project names is on the disk, and every image on the disk is named.
        using TempWorkspace workspace = new();
        string archive = await Exported(
            workspace, await Populated(workspace, ProjectSample.Rich()), ArchiveProfile.Share);

        ImportedProject imported = await Importer(workspace)
            .ImportAsync(archive, "Donjon partagé", Calibration(), CancellationToken.None);

        IReadOnlyList<string> referenced = ShareFilter.ReferencedImages(imported.Project.ToDocument());

        foreach (string relative in referenced)
        {
            File.Exists(Path.Combine(imported.Directory, relative)).ShouldBeTrue(relative);
        }

        Directory
            .EnumerateFiles(Path.Combine(imported.Directory, "images"))
            .Select(Path.GetFileName)
            .ShouldBe(referenced.Select(Path.GetFileName), ignoreOrder: true);
    }

    // --- C.12 n° 50 : la destination existante ----------------------------

    [Fact]
    public async Task AnExistingDestinationIsRefusedAndLeftAlone()
    {
        using TempWorkspace workspace = new();
        string archive = await Exported(
            workspace, await Populated(workspace, ProjectSample.Rich()), ArchiveProfile.Backup);

        // Empty, and it still counts: filling it would be a merge in all but
        // name, and whoever made it meant something by it.
        string destination = Path.Combine(workspace.Root, "deja-la");
        Directory.CreateDirectory(destination);

        ProjectException refusal = await Should.ThrowAsync<ProjectException>(
            () => Importer(workspace).ImportAsync(archive, "Déjà là", Calibration(), CancellationToken.None));

        refusal.Code.ShouldBe(ProjectErrorCode.ImportDestinationExists);
        refusal.WireCode.ShouldBe("IMPORT_DESTINATION_EXISTS");
        Directory.EnumerateFileSystemEntries(destination).ShouldBeEmpty();
    }

    [Fact]
    public async Task AnImportDoomedByItsDestinationNeverEvenOpensTheArchive()
    {
        // The destination is checked twice, once before the archive is touched
        // and once immediately before the folder appears. The second is the one
        // that matters for correctness; this test is what keeps the first from
        // becoming decoration, and it distinguishes them the only way available:
        // the archive does not exist. Checking the destination first gives
        // IMPORT_DESTINATION_EXISTS, checking it only at the end gives
        // PROJECT_NOT_FOUND.
        using TempWorkspace workspace = new();
        Directory.CreateDirectory(Path.Combine(workspace.Root, "deja-la"));

        ProjectException refusal = await Should.ThrowAsync<ProjectException>(
            () => Importer(workspace).ImportAsync(
                Path.Combine(workspace.Root, "nowhere.zip"),
                "Déjà là",
                Calibration(),
                CancellationToken.None));

        refusal.Code.ShouldBe(ProjectErrorCode.ImportDestinationExists);
    }

    [Fact]
    public async Task ASuccessfulImportLeavesNoStagingFolderBehind()
    {
        // The staging folder is *moved* into place, not copied into it. This
        // does not prove the move is atomic — nothing a unit test can do proves
        // that, the same limitation the atomic file swap of task 8 carries — but
        // it does pin the shape: a copy would leave the staging folder sitting
        // in the projects root, looking like a half-finished import.
        using TempWorkspace workspace = new();
        string archive = await Exported(
            workspace, await Populated(workspace, ProjectSample.Rich()), ArchiveProfile.Backup);

        await Importer(workspace).ImportAsync(archive, "Donjon copie", Calibration(), CancellationToken.None);

        Directory
            .EnumerateDirectories(workspace.Root, ProjectImporter.StagingPrefix + "*")
            .ShouldBeEmpty();
    }

    // --- C.12 n° 51 : l'échec en cours d'extraction ------------------------

    [Fact]
    public async Task AFailureDuringExtractionLeavesNoDestinationAndNoStagingFolder()
    {
        // A genuine failure part of the way through, not one at the door: the
        // damaged entry is an image, which the inspection never reads, so the
        // archive passes every check made before extraction and breaks with real
        // files already written beside it. That is what makes the assertion
        // below mean something - the staging folder that gets removed is a full
        // one.
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich();
        string directory = await Populated(workspace, source);

        string damaged = ShareFilter.ReferencedImages(source.ToDocument()).Order(StringComparer.Ordinal).Last();
        File.WriteAllBytes(Path.Combine(directory, damaged), ArchiveBuilder.Compressible());

        string archive = await Exported(workspace, directory, ArchiveProfile.Backup);
        ArchiveBuilder.CorruptEntryData(archive, damaged);

        await Should.ThrowAsync<Exception>(
            () => Importer(workspace).ImportAsync(archive, "Abimé", Calibration(), CancellationToken.None));

        Directory.Exists(Path.Combine(workspace.Root, "abime")).ShouldBeFalse();

        Directory
            .EnumerateDirectories(workspace.Root, ProjectImporter.StagingPrefix + "*")
            .ShouldBeEmpty();
    }

    [Fact]
    public async Task ARefusedArchiveCreatesNothingAtTheDestination()
    {
        using TempWorkspace workspace = new();
        string archive = ArchiveBuilder
            .Valid(ProjectSample.Rich())
            .With("../../evil.txt", "owned")
            .WriteTo(Path.Combine(workspace.Root, "hostile.zip"));

        await Should.ThrowAsync<ProjectException>(
            () => Importer(workspace).ImportAsync(archive, "Hostile", Calibration(), CancellationToken.None));

        Directory.Exists(Path.Combine(workspace.Root, "hostile")).ShouldBeFalse();
        File.Exists(Path.Combine(workspace.Root, "evil.txt")).ShouldBeFalse();
        File.Exists(Path.Combine(Path.GetTempPath(), "evil.txt")).ShouldBeFalse();

        Directory
            .EnumerateDirectories(workspace.Root, ProjectImporter.StagingPrefix + "*")
            .ShouldBeEmpty();
    }

    // --- C.12 n° 52 : le projectId en double -------------------------------

    [Fact]
    public async Task TheSameArchiveImportedTwiceGivesTwoUsableProjectsWithOneIdentity()
    {
        // A copy, not a corruption (DEC-047). The identity is preserved because
        // the dominant case is restoring one's own backup, where changing it
        // would simply be wrong; choosing between "replace" and "keep both"
        // means asking somebody, so it is T6.
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich();
        string archive = await Exported(
            workspace, await Populated(workspace, source), ArchiveProfile.Backup);

        ProjectImporter importer = Importer(workspace);

        ImportedProject first = await importer
            .ImportAsync(archive, "Copie une", Calibration(), CancellationToken.None);
        ImportedProject second = await importer
            .ImportAsync(archive, "Copie deux", Calibration(), CancellationToken.None);

        first.Directory.ShouldNotBe(second.Directory);
        first.Project.ProjectId.ShouldBe(source.ProjectId);
        second.Project.ProjectId.ShouldBe(source.ProjectId);

        ProjectReader reader = Reader(workspace);

        (await reader.LoadAsync(first.Directory, Calibration(), CancellationToken.None))
            .Project.ProjectId.ShouldBe(source.ProjectId);
        (await reader.LoadAsync(second.Directory, Calibration(), CancellationToken.None))
            .Project.ProjectId.ShouldBe(source.ProjectId);
    }

    // --- C.12 n° 53, moitié import : le diagnostic ne bloque pas ----------

    [Fact]
    public async Task AnOverrideWiderThanThePawnImportsAndIsReported()
    {
        // DEC-056 from the import side, and the scenario the Share profile exists
        // to make pleasant: an archive that is perfectly coherent, produced by
        // somebody whose bases have a different slot. It imports. It says so.
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich() with
        {
            Geometry = Geometry.TabAndSocket,
            CalibrationOverrides = new CalibrationOverrides(TabWidthMm: 40.0, TabHeightMm: null),
        };
        string archive = await Exported(
            workspace, await Populated(workspace, source), ArchiveProfile.Backup);

        ImportedProject imported = await Importer(workspace)
            .ImportAsync(archive, "Socles larges", Calibration(), CancellationToken.None);

        imported.Diagnostics
            .Where(diagnostic => diagnostic.Kind == ProjectDiagnosticKind.OverrideExceedsPawnWidth)
            .ShouldHaveSingleItem()
            .Message.ShouldContain("40");
    }

    [Fact]
    public async Task APaperFormatThisMachineDoesNotKnowImportsAndIsReported()
    {
        using TempWorkspace workspace = new();
        string archive = await Exported(
            workspace, await Populated(workspace, ProjectSample.Rich()), ArchiveProfile.Backup);

        ImportedProject imported = await Importer(workspace).ImportAsync(
            archive,
            "Autre papier",
            ProjectCalibration.WithPaperFormats("Letter"),
            CancellationToken.None);

        imported.Diagnostics
            .ShouldHaveSingleItem()
            .Kind.ShouldBe(ProjectDiagnosticKind.UnknownPaperFormat);
    }

    // --- MEN-009 : le nom de dossier vient d'une chaîne libre -------------

    [Fact]
    public async Task AHostileNameCannotDecideWhereTheProjectLands()
    {
        // The name is free text, and it can come from whoever sent the archive.
        // It never reaches a path by concatenation: it goes through the
        // transliteration of MEN-009 first, so "../../logs" becomes a folder
        // name and not a way out of the root.
        using TempWorkspace workspace = new();
        string archive = await Exported(
            workspace, await Populated(workspace, ProjectSample.Rich()), ArchiveProfile.Backup);

        ImportedProject imported = await Importer(workspace)
            .ImportAsync(archive, "../../logs", Calibration(), CancellationToken.None);

        Path.GetDirectoryName(imported.Directory).ShouldBe(workspace.Root);
        Path.GetFileName(imported.Directory).ShouldBe("logs");
    }
}
