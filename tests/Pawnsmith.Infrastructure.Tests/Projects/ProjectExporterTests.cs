using System.IO.Compression;

using Pawnsmith.Domain.Projects;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

using Xunit.Abstractions;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// Covers tests 33 and 36 to 40 of C.12: what an archive carries, and what the
/// export refuses.
/// </summary>
public class ProjectExporterTests(ITestOutputHelper output)
{
    private static readonly DateTimeOffset ExportInstant =
        new(2026, 9, 5, 16, 4, 0, TimeSpan.Zero);

    private static ProjectExporter Exporter() => new(new FixedClock(ExportInstant));

    /// <summary>A project folder holding a real file for everything it references.</summary>
    private static string Populated(TempWorkspace workspace, Project project, string name = "donjon")
    {
        string directory = Path.Combine(workspace.Root, name);
        Directory.CreateDirectory(Path.Combine(directory, "images"));
        Directory.CreateDirectory(Path.Combine(directory, "exports"));

        ProjectFileWriter
            .WriteAsync(directory, ProjectJson.Serialize(project.ToDocument()), CancellationToken.None)
            .GetAwaiter().GetResult();

        foreach (string relative in ShareFilter.ReferencedImages(project.ToDocument()))
        {
            File.WriteAllBytes(Path.Combine(directory, relative), [0x89, 0x50, 0x4E, 0x47]);
        }

        File.WriteAllBytes(Path.Combine(directory, "exports", "donjon-moyenne.pdf"), "%PDF-1.7"u8.ToArray());

        return directory;
    }

    private static string Destination(TempWorkspace workspace)
    {
        string directory = Path.Combine(workspace.Root, "archives");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static IReadOnlyList<string> Entries(string archivePath)
    {
        using ZipArchive zip = ZipFile.OpenRead(archivePath);
        return [.. zip.Entries.Select(entry => entry.FullName)];
    }

    private static ProjectDocument ProjectInside(string archivePath)
    {
        using ZipArchive zip = ZipFile.OpenRead(archivePath);
        using Stream stream = zip.GetEntry(ProjectFileWriter.FileName)!.Open();
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);

        return ProjectJson.Deserialize(buffer.ToArray());
    }

    // --- C.12 n° 33 : le profil Backup emporte tout -----------------------

    [Fact]
    public async Task ABackupCarriesTheExportsThePairedImagesAndEveryCandidate()
    {
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich();
        string directory = Populated(workspace, source);

        string archive = await Exporter().ExportAsync(
            directory, ArchiveProfile.Backup, Destination(workspace), CancellationToken.None);

        IReadOnlyList<string> entries = Entries(archive);

        entries.ShouldContain(name => name.EndsWith("-pair.png", StringComparison.Ordinal));
        entries.ShouldContain("exports/donjon-moyenne.pdf");

        ProjectInside(archive).Blueprints[0].Candidates.Count
            .ShouldBe(source.Blueprints[0].Candidates.Count);
    }

    // --- C.12 n° 34 et 35 : le profil Share, côté fichiers ----------------

    [Fact]
    public async Task AShareCarriesNeitherExportsNorPairedImages()
    {
        using TempWorkspace workspace = new();
        string directory = Populated(workspace, ProjectSample.Rich());

        string archive = await Exporter().ExportAsync(
            directory, ArchiveProfile.Share, Destination(workspace), CancellationToken.None);

        IReadOnlyList<string> entries = Entries(archive);

        entries.ShouldNotContain(name => name.StartsWith("exports/", StringComparison.Ordinal));
        entries.ShouldNotContain(name => name.EndsWith("-pair.png", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AShareCarriesNoFileOfARejectedCandidate()
    {
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich();
        string directory = Populated(workspace, source);
        Guid rejected = source.Blueprints[0].Candidates[1].Id;

        string archive = await Exporter().ExportAsync(
            directory, ArchiveProfile.Share, Destination(workspace), CancellationToken.None);

        Entries(archive).ShouldNotContain(name => name.Contains(rejected.ToString(), StringComparison.Ordinal));
    }

    [Fact]
    public async Task EveryFileTheArchivedProjectNamesIsActuallyInTheArchive()
    {
        // The invariant of DEC-050, checked from the archive's own point of
        // view: no dangling reference, in either profile.
        using TempWorkspace workspace = new();
        string directory = Populated(workspace, ProjectSample.Rich());

        foreach (ArchiveProfile profile in Enum.GetValues<ArchiveProfile>())
        {
            string archive = await Exporter().ExportAsync(
                directory, profile, Destination(workspace), CancellationToken.None);

            IReadOnlyList<string> entries = Entries(archive);

            foreach (string referenced in ShareFilter.ReferencedImages(ProjectInside(archive)))
            {
                entries.ShouldContain(referenced);
            }
        }
    }

    // --- C.12 n° 36 : MEN-006, et la liste blanche est exhaustive ---------

    [Fact]
    public async Task NothingOutsideTheWhitelistEverTravels()
    {
        // The test no longer looks for a named secret; it checks that every
        // entry belongs to the list. That is what turns MEN-006 from an
        // exemplary test into an exhaustive one (DEC-050).
        using TempWorkspace workspace = new();
        string directory = Populated(workspace, ProjectSample.Rich());

        File.WriteAllText(Path.Combine(directory, "secret.env"), "API_KEY=hunter2");
        File.WriteAllText(Path.Combine(directory, "notes.txt"), "revoir l'orc");
        Directory.CreateDirectory(Path.Combine(directory, "logs"));
        File.WriteAllText(Path.Combine(directory, "logs", "app.log"), "chemins absolus partout");
        Directory.CreateDirectory(Path.Combine(directory, ".git"));
        File.WriteAllText(Path.Combine(directory, ".git", "config"), "[remote]");
        File.WriteAllBytes(Path.Combine(directory, "images", "orpheline.png"), [0x89, 0x50]);

        foreach (ArchiveProfile profile in Enum.GetValues<ArchiveProfile>())
        {
            string archive = await Exporter().ExportAsync(
                directory, profile, Destination(workspace), CancellationToken.None);

            IReadOnlyList<string> referenced = ShareFilter.ReferencedImages(ProjectInside(archive));

            foreach (string entry in Entries(archive))
            {
                bool allowed = entry == ArchiveManifestFile.EntryName
                    || entry == ProjectFileWriter.FileName
                    || referenced.Contains(entry)
                    || (profile == ArchiveProfile.Backup
                        && entry.StartsWith("exports/", StringComparison.Ordinal)
                        && entry.EndsWith(".pdf", StringComparison.Ordinal));

                allowed.ShouldBeTrue($"'{entry}' is not on the whitelist of C.8.2.");
            }
        }
    }

    [Fact]
    public async Task AnOrphanImageDoesNotTravel()
    {
        // A leftover from a deleted candidate, or a file dropped in by hand. It
        // has no reason to travel, and the whitelist is keyed on what the
        // archive's own project.json names.
        using TempWorkspace workspace = new();
        string directory = Populated(workspace, ProjectSample.Rich());
        File.WriteAllBytes(Path.Combine(directory, "images", "orpheline.png"), [0x89, 0x50]);

        string archive = await Exporter().ExportAsync(
            directory, ArchiveProfile.Backup, Destination(workspace), CancellationToken.None);

        Entries(archive).ShouldNotContain("images/orpheline.png");
    }

    // --- C.12 n° 37 : MEN-008, le lien symbolique -------------------------

    [Fact]
    public async Task ASymbolicLinkMakesTheExportFailAndLeavesNoArchiveBehind()
    {
        using TempWorkspace workspace = new();
        string directory = Populated(workspace, ProjectSample.Rich());
        string destination = Destination(workspace);

        if (!SymbolicLinks.AreSupported)
        {
            // Windows without the SeCreateSymbolicLink privilege. The threat is
            // certified by the Ubuntu CI run; here only the second layer of the
            // same countermeasure is exercised - the whitelist, which lets
            // nothing but referenced .png and .pdf files through. Weaker, and
            // said out loud rather than passing silently.
            output.WriteLine("Symbolic links cannot be created on this machine: MEN-008 is "
                + "covered here by the whitelist only, and for real on the CI run.");

            string archive = await Exporter().ExportAsync(
                directory, ArchiveProfile.Backup, destination, CancellationToken.None);

            Entries(archive).ShouldAllBe(entry =>
                entry == ArchiveManifestFile.EntryName
                || entry == ProjectFileWriter.FileName
                || entry.EndsWith(".png", StringComparison.Ordinal)
                || entry.EndsWith(".pdf", StringComparison.Ordinal));
            return;
        }

        string outside = Path.Combine(workspace.Root, "secrets.txt");
        File.WriteAllText(outside, "API_KEY=hunter2");
        SymbolicLinks.Create(Path.Combine(directory, "images", "innocent.png"), outside);

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Exporter().ExportAsync(directory, ArchiveProfile.Backup, destination, CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.ArchiveExportFailed);
        error.Message.ShouldContain("innocent.png");

        Directory.EnumerateFiles(destination).ShouldBeEmpty();
    }

    // --- C.12 n° 38 : un fichier référencé mais absent --------------------

    [Fact]
    public async Task AReferencedFileMissingFromTheDiskMakesTheExportFailAndNamesIt()
    {
        // Where the diagnostic the load tolerates becomes blocking, and the two
        // are consistent: opening a project with holes is acceptable, handing
        // one out is not.
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich();
        string directory = Populated(workspace, source);
        string missing = source.Blueprints[0].Candidates[0].FrontImageFile!;
        File.Delete(Path.Combine(directory, missing));

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Exporter().ExportAsync(
                directory, ArchiveProfile.Backup, Destination(workspace), CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.ArchiveExportFailed);
        error.Message.ShouldContain(missing);
        Directory.EnumerateFiles(Destination(workspace)).ShouldBeEmpty();
    }

    // --- C.12 n° 39 : la forme de l'archive -------------------------------

    [Fact]
    public async Task EveryEntryIsARelativePosixPathAndArchiveJsonComesFirst()
    {
        using TempWorkspace workspace = new();
        string directory = Populated(workspace, ProjectSample.Rich());

        string archive = await Exporter().ExportAsync(
            directory, ArchiveProfile.Backup, Destination(workspace), CancellationToken.None);

        IReadOnlyList<string> entries = Entries(archive);

        entries[0].ShouldBe(ArchiveManifestFile.EntryName);
        entries.ShouldBeUnique();
        entries.ShouldAllBe(entry => !entry.StartsWith('/'));
        entries.ShouldAllBe(entry => !entry.Contains('\\'));
        entries.ShouldAllBe(entry => !entry.Contains(".."));

        // No enclosing root folder: the destination is chosen at import, not
        // suffered (C.8.5).
        entries.ShouldNotContain(entry => entry.StartsWith("donjon/", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheArchiveIsNamedAfterTheFolderTheProfileAndTheInstant()
    {
        using TempWorkspace workspace = new();
        string directory = Populated(workspace, ProjectSample.Rich());

        string archive = await Exporter().ExportAsync(
            directory, ArchiveProfile.Share, Destination(workspace), CancellationToken.None);

        Path.GetFileName(archive).ShouldBe("donjon-share-202609051604.zip");
    }

    [Fact]
    public async Task TheArchiveOpensWithAnyZipTool()
    {
        // Plain ZIP, no proprietary extension, no password: DEC-011 assumes
        // anyone can open the archive with their own system's tool, ten years
        // from now and without Pawnsmith.
        using TempWorkspace workspace = new();
        string directory = Populated(workspace, ProjectSample.Rich());

        string archive = await Exporter().ExportAsync(
            directory, ArchiveProfile.Backup, Destination(workspace), CancellationToken.None);

        Path.GetExtension(archive).ShouldBe(".zip");
        Should.NotThrow(() => ZipFile.OpenRead(archive).Dispose());
    }

    // --- C.12 n° 40 : pas d'archive dans un dossier de projet -------------

    [Fact]
    public async Task WritingAnArchiveInsideAProjectFolderIsRefused()
    {
        using TempWorkspace workspace = new();
        string directory = Populated(workspace, ProjectSample.Rich());

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Exporter().ExportAsync(
                directory, ArchiveProfile.Backup, directory, CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.ArchiveExportFailed);
    }

    [Fact]
    public async Task WritingAnArchiveDeepInsideAProjectFolderIsRefusedToo()
    {
        // "mon-projet/exports/sauvegardes" is just as much inside a project as
        // "mon-projet" is, so the whole chain of parents is checked. Otherwise
        // the next backup would contain the previous one, then both.
        using TempWorkspace workspace = new();
        string directory = Populated(workspace, ProjectSample.Rich());
        string nested = Path.Combine(directory, "exports", "sauvegardes");
        Directory.CreateDirectory(nested);

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Exporter().ExportAsync(
                directory, ArchiveProfile.Backup, nested, CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.ArchiveExportFailed);
    }
}
