using Pawnsmith.Domain.Projects;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// Covers tests 43 to 49 of C.12: everything an import refuses before it writes
/// anything.
/// </summary>
/// <remarks>
/// <b>None of these tests asserts that no file was written, and that is on
/// purpose.</b> <see cref="ArchiveInspector"/> takes no destination and has no
/// way to create a file, so the property MEN-001 asks for is carried by the
/// shape of the type rather than by an assertion someone could forget to write.
/// What these tests check is that the right archives are refused, and with which
/// code.
/// </remarks>
public class ArchiveInspectorTests
{
    private static ArchiveInspector Inspector(ProjectRepositoryOptions? options = null) =>
        new(options ?? Options());

    private static ProjectRepositoryOptions Options() => new(Path.Combine(Path.GetTempPath(), "projects"));

    /// <summary>Runs the inspection and hands back the refusal it produced.</summary>
    private static ProjectException Refusal(TempWorkspace workspace, ArchiveBuilder builder, ProjectRepositoryOptions? options = null)
    {
        string path = builder.WriteTo(Path.Combine(workspace.Root, "archive.zip"));

        return Should.Throw<ProjectException>(() => Inspector(options).Inspect(path));
    }

    private static ArchiveBuilder Valid() => ArchiveBuilder.Valid(ProjectSample.Rich());

    /// <summary>
    /// A project whose own images differ only in case, so that nothing but the
    /// case-collision rule can refuse the archive built from it.
    /// </summary>
    /// <remarks>
    /// Two orphan entries would have been easier to write and would have tested
    /// nothing: the internal-consistency check of step 6 refuses an unreferenced
    /// image, so it would have caught them whether or not the case rule existed.
    /// Making the project reference both paths takes that shortcut away.
    /// </remarks>
    private static ArchiveBuilder ValidWithCaseCollidingImages()
    {
        ProjectDocument document = ProjectSample.Rich().ToDocument();
        BlueprintDocument blueprint = document.Blueprints[0];
        CandidateDocument candidate = blueprint.Candidates[0] with
        {
            PairedImageFile = "images/a.png",
            FrontImageFile = "images/A.png",
            BackImageFile = null,
        };

        ProjectDocument rewritten = document with
        {
            Blueprints =
            [
                blueprint with { Candidates = [candidate] },
                document.Blueprints[1],
            ],
        };

        return new ArchiveBuilder()
            .With(ArchiveManifestFile.EntryName, ArchiveBuilder.Manifest())
            .With(ProjectFileWriter.FileName, ProjectJson.Serialize(rewritten))
            .With("images/a.png", ArchiveBuilder.Png())
            .With("images/A.png", ArchiveBuilder.Png());
    }

    // --- Le témoin : une archive saine passe ------------------------------

    [Fact]
    public void ASoundArchiveIsAccepted()
    {
        // The control the other tests are read against. Without it, every
        // refusal below could be caused by the fixture rather than by the rule
        // under test, and nothing would say so.
        using TempWorkspace workspace = new();
        string path = Valid().WriteTo(Path.Combine(workspace.Root, "archive.zip"));

        InspectedArchive inspected = Inspector().Inspect(path);

        inspected.Manifest.ArchiveVersion.ShouldBe(1);
        inspected.Project.ProjectId.ShouldBe(ProjectSample.Rich().ProjectId.ToString());
        inspected.EntryNames.ShouldContain(ArchiveManifestFile.EntryName);
        inspected.EntryNames.ShouldContain(ProjectFileWriter.FileName);
    }

    [Fact]
    public async Task AnArchiveTheExporterProducedPassesEveryDefaultBound()
    {
        // The bounds of C.9.3 are arbitrated numbers, and a bound that refuses
        // real output would be discovered by a user rather than by a test. The
        // compression ratio is the one at risk: project.json is repetitive JSON,
        // and it compresses well.
        using TempWorkspace workspace = new();
        Project source = ProjectSample.Rich();
        string directory = Path.Combine(workspace.Root, "donjon");
        Directory.CreateDirectory(Path.Combine(directory, "images"));

        await ProjectFileWriter.WriteAsync(
            directory, ProjectJson.Serialize(source.ToDocument()), CancellationToken.None);

        foreach (string relative in ShareFilter.ReferencedImages(source.ToDocument()))
        {
            File.WriteAllBytes(Path.Combine(directory, relative), ArchiveBuilder.Png());
        }

        string destination = Path.Combine(workspace.Root, "archives");
        string archive = await new ProjectExporter().ExportAsync(
            directory, ArchiveProfile.Backup, destination, CancellationToken.None);

        InspectedArchive inspected = Inspector().Inspect(archive);

        inspected.Project.Blueprints.Count.ShouldBe(source.Blueprints.Count);
    }

    [Fact]
    public void ADecoyFooterInsideAFileDoesNotHideAnEncryptedEntry()
    {
        // The four bytes marking the end of a ZIP's directory can perfectly well
        // occur inside a file the archive carries — a PNG is arbitrary bytes.
        // The reader looks for that footer from the end of the file backwards
        // for exactly this reason, and this is the archive that tells the two
        // apart: a decoy planted in an image, followed by the zeroes a forward
        // scan would read as "this archive has no entries at all". Read
        // forwards, the encrypted entries below become invisible and the archive
        // is waved through.
        //
        // Marking the entries encrypted is what gives the test something to
        // lose. A sound archive with a decoy in it passes either way, so it
        // would have proved nothing.
        using TempWorkspace workspace = new();
        string image = ShareFilter.ReferencedImages(ProjectSample.Rich().ToDocument())[0];
        byte[] decoy = [.. ArchiveBuilder.Png(), 0x50, 0x4B, 0x05, 0x06, .. new byte[32]];

        string path = Valid()
            .Without(name => name == image)
            .WithStored(image, decoy)
            .WriteTo(Path.Combine(workspace.Root, "archive.zip"));

        ArchiveBuilder.MarkEveryEntryEncrypted(path);

        Should.Throw<ProjectException>(() => Inspector().Inspect(path))
            .Message.ShouldContain("is encrypted");
    }

    // --- C.12 n° 43 : MEN-001, la traversée de chemin ---------------------

    [Fact]
    public void AnEntryClimbingOutOfTheArchiveIsRefused()
    {
        using TempWorkspace workspace = new();

        ProjectException refusal = Refusal(workspace, Valid().With("../../evil.txt", "owned"));

        refusal.Code.ShouldBe(ProjectErrorCode.ArchiveRejected);
        refusal.WireCode.ShouldBe("ARCHIVE_REJECTED");

        // Asserted on the message as well as the code, and not out of zeal. The
        // whitelist would refuse this name too, so a test that checked only the
        // code would still pass with the path rule deleted — and would then be
        // guarding the whitelist while claiming to guard MEN-001.
        refusal.Message.ShouldContain("not a plain relative path");
    }

    // --- C.12 n° 44 : chemin absolu, lettre de lecteur, antislash ---------

    [Theory]
    [InlineData("/etc/passwd")]
    [InlineData("C:/images/a.png")]
    [InlineData("images\\a.png")]
    [InlineData("images/./a.png")]
    public void AnEntryThatIsNotAPlainRelativePathIsRefused(string name)
    {
        using TempWorkspace workspace = new();

        ProjectException refusal = Refusal(workspace, Valid().With(name, ArchiveBuilder.Png()));

        refusal.Code.ShouldBe(ProjectErrorCode.ArchiveRejected);
        refusal.Message.ShouldContain("not a plain relative path");
    }

    // --- C.12 n° 45 : le doublon et la collision de casse -----------------

    [Fact]
    public void TwoEntriesOfTheSameNameAreRefused()
    {
        // The classic bypass: the validator inspects the first entry and the
        // extractor writes the second.
        using TempWorkspace workspace = new();
        string image = ShareFilter.ReferencedImages(ProjectSample.Rich().ToDocument())[0];

        ProjectException refusal = Refusal(workspace, Valid().With(image, ArchiveBuilder.Png()));

        refusal.Code.ShouldBe(ProjectErrorCode.ArchiveRejected);

        // The message is asserted, not only the code, and the reason is worth
        // knowing. An exact duplicate is also a case-insensitive one, so the
        // case rule alone would refuse this archive and the exact rule would
        // look load-bearing while being dead. What it really carries is the
        // message: the log has to say "twice", not "differs in case", because
        // the two send whoever reads it looking for different things.
        refusal.Message.ShouldContain("appears twice");
    }

    [Fact]
    public void TwoEntriesDifferingOnlyInCaseAreRefused()
    {
        // Harmless on Linux, a silent overwrite on Windows and macOS. Refused
        // everywhere, because an archive must not mean two things on two
        // machines.
        using TempWorkspace workspace = new();

        ProjectException refusal = Refusal(workspace, ValidWithCaseCollidingImages());

        refusal.Code.ShouldBe(ProjectErrorCode.ArchiveRejected);
        refusal.Message.ShouldContain("differs only in case");
    }

    // --- C.12 n° 46 : l'entrée chiffrée -----------------------------------

    [Fact]
    public void AnEncryptedEntryIsRefused()
    {
        // The framework is no help at all here: it never reads this bit, and it
        // hands over the bytes of an "encrypted" entry without a word. Left
        // unchecked, such an archive would be extracted into a project folder as
        // ciphertext, silently.
        using TempWorkspace workspace = new();
        string path = Valid().WriteTo(Path.Combine(workspace.Root, "archive.zip"));

        ArchiveBuilder.MarkEveryEntryEncrypted(path);

        Should.Throw<ProjectException>(() => Inspector().Inspect(path))
            .Code.ShouldBe(ProjectErrorCode.ArchiveRejected);
    }

    // --- C.12 n° 47 : les bornes de ressources ----------------------------

    [Fact]
    public void TooManyEntriesIsRefusedWithItsOwnCode()
    {
        using TempWorkspace workspace = new();
        ProjectRepositoryOptions options = Options() with { MaxArchiveEntryCount = 3 };

        Refusal(workspace, Valid(), options)
            .Code.ShouldBe(ProjectErrorCode.ArchiveLimitExceeded);
    }

    [Fact]
    public void TooLargeUncompressedIsRefusedWithItsOwnCode()
    {
        using TempWorkspace workspace = new();
        ProjectRepositoryOptions options = Options() with { MaxArchiveUncompressedBytes = 16 };

        Refusal(workspace, Valid(), options)
            .Code.ShouldBe(ProjectErrorCode.ArchiveLimitExceeded);
    }

    [Fact]
    public void ADecompressionBombIsRefusedByTheDefaultRatio()
    {
        // The one bound tested at its real value rather than at a lowered one,
        // because it is the bound that exists for an attack: a hundred kilobytes
        // of zeroes compress to a few dozen bytes, and nothing an honest project
        // holds does that.
        using TempWorkspace workspace = new();

        Refusal(workspace, Valid().With("images/bomb.png", new byte[100_000]))
            .Code.ShouldBe(ProjectErrorCode.ArchiveLimitExceeded);
    }

    [Fact]
    public void APathDeeperThanTheBoundIsRefusedWithItsOwnCode()
    {
        using TempWorkspace workspace = new();

        Refusal(workspace, Valid().With("images/deep/deeper/a.png", ArchiveBuilder.Png()))
            .Code.ShouldBe(ProjectErrorCode.ArchiveLimitExceeded);
    }

    [Fact]
    public void APathLongerThanTheBoundIsRefusedWithItsOwnCode()
    {
        using TempWorkspace workspace = new();
        string name = "images/" + new string('a', 300) + ".png";

        Refusal(workspace, Valid().With(name, ArchiveBuilder.Png()))
            .Code.ShouldBe(ProjectErrorCode.ArchiveLimitExceeded);
    }

    // --- C.12 n° 48 : le schéma trop récent, avant toute extraction -------

    [Fact]
    public void ASchemaNewerThanThisBuildIsRefusedByItsOwnCode()
    {
        // C.9.2 names this code specifically: an archive carrying a newer schema
        // is refused before extraction, and not behind the opaque
        // ARCHIVE_REJECTED. A project file inside an archive is still a project
        // file.
        using TempWorkspace workspace = new();
        ProjectDocument tooRecent = ProjectSample.Rich().ToDocument() with { VersionSchema = 2 };

        ProjectException refusal = Refusal(
            workspace,
            Valid().Without(name => name == ProjectFileWriter.FileName)
                .With(ProjectFileWriter.FileName, ProjectJson.Serialize(tooRecent)));

        refusal.Code.ShouldBe(ProjectErrorCode.SchemaTooRecent);
        refusal.WireCode.ShouldBe("PROJECT_SCHEMA_TOO_RECENT");
    }

    // --- C.12 n° 49 : la cohérence interne, dans les deux sens ------------

    [Fact]
    public void AReferencedFileMissingFromTheArchiveIsRefused()
    {
        using TempWorkspace workspace = new();
        string image = ShareFilter.ReferencedImages(ProjectSample.Rich().ToDocument())[0];

        ProjectException refusal = Refusal(workspace, Valid().Without(name => name == image));

        refusal.Code.ShouldBe(ProjectErrorCode.ArchiveRejected);
    }

    [Fact]
    public void AFileNobodyReferencesIsRefused()
    {
        // The other direction, and it is what stops an archive being used to drop
        // a chosen file into somebody's project folder.
        using TempWorkspace workspace = new();

        Refusal(workspace, Valid().With("images/orphan.png", ArchiveBuilder.Png()))
            .Code.ShouldBe(ProjectErrorCode.ArchiveRejected);
    }

    // --- La liste blanche de C.8.2, vue depuis l'import -------------------

    [Theory]
    [InlineData("secret.env")]
    [InlineData("logs/pawnsmith.log")]
    [InlineData("notes.txt")]
    [InlineData("images/a.txt")]
    [InlineData("exports/planche.png")]
    [InlineData("images/deep/a.png")]
    [InlineData("images/a.png/extra")]
    public void AnEntryOutsideTheWhitelistIsRefused(string name)
    {
        // The import-side twin of test 36. The export honours the whitelist by
        // construction, since it enumerates what it adds; the import has to ask
        // the question one name at a time.
        using TempWorkspace workspace = new();

        ProjectException refusal = Refusal(workspace, Valid().With(name, "whatever"));

        refusal.Code.ShouldBe(ProjectErrorCode.ArchiveRejected);

        // The message again, and for a reason worth naming: "images/a.txt" is an
        // unreferenced file under images/, so step 6 would refuse it even if the
        // whitelist stopped looking at extensions altogether. Without this line
        // the case would guard the consistency check while claiming the
        // whitelist.
        refusal.Message.ShouldContain("not one an archive may hold");
    }

    [Fact]
    public void AProjectFileLargerThanTheBoundIsRefusedBeforeItIsRead()
    {
        // The bound has to come before the read, because the file goes into
        // memory whole - the same reasoning as C.7.1 step 2, and the same bound.
        // Here it is the announced size that is checked, so nothing is
        // decompressed at all.
        using TempWorkspace workspace = new();
        ProjectRepositoryOptions options = Options() with { MaxProjectFileBytes = 32 };

        ProjectException refusal = Refusal(workspace, Valid(), options);

        refusal.Code.ShouldBe(ProjectErrorCode.TooLarge);
        refusal.WireCode.ShouldBe("PROJECT_TOO_LARGE");
    }

    [Fact]
    public void ASymbolicLinkEntryIsRefused()
    {
        // MEN-008 has a way in as well as a way out: a link inside an archive is
        // a file pointing somewhere it was never given permission to point.
        //
        // The link takes the name of an image the project genuinely references,
        // rather than a name of its own. An extra entry would have been simpler
        // and would have proved nothing: the consistency check of step 6 refuses
        // an unreferenced image, so it would have caught the link whether or not
        // the entry-type rule existed.
        using TempWorkspace workspace = new();
        string image = ShareFilter.ReferencedImages(ProjectSample.Rich().ToDocument())[0];

        ProjectException refusal = Refusal(
            workspace,
            Valid().Without(name => name == image)
                .With(image, "/etc/passwd"u8.ToArray(), ArchiveBuilder.SymbolicLinkAttributes));

        refusal.Code.ShouldBe(ProjectErrorCode.ArchiveRejected);
        refusal.Message.ShouldContain("not an ordinary file");
    }

    [Fact]
    public void ADirectoryEntryIsRefused()
    {
        using TempWorkspace workspace = new();

        ProjectException refusal = Refusal(workspace, Valid().With("images/", []));

        refusal.Code.ShouldBe(ProjectErrorCode.ArchiveRejected);
        refusal.Message.ShouldContain("not an ordinary file");
    }

    // --- archive.json et project.json --------------------------------------

    [Fact]
    public void AnArchiveWithNoManifestIsRefused()
    {
        using TempWorkspace workspace = new();

        Refusal(workspace, Valid().Without(name => name == ArchiveManifestFile.EntryName))
            .Code.ShouldBe(ProjectErrorCode.ArchiveRejected);
    }

    [Fact]
    public void AnArchiveVersionThisBuildDoesNotReadIsRefused()
    {
        using TempWorkspace workspace = new();

        Refusal(
            workspace,
            Valid().Without(name => name == ArchiveManifestFile.EntryName)
                .With(ArchiveManifestFile.EntryName, ArchiveBuilder.Manifest(archiveVersion: 2)))
            .Code.ShouldBe(ProjectErrorCode.ArchiveRejected);
    }

    [Fact]
    public void AnArchiveWithNoProjectFileIsRefused()
    {
        using TempWorkspace workspace = new();

        Refusal(workspace, Valid().Without(name => name == ProjectFileWriter.FileName))
            .Code.ShouldBe(ProjectErrorCode.ArchiveRejected);
    }

    [Fact]
    public void AProjectFileTheArchiveCannotOfferIsRefusedByItsFieldName()
    {
        // PROJECT_INVALID rather than ARCHIVE_REJECTED, and the message names the
        // field: the reticence of ARCHIVE_REJECTED is about the shape of the
        // archive, and a malformed project inside a well-formed archive is nearly
        // always one's own.
        using TempWorkspace workspace = new();

        ProjectException refusal = Refusal(
            workspace,
            Valid().Without(name => name == ProjectFileWriter.FileName)
                .With(ProjectFileWriter.FileName, """{"versionSchema": 1, "surprise": true}"""));

        refusal.Code.ShouldBe(ProjectErrorCode.Invalid);
        refusal.Message.ShouldContain("surprise");
    }

    [Fact]
    public void AFileThatIsNotAZipIsRefused()
    {
        using TempWorkspace workspace = new();
        string path = workspace.WriteFile("archive.zip", "this is not a zip file at all");

        Should.Throw<ProjectException>(() => Inspector().Inspect(path))
            .Code.ShouldBe(ProjectErrorCode.ArchiveRejected);
    }

    [Fact]
    public void AnArchiveThatIsNotThereIsReportedAsMissing()
    {
        using TempWorkspace workspace = new();

        Should.Throw<ProjectException>(
            () => Inspector().Inspect(Path.Combine(workspace.Root, "nowhere.zip")))
            .Code.ShouldBe(ProjectErrorCode.NotFound);
    }
}
