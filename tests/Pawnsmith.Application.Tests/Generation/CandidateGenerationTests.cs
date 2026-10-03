using Pawnsmith.Application.Blueprints;
using Pawnsmith.Application.Generation;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Projects;
using Pawnsmith.Application.Tests.Fixtures;
using Pawnsmith.Domain.Jobs;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Application.Tests.Generation;

/// <summary>
/// Covers tests 32 to 41 of E.12: the batch of DEC-075, against a repository in
/// memory and a scripted generator.
/// </summary>
public class CandidateGenerationTests
{
    private const string Directory = "/projects/donjon";

    // Deliberately un-normalised: a CRLF and trailing spaces, so that a test
    // can tell whether what is frozen and what is sent went through Normalize.
    private const string Framing = "front view on the left\r\nback view on the right  ";

    private static readonly DateTimeOffset Now = new(2026, 10, 3, 11, 42, 17, 836, TimeSpan.Zero);
    private static readonly Guid Elected = ProjectFixture.CandidateId;

    private readonly InMemoryProjectRepository repository;
    private readonly ScriptedImageGenerator generator = new();
    private readonly List<Job> changes = [];

    public CandidateGenerationTests()
    {
        // One blueprint, which already has an elected, validated candidate.
        Blueprint blueprint = ProjectFixture.Blueprint(
            subjectClause: "  a goblin skirmisher, wielding a short spear\r\n",
            candidates: [ProjectFixture.Candidate(id: Elected, status: CandidateStatus.Valid)],
            electedCandidateId: Elected);

        repository = new InMemoryProjectRepository(ProjectFixture.Project(blueprints: [blueprint]));
    }

    private CandidateGeneration UseCase(int maxBatchSize = 20) =>
        new(generator, repository, new GenerationOptions { MaxBatchSize = maxBatchSize }, new FixedClock(Now));

    private Task<Job> Run(IReadOnlyList<ulong> seeds, CancellationToken cancellationToken = default, Guid? blueprintId = null) =>
        UseCase().RunAsync(
            new GenerationBatch(Directory, blueprintId ?? ProjectFixture.BlueprintId, seeds, Framing, CalibrationFixture.Calibration()),
            changes.Add,
            cancellationToken);

    private Blueprint StoredBlueprint => repository.Project.Blueprints.Single();

    private IReadOnlyList<Candidate> Produced => StoredBlueprint.Candidates.Skip(1).ToList();

    // --- E.12 n° 32 : un lot de trois -----------------------------------------------

    [Fact]
    public async Task ABatchOfThreeProducesThreeDraftsInSeedOrderAndCompletes()
    {
        Job job = await Run([11UL, 22UL, 33UL]);

        job.State.ShouldBe(JobState.Completed);
        Produced.Select(candidate => candidate.Seed).ShouldBe([11UL, 22UL, 33UL]);
        Produced.ShouldAllBe(candidate => candidate.Status == CandidateStatus.Draft);
        Produced.ShouldAllBe(candidate => candidate.FrontImageFile == null && candidate.BackImageFile == null);
        job.Produced.ShouldBe(Produced.Select(candidate => candidate.Id).ToList());

        // Each candidate references the image written for it, and only that one.
        Produced.Select(candidate => candidate.PairedImageFile).ShouldBe(repository.Images.Select(image => image.Path).ToList());
        repository.Images.Select(image => image.Png).ShouldBe(
        [
            ScriptedImageGenerator.ImageFor(0).Png,
            ScriptedImageGenerator.ImageFor(1).Png,
            ScriptedImageGenerator.ImageFor(2).Png,
        ]);
    }

    [Fact]
    public async Task EveryCandidateIsSavedAsSoonAsItExists()
    {
        await Run([11UL, 22UL, 33UL]);

        repository.Saves.ShouldBe(3);
    }

    [Fact]
    public async Task TheGenerationInstantIsStoredToTheSecond()
    {
        await Run([11UL]);

        Produced.Single().GeneratedAt.ShouldBe(new DateTimeOffset(2026, 10, 3, 11, 42, 17, TimeSpan.Zero));
    }

    [Fact]
    public async Task EveryStateOfTheJobIsReportedInOrder()
    {
        await Run([11UL, 22UL]);

        changes.Select(job => (job.State, job.Produced.Count)).ShouldBe(
        [
            (JobState.Queued, 0),
            (JobState.Running, 0),
            (JobState.Running, 1),
            (JobState.Running, 2),
            (JobState.Completed, 2),
        ]);
    }

    // --- E.12 n° 33 : clauses figées, jamais désaligné à la naissance ----------------

    [Fact]
    public async Task TheFrozenClausesAreTheCurrentOnesNormalisedAndANewCandidateIsAligned()
    {
        await Run([11UL]);

        Candidate candidate = Produced.Single();
        candidate.FramingClauseUsed.ShouldBe("front view on the left\nback view on the right");
        candidate.SubjectClauseUsed.ShouldBe("a goblin skirmisher, wielding a short spear");
        candidate.StyleClauseUsed.ShouldBe(ProjectFixture.StyleClause);

        Misalignment.Of(candidate, StoredBlueprint, repository.Project.Style, Framing).ShouldBeEmpty();
    }

    // --- E.12 n° 34 : ce qui part est exactement ResolvedPrompt.From -------------------

    [Fact]
    public async Task WhatTheGeneratorReceivesIsExactlyTheResolvedPromptOfTheFrozenClauses()
    {
        await Run([11UL, 22UL]);

        generator.Requests.Count.ShouldBe(2);

        foreach ((GenerationRequest request, Candidate candidate) in generator.Requests.Zip(Produced))
        {
            request.Prompt.ShouldBe(ResolvedPrompt.From(
                candidate.FramingClauseUsed,
                candidate.SubjectClauseUsed,
                candidate.StyleClauseUsed));
            request.Seed.ShouldBe(candidate.Seed);
            request.NegativePrompt.ShouldBe(ProjectFixture.Style().NegativeClause);
        }

        // Spelled out once, so the test does not only compare the code to itself.
        generator.Requests[0].Prompt.ShouldBe(
            "front view on the left\nback view on the right\n" +
            "a goblin skirmisher, wielding a short spear\n" +
            ProjectFixture.StyleClause);
    }

    [Fact]
    public async Task AClauseEditedDuringTheBatchDoesNotChangeTheBatchsPrompt()
    {
        generator.Script = (call, _) =>
        {
            if (call == 0)
            {
                repository.EditBehindTheBatch(project => BlueprintEditor
                    .EditSubjectClause(project, ProjectFixture.BlueprintId, "an orc chieftain").Project);
            }

            return Task.FromResult(ScriptedImageGenerator.ImageFor(call));
        };

        await Run([11UL, 22UL]);

        generator.Requests.Select(request => request.Prompt).Distinct().Count().ShouldBe(1);
        Produced.ShouldAllBe(candidate => candidate.SubjectClauseUsed == "a goblin skirmisher, wielding a short spear");

        // Both are misaligned from birth - which is exactly true (DEC-075).
        Produced.ShouldAllBe(candidate =>
            Misalignment.Of(candidate, StoredBlueprint, repository.Project.Style, Framing).Contains(ClauseKind.Subject));
    }

    // --- E.12 n° 35 : un échec au troisième garde les deux premiers ----------------------

    [Fact]
    public async Task AFailureOnTheThirdKeepsTheFirstTwoInTheProject()
    {
        generator.Script = (call, _) => call == 2
            ? throw new GeneratorException(GeneratorErrorCode.Failed, "CUDA out of memory")
            : Task.FromResult(ScriptedImageGenerator.ImageFor(call));

        Job job = await Run([11UL, 22UL, 33UL, 44UL]);

        job.State.ShouldBe(JobState.Failed);
        job.Failure!.Code.ShouldBe("GENERATOR_FAILED");
        job.Failure.Message.ShouldContain("CUDA");
        Produced.Select(candidate => candidate.Seed).ShouldBe([11UL, 22UL]);
        job.Produced.Count.ShouldBe(2);

        // The first failure stops the batch (DEC-074): seed 44 was never tried.
        generator.Requests.Count.ShouldBe(3);
    }

    // --- E.12 n° 36 : annulation après le premier ----------------------------------------

    [Fact]
    public async Task ACancellationAfterTheFirstCandidateKeepsItAndGoesNoFurther()
    {
        using var cancellation = new CancellationTokenSource();

        generator.Script = (call, _) =>
        {
            // The user cancels while the first image is on its way back: that
            // image is paid for and kept, the next one is never asked for.
            cancellation.Cancel();
            return Task.FromResult(ScriptedImageGenerator.ImageFor(call));
        };

        Job job = await Run([11UL, 22UL, 33UL], cancellation.Token);

        job.State.ShouldBe(JobState.Cancelled);
        job.Failure.ShouldBeNull();
        Produced.Select(candidate => candidate.Seed).ShouldBe([11UL]);
        repository.Images.Count.ShouldBe(1);
        generator.Requests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task ACancellationDuringAGenerationKeepsWhatWasAlreadySaved()
    {
        using var cancellation = new CancellationTokenSource();

        generator.Script = async (call, token) =>
        {
            if (call == 1)
            {
                await cancellation.CancelAsync();
                await Task.Delay(Timeout.Infinite, token);
            }

            return ScriptedImageGenerator.ImageFor(call);
        };

        Job job = await Run([11UL, 22UL, 33UL], cancellation.Token);

        job.State.ShouldBe(JobState.Cancelled);
        Produced.Select(candidate => candidate.Seed).ShouldBe([11UL]);
        repository.Images.Count.ShouldBe(1);
    }

    // --- E.12 n° 37 : générateur injoignable -----------------------------------------------

    [Fact]
    public async Task AnUnreachableGeneratorEndsTheJobFailedWithoutThrowingOrWriting()
    {
        generator.Script = (_, _) => throw new GeneratorException(GeneratorErrorCode.Unreachable, "Connection refused");

        Job job = await Run([11UL, 22UL]);

        job.State.ShouldBe(JobState.Failed);
        job.Failure!.Code.ShouldBe("GENERATOR_UNREACHABLE");
        repository.Images.ShouldBeEmpty();
        repository.Saves.ShouldBe(0);
        changes[^1].ShouldBe(job);
    }

    // --- E.12 n° 38 : gabarit supprimé pendant le lot ----------------------------------------

    [Fact]
    public async Task ABlueprintDeletedMidBatchFailsTheJobWithoutAnOrphanFile()
    {
        generator.Script = (call, _) =>
        {
            if (call == 1)
            {
                repository.EditBehindTheBatch(project =>
                    BlueprintRemoval.Remove(project, ProjectFixture.BlueprintId).Project);
            }

            return Task.FromResult(ScriptedImageGenerator.ImageFor(call));
        };

        Job job = await Run([11UL, 22UL, 33UL]);

        job.State.ShouldBe(JobState.Failed);
        job.Failure!.Code.ShouldBe("BLUEPRINT_NOT_FOUND");
        job.Produced.Count.ShouldBe(1);

        // The second image arrived after the deletion and was not written:
        // nothing would have referenced it.
        repository.Images.Count.ShouldBe(1);
    }

    // --- E.12 n° 39 : lot vide ou trop grand -------------------------------------------------

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public async Task ABatchOfTheWrongSizeIsRefusedBeforeAnything(int count)
    {
        ulong[] seeds = [.. Enumerable.Range(1, count).Select(seed => (ulong)seed)];

        GenerationRuleException error = await Should.ThrowAsync<GenerationRuleException>(() =>
            UseCase(maxBatchSize: 3).RunAsync(
                new GenerationBatch(Directory, ProjectFixture.BlueprintId, seeds, Framing, CalibrationFixture.Calibration()),
                changes.Add,
                CancellationToken.None));

        error.WireCode.ShouldBe("BATCH_SIZE_INVALID");
        generator.Requests.ShouldBeEmpty();
        changes.ShouldBeEmpty();
    }

    [Fact]
    public async Task ABatchAtTheBoundIsAccepted()
    {
        Job job = await UseCase(maxBatchSize: 3).RunAsync(
            new GenerationBatch(Directory, ProjectFixture.BlueprintId, [1UL, 2UL, 3UL], Framing, CalibrationFixture.Calibration()),
            onChange: null,
            CancellationToken.None);

        job.State.ShouldBe(JobState.Completed);
    }

    [Fact]
    public async Task AnUnknownBlueprintIsRefusedBeforeAnything()
    {
        BlueprintRuleException error = await Should.ThrowAsync<BlueprintRuleException>(() =>
            Run([11UL], blueprintId: Guid.NewGuid()));

        error.WireCode.ShouldBe("BLUEPRINT_NOT_FOUND");
        generator.Requests.ShouldBeEmpty();
        changes.ShouldBeEmpty();
    }

    // --- E.12 n° 40 : une modification faite pendant le lot survit -----------------------------

    [Fact]
    public async Task AChangeMadeDuringTheBatchIsNotOverwrittenByTheNextSave()
    {
        generator.Script = (call, _) =>
        {
            if (call == 1)
            {
                // Between two candidates, the user renames the project and
                // changes the quantity - two writes the batch knows nothing of.
                repository.EditBehindTheBatch(project => project with
                {
                    Name = "Renamed during the batch",
                    Blueprints = [project.Blueprints.Single() with { Quantity = 9 }],
                });
            }

            return Task.FromResult(ScriptedImageGenerator.ImageFor(call));
        };

        await Run([11UL, 22UL, 33UL]);

        repository.Project.Name.ShouldBe("Renamed during the batch");
        StoredBlueprint.Quantity.ShouldBe(9);
        Produced.Count.ShouldBe(3);
    }

    // --- E.12 n° 41 : l'élu et les statuts ne bougent pas --------------------------------------

    [Fact]
    public async Task TheElectionTheStatusesAndTheClauseDoNotMove()
    {
        await Run([11UL, 22UL]);

        StoredBlueprint.ElectedCandidateId.ShouldBe(Elected);
        StoredBlueprint.Candidates[0].Id.ShouldBe(Elected);
        StoredBlueprint.Candidates[0].Status.ShouldBe(CandidateStatus.Valid);
        StoredBlueprint.SubjectClause.ShouldBe("  a goblin skirmisher, wielding a short spear\r\n");
    }

    // --- Ce qu'aucun code ne décrit -------------------------------------------------------------

    [Fact]
    public async Task AnExceptionFromAnotherLayerKeepsItsCode()
    {
        repository.FailNextSave = new CodedFailure("PROJECT_INVALID");

        Job job = await Run([11UL]);

        job.State.ShouldBe(JobState.Failed);
        job.Failure!.Code.ShouldBe("PROJECT_INVALID");
    }

    [Fact]
    public async Task AnUncodedExceptionEndsTheJobRatherThanEscapingIt()
    {
        repository.FailNextSave = new IOException("No space left on device");

        Job job = await Run([11UL, 22UL]);

        job.State.ShouldBe(JobState.Failed);
        job.Failure!.Code.ShouldBe(CandidateGeneration.UnexpectedErrorCode);
        job.Failure.Message.ShouldContain("No space left");
    }

    /// <summary>Stands for an Infrastructure exception, which these tests cannot reference.</summary>
    private sealed class CodedFailure(string wireCode) : Exception("Refused by the repository."), ICodedException
    {
        public string WireCode { get; } = wireCode;
    }

    // --- T6, DEC-085 : file d'attente, puis exécution ---------------------------------------

    private GenerationBatch Batch(params ulong[] seeds) =>
        new(Directory, ProjectFixture.BlueprintId, seeds, Framing, CalibrationFixture.Calibration());

    [Fact]
    public async Task QueueingValidatesAndGeneratesNothing()
    {
        Job queued = await UseCase().QueueAsync(Batch(11UL, 22UL), CancellationToken.None);

        queued.State.ShouldBe(JobState.Queued);
        queued.Requested.ShouldBe(2);
        generator.Requests.ShouldBeEmpty();
        repository.Saves.ShouldBe(0);
    }

    [Fact]
    public async Task ABlueprintDeletedWhileTheJobWaitedFailsItWithoutGenerating()
    {
        CandidateGeneration useCase = UseCase();
        Job queued = await useCase.QueueAsync(Batch(11UL), CancellationToken.None);

        repository.EditBehindTheBatch(project => BlueprintRemoval.Remove(project, ProjectFixture.BlueprintId).Project);

        Job job = await useCase.RunAsync(Batch(11UL), queued, onChange: null, CancellationToken.None);

        job.State.ShouldBe(JobState.Failed);
        job.Failure!.Code.ShouldBe("BLUEPRINT_NOT_FOUND");
        generator.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task TheClausesAreFrozenWhenTheBatchStartsNotWhenItWasQueued()
    {
        CandidateGeneration useCase = UseCase();
        Job queued = await useCase.QueueAsync(Batch(11UL), CancellationToken.None);

        repository.EditBehindTheBatch(project => BlueprintEditor
            .EditSubjectClause(project, ProjectFixture.BlueprintId, "an orc chieftain").Project);

        await useCase.RunAsync(Batch(11UL), queued, onChange: null, CancellationToken.None);

        Produced.Single().SubjectClauseUsed.ShouldBe("an orc chieftain");
    }

    [Fact]
    public async Task OnlyAQueuedJobCanBeRun()
    {
        CandidateGeneration useCase = UseCase();
        Job queued = await useCase.QueueAsync(Batch(11UL), CancellationToken.None);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            useCase.RunAsync(Batch(11UL), queued.Cancel(), onChange: null, CancellationToken.None));
    }

    // --- T6, DEC-086 : la persistance passe par la porte du projet ---------------------------

    [Fact]
    public async Task TheBatchWaitsAtTheProjectsGateBeforeSaving()
    {
        var gate = new ProjectWriteGate();
        var useCase = new CandidateGeneration(generator, repository, new GenerationOptions(), new FixedClock(Now), gate);
        var release = new TaskCompletionSource<int>();

        // Another writer holds the project.
        Task<int> holder = gate.RunAsync(Directory, () => release.Task, CancellationToken.None);

        Task<Job> batch = useCase.RunAsync(Batch(11UL), onChange: null, CancellationToken.None);

        await Task.Delay(100);
        generator.Requests.Count.ShouldBe(1);
        repository.Saves.ShouldBe(0);
        batch.IsCompleted.ShouldBeFalse();

        release.SetResult(0);
        await holder;

        (await batch).State.ShouldBe(JobState.Completed);
        repository.Saves.ShouldBe(1);
    }
}
