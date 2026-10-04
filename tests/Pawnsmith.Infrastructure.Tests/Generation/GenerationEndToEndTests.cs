using Pawnsmith.Application.Generation;
using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.Jobs;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;
using Pawnsmith.Infrastructure.Cutout;
using Pawnsmith.Infrastructure.Generation;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Generation;

/// <summary>
/// Covers test 42 of E.12: a batch from end to end, against the fake ComfyUI
/// and the real repository on disk.
/// </summary>
/// <remarks>
/// Every piece is tested on its own elsewhere. What only exists here is the
/// seam: that the image the server sent is the file on disk, byte for byte;
/// that the path the repository chose reads back through the reader's own
/// path rules; and that the prompt the server received is the one the
/// candidate froze, once the whole chain — use case, substitution, HTTP,
/// JSON — has run.
/// </remarks>
public class GenerationEndToEndTests
{
    private static readonly UniformBackgroundRemover Remover = new(new CutoutOptions());

    /// <summary>A distinct paired scene per call.</summary>
    private static byte[] Scene(int index) =>
        TestScene.PairPng(new Rgb((byte)(150 + index), 40, 40), new Rgb(40, 60, (byte)(150 + index)));

    private static readonly DateTimeOffset Instant = new(2026, 10, 3, 11, 42, 17, TimeSpan.Zero);

    private static Calibration Calibration() => ProjectCalibration.WithPaperFormats("A4");

    private static async Task<(IProjectRepository Repository, string Directory, Guid BlueprintId)> ProjectWithOneBlueprint(TempWorkspace workspace)
    {
        IProjectRepository repository = new FileSystemProjectRepository(
            new ProjectRepositoryOptions(workspace.Root),
            new FixedClock(Instant));

        CreatedProjectResult created = await repository.CreateAsync(
            "Donjon", Universe.Fantasy, Geometry.TabAndSocket, "A4", CancellationToken.None);

        var blueprintId = Guid.NewGuid();

        Project project = created.Project with
        {
            Style = new Style("Ink", "ink outlines, \"muted\" watercolour", "photorealistic", "earth tones"),
            Blueprints =
            [
                new Blueprint(
                    Id: blueprintId,
                    Race: "goblin",
                    CharacterClass: "skirmisher",
                    Size: Size.Medium,
                    OptionalParameters: new Dictionary<string, string>(StringComparer.Ordinal),
                    Details: string.Empty,
                    SubjectClause: "a goblin skirmisher, déjà vu, holding a sign that reads {{SEED}}",
                    Quantity: 6,
                    Candidates: [],
                    ElectedCandidateId: null),
            ],
        };

        await repository.SaveAsync(created.Directory, project, CancellationToken.None);

        return (repository, created.Directory, blueprintId);
    }

    [Fact]
    public async Task ABatchRunsFromTheUseCaseToTheDiskAndBack()
    {
        using TempWorkspace workspace = new();
        (IProjectRepository repository, string directory, Guid blueprintId) = await ProjectWithOneBlueprint(workspace);

        // Real paired scenes, so that the real cut-out has a figure to find (T5).
        await using var comfy = new FakeComfyUi { Image = Scene };

        WorkflowTemplate workflow = WorkflowTemplateReader.Parse(WorkflowFixture.File(), "workflow.test.json");
        using var generator = new ComfyUiImageGenerator(
            new ComfyUiOptions(comfy.BaseAddress) { PollInterval = TimeSpan.FromMilliseconds(10) },
            workflow);

        var batch = new CandidateGeneration(generator, Remover, repository, new GenerationOptions(), new FixedClock(Instant));

        Job job = await batch.RunAsync(
            new GenerationBatch(directory, blueprintId, [7UL, 8UL], generator.FramingClause, Calibration()),
            onChange: null,
            CancellationToken.None);

        job.State.ShouldBe(JobState.Completed);

        // project.json reads back through the real reader, path rules included.
        LoadedProjectResult reloaded = await repository.LoadAsync(directory, Calibration(), CancellationToken.None);
        Blueprint blueprint = reloaded.Project.Blueprints.Single();
        blueprint.Candidates.Select(candidate => candidate.Seed).ShouldBe([7UL, 8UL]);
        reloaded.Diagnostics.ShouldBeEmpty();

        for (int index = 0; index < 2; index++)
        {
            Candidate candidate = blueprint.Candidates[index];

            // The file on disk is the image the server sent, byte for byte,
            // under the name Pawnsmith chose.
            candidate.PairedImageFile.ShouldBe($"images/{candidate.Id}-pair.png");
            byte[] onDisk = await File.ReadAllBytesAsync(Path.Combine(directory, "images", $"{candidate.Id}-pair.png"));
            onDisk.ShouldBe(Scene(index));

            // And it was cut out on the way (T5, DEC-101): both views exist
            // and the candidate references them.
            candidate.FrontImageFile.ShouldBe($"images/{candidate.Id}-front.png");
            candidate.BackImageFile.ShouldBe($"images/{candidate.Id}-back.png");
            File.Exists(Path.Combine(directory, "images", $"{candidate.Id}-back.png")).ShouldBeTrue();

            // What the server received is what the candidate froze (DEC-049) -
            // literal {{SEED}}, quotes and accents included.
            WorkflowFixture.Input(comfy.Submitted[index], "4", "text")!.GetValue<string>().ShouldBe(
                ResolvedPrompt.From(candidate.FramingClauseUsed, candidate.SubjectClauseUsed, candidate.StyleClauseUsed));
            WorkflowFixture.Input(comfy.Submitted[index], "7", "seed")!.GetValue<ulong>().ShouldBe(candidate.Seed);

            Misalignment.Of(candidate, blueprint, reloaded.Project.Style, generator.FramingClause).ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task AFailedBatchLeavesAProjectThatStillLoadsWithWhatWasProduced()
    {
        using TempWorkspace workspace = new();
        (IProjectRepository repository, string directory, Guid blueprintId) = await ProjectWithOneBlueprint(workspace);

        await using var comfy = new FakeComfyUi
        {
            Outcome = index => index == 1 ? FakeOutcome.ExecutionError : FakeOutcome.Success,
        };

        using var generator = new ComfyUiImageGenerator(
            new ComfyUiOptions(comfy.BaseAddress) { PollInterval = TimeSpan.FromMilliseconds(10) },
            WorkflowTemplateReader.Parse(WorkflowFixture.File(), "workflow.test.json"));

        Job job = await new CandidateGeneration(generator, Remover, repository, new GenerationOptions()).RunAsync(
            new GenerationBatch(directory, blueprintId, [7UL, 8UL, 9UL], generator.FramingClause, Calibration()),
            onChange: null,
            CancellationToken.None);

        job.State.ShouldBe(JobState.Failed);
        job.Failure!.Code.ShouldBe("GENERATOR_FAILED");

        LoadedProjectResult reloaded = await repository.LoadAsync(directory, Calibration(), CancellationToken.None);
        reloaded.Project.Blueprints.Single().Candidates.Select(candidate => candidate.Seed).ShouldBe([7UL]);
        Directory.GetFiles(Path.Combine(directory, "images")).Length.ShouldBe(1);
    }
}
