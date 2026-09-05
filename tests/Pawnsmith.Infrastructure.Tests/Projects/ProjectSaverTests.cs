using Pawnsmith.Domain.Projects;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Projects;

/// <summary>
/// Covers test 12 of C.12 and the order of operations of C.7.3: nothing is
/// written until the project is worth writing.
/// </summary>
public class ProjectSaverTests
{
    private static readonly DateTimeOffset SaveInstant =
        new(2026, 9, 5, 11, 30, 0, TimeSpan.Zero);

    private static ProjectSaver Saver() => new(new FixedClock(SaveInstant));

    private static string EmptyProjectFolder(TempWorkspace workspace, string name = "donjon")
    {
        string directory = Path.Combine(workspace.Root, name);
        Directory.CreateDirectory(Path.Combine(directory, "images"));
        return directory;
    }

    private static async Task<Project> Reload(TempWorkspace workspace, string directory)
    {
        ProjectReader reader = new(new ProjectRepositoryOptions(workspace.Root));

        return (await reader.LoadAsync(
            directory,
            ProjectCalibration.WithPaperFormats("A4"),
            CancellationToken.None)).Project;
    }

    // --- Le cas ordinaire ---------------------------------------------------

    [Fact]
    public async Task SavingWritesAFileThatLoadsBackIdentically()
    {
        using TempWorkspace workspace = new();
        string directory = EmptyProjectFolder(workspace);
        Project source = ProjectSample.Rich();

        Project written = await Saver().SaveAsync(directory, source, CancellationToken.None);
        Project reloaded = await Reload(workspace, directory);

        reloaded.ProjectId.ShouldBe(source.ProjectId);
        reloaded.Name.ShouldBe(source.Name);
        reloaded.Style.ShouldBe(source.Style);
        reloaded.CalibrationOverrides.ShouldBe(source.CalibrationOverrides);
        reloaded.Blueprints.Count.ShouldBe(source.Blueprints.Count);
        reloaded.ModifiedAt.ShouldBe(written.ModifiedAt);
    }

    [Fact]
    public async Task SavingStampsModifiedAtAndLeavesCreatedAtAlone()
    {
        using TempWorkspace workspace = new();
        string directory = EmptyProjectFolder(workspace);
        Project source = ProjectSample.Rich();

        Project written = await Saver().SaveAsync(directory, source, CancellationToken.None);

        written.ModifiedAt.ShouldBe(SaveInstant);
        written.CreatedAt.ShouldBe(source.CreatedAt);
        (await Reload(workspace, directory)).ModifiedAt.ShouldBe(SaveInstant);
    }

    [Fact]
    public async Task TheReturnedProjectMatchesTheFileToTheSecond()
    {
        // The instant is truncated to the second on purpose: that is the
        // precision the file records. Without it the project in memory would
        // carry milliseconds the file does not, and the very next comparison
        // between the two would disagree for a reason nobody could see.
        using TempWorkspace workspace = new();
        string directory = EmptyProjectFolder(workspace);
        FixedClock clock = new(SaveInstant.AddMilliseconds(457));

        Project written = await new ProjectSaver(clock)
            .SaveAsync(directory, ProjectSample.Rich(), CancellationToken.None);

        written.ModifiedAt.ShouldBe(SaveInstant);
        (await Reload(workspace, directory)).ModifiedAt.ShouldBe(written.ModifiedAt);
    }

    // --- C.12 n° 12 : une surcharge fausse est refusée à la sauvegarde ----

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.5)]
    [InlineData(double.NaN)]
    public async Task AnIntrinsicallyWrongOverrideIsRefusedBeforeAnythingIsWritten(double value)
    {
        using TempWorkspace workspace = new();
        string directory = EmptyProjectFolder(workspace);
        Project broken = ProjectSample.Rich() with
        {
            CalibrationOverrides = new CalibrationOverrides(value, null),
        };

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Saver().SaveAsync(directory, broken, CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.OverrideInvalid);
        error.WireCode.ShouldBe("PROJECT_OVERRIDE_INVALID");
        error.Message.ShouldContain("tabWidthMm");

        // Nothing reached the disk, not even a staged file.
        File.Exists(Path.Combine(directory, ProjectFileWriter.FileName)).ShouldBeFalse();
        File.Exists(Path.Combine(directory, ProjectFileWriter.TemporaryFileName)).ShouldBeFalse();
    }

    [Fact]
    public async Task AnInvalidProjectNeverOverwritesAValidFile()
    {
        // The reason validation comes first in C.7.3: the project folder is the
        // user's only copy, so a refused save has to leave the previous one
        // exactly where it was.
        using TempWorkspace workspace = new();
        string directory = EmptyProjectFolder(workspace);
        Project good = ProjectSample.Rich();

        await Saver().SaveAsync(directory, good, CancellationToken.None);
        byte[] before = await File.ReadAllBytesAsync(Path.Combine(directory, ProjectFileWriter.FileName));

        Project broken = good with { Blueprints = [good.Blueprints[0] with { Quantity = 0 }] };

        await Should.ThrowAsync<ProjectException>(
            () => Saver().SaveAsync(directory, broken, CancellationToken.None));

        (await File.ReadAllBytesAsync(Path.Combine(directory, ProjectFileWriter.FileName)))
            .ShouldBe(before);
    }

    [Fact]
    public async Task AnElectedCandidateOfAnotherBlueprintIsRefusedAtSaveToo()
    {
        using TempWorkspace workspace = new();
        string directory = EmptyProjectFolder(workspace);
        Project source = ProjectSample.Rich();
        Guid foreignCandidate = source.Blueprints[0].Candidates[0].Id;

        Project broken = source with
        {
            Blueprints = [source.Blueprints[1] with { ElectedCandidateId = foreignCandidate }],
        };

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Saver().SaveAsync(directory, broken, CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.Invalid);
        error.Message.ShouldContain("electedCandidateId");
    }

    // --- C.7.3 étape 2 : les clauses stockées sont déjà normalisées -------

    [Theory]
    [InlineData("  une clause avec des bords ")]
    [InlineData("une clause\r\navec un retour Windows")]
    [InlineData("une clause avec un saut final\n")]
    public async Task AClauseThatIsNotNormalisedIsRefused(string clause)
    {
        // Checked rather than repaired. Normalising silently on the way out
        // would make the file disagree with the project still held in memory,
        // and the next misalignment calculation would compare a normalised
        // stored clause against an unnormalised current one - a phantom
        // misalignment, on the one mechanism the product relies on for safety.
        using TempWorkspace workspace = new();
        string directory = EmptyProjectFolder(workspace);
        Project source = ProjectSample.Rich();
        Project broken = source with { Style = source.Style with { StyleClause = clause } };

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Saver().SaveAsync(directory, broken, CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.Invalid);
        error.Message.ShouldContain("style.styleClause");
    }

    [Fact]
    public async Task AnUnnormalisedFrozenClauseOnACandidateIsRefusedAndNamed()
    {
        using TempWorkspace workspace = new();
        string directory = EmptyProjectFolder(workspace);
        Project source = ProjectSample.Rich();
        Blueprint blueprint = source.Blueprints[0];
        Candidate broken = blueprint.Candidates[0] with
        {
            StyleClauseUsed = blueprint.Candidates[0].StyleClauseUsed + "  ",
        };

        Project project = source with
        {
            Blueprints = [blueprint with { Candidates = [broken], ElectedCandidateId = broken.Id }],
        };

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Saver().SaveAsync(directory, project, CancellationToken.None));

        error.Message.ShouldContain("blueprints[0].candidates[0].styleClauseUsed");
    }

    [Fact]
    public async Task AnEmptyClauseIsNormalisedAndThereforeAccepted()
    {
        // Normalize is idempotent and an empty string is its own canonical form,
        // so an empty style clause must not be caught by the check.
        using TempWorkspace workspace = new();
        string directory = EmptyProjectFolder(workspace);
        Project source = ProjectSample.Rich();

        await Should.NotThrowAsync(() => Saver().SaveAsync(
            directory,
            source with { Style = source.Style with { StyleClause = string.Empty } },
            CancellationToken.None));
    }

    // --- Le dossier -------------------------------------------------------

    [Fact]
    public async Task SavingIntoAFolderThatDoesNotExistIsRefused()
    {
        // Saving writes a file, never a folder. Creating the folder here would
        // make a typo in a path produce a silently empty project somewhere
        // nobody expects.
        using TempWorkspace workspace = new();

        ProjectException error = await Should.ThrowAsync<ProjectException>(
            () => Saver().SaveAsync(
                Path.Combine(workspace.Root, "jamais-cree"),
                ProjectSample.Rich(),
                CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.NotFound);
    }

    [Fact]
    public async Task TwoSavesOfAnUnchangedProjectDifferOnlyByModifiedAt()
    {
        // Test 19 seen from the save side: everything else in the file has to be
        // byte-identical, so that a git diff of a project shows what changed.
        using TempWorkspace workspace = new();
        string directory = EmptyProjectFolder(workspace);
        string path = Path.Combine(directory, ProjectFileWriter.FileName);
        Project source = ProjectSample.Rich();

        await Saver().SaveAsync(directory, source, CancellationToken.None);
        string first = await File.ReadAllTextAsync(path);

        await new ProjectSaver(new FixedClock(SaveInstant.AddHours(3)))
            .SaveAsync(directory, source, CancellationToken.None);
        string second = await File.ReadAllTextAsync(path);

        first.ShouldNotBe(second);
        Strip(first).ShouldBe(Strip(second));

        static string Strip(string json) => string.Join(
            '\n',
            json.Split('\n').Where(line => !line.Contains("\"modifiedAt\"", StringComparison.Ordinal)));
    }
}
