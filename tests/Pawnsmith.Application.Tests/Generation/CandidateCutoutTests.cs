using Pawnsmith.Application.Blueprints;
using Pawnsmith.Application.Generation;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Projects;
using Pawnsmith.Application.Tests.Fixtures;
using Pawnsmith.Domain.Jobs;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Tests.Generation;

/// <summary>
/// The cut-out in the batch and on demand (DEC-101). Covers tests 18 to 21 of F.9.
/// </summary>
public class CandidateCutoutTests
{
    private const string Directory = "/projects/donjon";

    private static readonly Guid Existing = new("6a1d0c2e-93b4-4f57-8e21-0c4d5b6a7f80");
    private static readonly byte[] PairedBytes = [0x89, (byte)'P', 42];

    private readonly ScriptedImageGenerator generator = new();
    private readonly ScriptedBackgroundRemover remover = new();
    private readonly InMemoryProjectRepository repository;

    public CandidateCutoutTests()
    {
        // One candidate generated before T5: a paired image, no cut-outs,
        // Valid, and elected... which a real project cannot be without
        // cut-outs; here, to show that the election does not move.
        Blueprint blueprint = ProjectFixture.Blueprint(
            candidates: [ProjectFixture.Candidate(id: Existing, status: CandidateStatus.Valid, cutOut: false)]);

        repository = new InMemoryProjectRepository(ProjectFixture.Project(blueprints: [blueprint]));
        repository.Images.Add(($"images/{Existing}-pair.png", PairedBytes));
    }

    private Candidate Stored(Guid id) =>
        repository.Project.Blueprints.Single().Candidates.Single(candidate => candidate.Id == id);

    private CandidateCutout OnDemand() => new(repository, remover, new ProjectWriteGate());

    private Task<Job> Batch(params ulong[] seeds) =>
        new CandidateGeneration(generator, remover, repository, new GenerationOptions()).RunAsync(
            new GenerationBatch(Directory, ProjectFixture.BlueprintId, seeds, "front view on the left", CalibrationFixture.Calibration()),
            onChange: null,
            CancellationToken.None);

    // --- F.9 n° 18 : dans le lot, le candidat sort détouré et élisible ---------------------------

    [Fact]
    public async Task ABatchCandidateComesOutWithItsThreeFilesAndCanBeElected()
    {
        Job job = await Batch(11UL);

        Guid produced = job.Produced.Single();
        Candidate candidate = Stored(produced);
        candidate.FrontImageFile.ShouldBe($"images/{produced}-front.png");

        // The cut-outs written are those of this very image.
        repository.Images.Single(image => image.Path == candidate.FrontImageFile).Png
            .ShouldBe(ScriptedBackgroundRemover.CutoutsOf(ScriptedImageGenerator.ImageFor(0).Png).FrontPng);

        // DEC-071: with both cut-outs, it can be elected.
        Should.NotThrow(() => CandidateElection.Elect(repository.Project, ProjectFixture.BlueprintId, produced));
    }

    // --- F.9 n° 19 : un détourage raté n'arrête pas le lot -------------------------------------------

    [Fact]
    public async Task AFailedCutOutLeavesTheCandidateWithoutCutOutsIsRecordedAndTheBatchGoesOn()
    {
        remover.Script = (call, paired) => call == 1
            ? ScriptedBackgroundRemover.Refusing(CutoutErrorCode.BackgroundNotUniform)(call, paired)
            : Task.FromResult(ScriptedBackgroundRemover.CutoutsOf(paired));

        Job job = await Batch(11UL, 22UL, 33UL);

        job.State.ShouldBe(JobState.Completed);
        job.Produced.Count.ShouldBe(3);

        CutoutFailure failure = job.CutoutFailures.ShouldHaveSingleItem();
        failure.CandidateId.ShouldBe(job.Produced[1]);
        failure.Code.ShouldBe("CUTOUT_BACKGROUND_NOT_UNIFORM");

        Candidate second = Stored(job.Produced[1]);
        second.PairedImageFile.ShouldNotBeNull();
        second.FrontImageFile.ShouldBeNull();
        second.BackImageFile.ShouldBeNull();
        Stored(job.Produced[2]).FrontImageFile.ShouldNotBeNull();
    }

    [Fact]
    public async Task AnUnexpectedExceptionInTheCutOutIsADefectAndEndsTheBatch()
    {
        // Only a coded refusal is a failed cut-out; anything else is a bug.
        remover.Script = (_, _) => Task.FromException<CutoutPair>(new InvalidOperationException("bug"));

        Job job = await Batch(11UL, 22UL);

        job.State.ShouldBe(JobState.Failed);
        job.Failure!.Code.ShouldBe(CandidateGeneration.UnexpectedErrorCode);
    }

    // --- F.9 n° 20 : à la demande ---------------------------------------------------------------------

    [Fact]
    public async Task AnExistingCandidateIsCutOutOnDemandAndNothingElseMoves()
    {
        Candidate before = Stored(Existing);

        EditedProject edited = await OnDemand().CutOutAsync(
            Directory, CalibrationFixture.Calibration(), ProjectFixture.BlueprintId, Existing, CancellationToken.None);

        remover.Received.ShouldHaveSingleItem().ShouldBe(PairedBytes);

        Candidate after = Stored(Existing);
        after.FrontImageFile.ShouldBe($"images/{Existing}-front.png");
        after.BackImageFile.ShouldBe($"images/{Existing}-back.png");
        (after with { FrontImageFile = null, BackImageFile = null }).ShouldBe(before);
        edited.Blueprint.Candidates.Single().FrontImageFile.ShouldBe(after.FrontImageFile);
    }

    [Fact]
    public async Task CuttingOutAgainReplacesTheCutOuts()
    {
        await OnDemand().CutOutAsync(Directory, CalibrationFixture.Calibration(), ProjectFixture.BlueprintId, Existing, CancellationToken.None);
        remover.Script = (_, _) => Task.FromResult(new CutoutPair([7], [8]));

        await OnDemand().CutOutAsync(Directory, CalibrationFixture.Calibration(), ProjectFixture.BlueprintId, Existing, CancellationToken.None);

        repository.Images.Last(image => image.Path == $"images/{Existing}-front.png").Png.ShouldBe([7]);
        repository.Images.Count(image => image.Path == $"images/{Existing}-front.png").ShouldBe(1);
    }

    [Fact]
    public async Task ARefusedCutOutChangesNothing()
    {
        remover.Script = ScriptedBackgroundRemover.Refusing(CutoutErrorCode.SubjectNotFound);
        int saves = repository.Saves;

        CutoutException error = await Should.ThrowAsync<CutoutException>(() => OnDemand().CutOutAsync(
            Directory, CalibrationFixture.Calibration(), ProjectFixture.BlueprintId, Existing, CancellationToken.None));

        error.WireCode.ShouldBe("CUTOUT_SUBJECT_NOT_FOUND");
        repository.Saves.ShouldBe(saves);
        Stored(Existing).FrontImageFile.ShouldBeNull();
    }

    // --- F.9 n° 21 : rien à détourer ------------------------------------------------------------------

    [Fact]
    public async Task ACandidateWithoutAPairedImageHasNothingToCutOut()
    {
        // What a Share archive leaves: the paired image is gone (DEC-050).
        repository.EditBehindTheBatch(project => project with
        {
            Blueprints = [ProjectFixture.Blueprint(
                candidates: [ProjectFixture.Candidate(id: Existing, cutOut: false) with { PairedImageFile = null }])],
        });

        CutoutException error = await Should.ThrowAsync<CutoutException>(() => OnDemand().CutOutAsync(
            Directory, CalibrationFixture.Calibration(), ProjectFixture.BlueprintId, Existing, CancellationToken.None));

        error.WireCode.ShouldBe("CANDIDATE_NO_PAIRED_IMAGE");
        remover.Received.ShouldBeEmpty();
    }

    [Fact]
    public async Task APairedImageMissingOnDiskIsNothingToCutOutEither()
    {
        repository.Images.Clear();

        CutoutException error = await Should.ThrowAsync<CutoutException>(() => OnDemand().CutOutAsync(
            Directory, CalibrationFixture.Calibration(), ProjectFixture.BlueprintId, Existing, CancellationToken.None));

        error.WireCode.ShouldBe("CANDIDATE_NO_PAIRED_IMAGE");
    }

    [Fact]
    public async Task AnUnknownCandidateIsNotFound()
    {
        BlueprintRuleException error = await Should.ThrowAsync<BlueprintRuleException>(() => OnDemand().CutOutAsync(
            Directory, CalibrationFixture.Calibration(), ProjectFixture.BlueprintId, Guid.NewGuid(), CancellationToken.None));

        error.WireCode.ShouldBe("CANDIDATE_NOT_FOUND");
    }
}
