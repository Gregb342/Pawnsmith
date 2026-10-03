using Pawnsmith.Application.Blueprints;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Projects;
using Pawnsmith.Domain.Jobs;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Application.Generation;

/// <summary>A batch to run: N seeds for one blueprint of one project.</summary>
/// <param name="ProjectDirectory">The project folder. The repository is stateless (DEC-062).</param>
/// <param name="BlueprintId">The blueprint to produce candidates for.</param>
/// <param name="Seeds">One candidate per seed, in this order. Chosen by the caller (§E.4.1).</param>
/// <param name="FramingClause">
/// The framing clause in force, from the workflow template. A plain argument:
/// no port exists to fetch it, and none should (§C.5.2).
/// </param>
/// <param name="Calibration">Needed to load the project (DEC-053), and for nothing else here.</param>
public sealed record GenerationBatch(
    string ProjectDirectory,
    Guid BlueprintId,
    IReadOnlyList<ulong> Seeds,
    string FramingClause,
    Calibration Calibration);

/// <summary>
/// Produces the candidates of a batch, saving each one the moment it exists
/// (§E.4, DEC-075), and reports every move of its job (DEC-074).
/// </summary>
/// <remarks>
/// <para>
/// <b>This is where the pair is produced</b> — the use case that DEC-078 put in
/// place of <c>IPawnPairProducer</c>. One generation gives one paired image;
/// the cut is decided by <c>PairSplit</c> and executed in T5.
/// </para>
/// <para>
/// <b>One prompt for the whole batch.</b> The three clauses are frozen once, at
/// the start; the seeds are what varies. A candidate produced after the user
/// edited the subject clause still carries the old one, and is misaligned from
/// birth — which is exactly true.
/// </para>
/// <para>
/// <b>For each seed: generate, reload, write the image, add the candidate,
/// save.</b> The project is reloaded every time and never held for the length
/// of the batch, which can last an hour: a project held that long would
/// overwrite, at every save, whatever the user changed meanwhile. The image is
/// written before the candidate that references it, so that the worst a crash
/// leaves is an orphan file, never a dangling reference.
/// </para>
/// <para>
/// <b>Seen from outside, nothing here throws once the job exists.</b> A
/// generator that cannot be reached, a blueprint deleted mid-batch, a disk that
/// refuses a write: each ends the job <c>Failed</c>, with the code of what
/// stopped it, and what was produced before stays in the project. From T6 on,
/// the batch runs in the background, and a job that vanished on an unexpected
/// exception would leave the interface waiting forever. What <i>does</i> throw is
/// a request refused before any job exists — an empty or oversized batch, an
/// unknown blueprint, a project that does not load.
/// </para>
/// </remarks>
public sealed class CandidateGeneration
{
    /// <summary>The code of a failure no other code describes (§E.11).</summary>
    public const string UnexpectedErrorCode = "JOB_UNEXPECTED_ERROR";

    private readonly IImageGenerator generator;
    private readonly IProjectRepository repository;
    private readonly GenerationOptions options;
    private readonly TimeProvider clock;
    private readonly ProjectWriteGate gate;

    /// <param name="clock">Where <c>generatedAt</c> comes from. Injected so a test can pin it.</param>
    /// <param name="gate">
    /// The per-project write gate shared with every other writer (DEC-086).
    /// Without one — the command line, which is the only writer — the batch
    /// uses a gate of its own, which never waits.
    /// </param>
    public CandidateGeneration(
        IImageGenerator generator,
        IProjectRepository repository,
        GenerationOptions options,
        TimeProvider? clock = null,
        ProjectWriteGate? gate = null)
    {
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(options);

        this.generator = generator;
        this.repository = repository;
        this.options = options;
        this.clock = clock ?? TimeProvider.System;
        this.gate = gate ?? new ProjectWriteGate();
    }

    /// <summary>Validates a batch and runs it to a terminal state, in one call.</summary>
    /// <remarks>
    /// <see cref="QueueAsync"/> then <see cref="RunAsync(GenerationBatch, Job, Action{Job}?, CancellationToken)"/>,
    /// for a caller that has no queue of its own — the command line.
    /// </remarks>
    /// <param name="batch">What to produce.</param>
    /// <param name="onChange">
    /// Told every new state of the job, in order, synchronously — queued,
    /// running, each candidate, the end. A plain callback rather than an
    /// <see cref="IProgress{T}"/>: the framework's <c>Progress</c> posts its
    /// reports to the thread pool, where they may arrive out of order. It must
    /// not throw.
    /// </param>
    /// <param name="cancellationToken">Cancels the batch. What was produced stays.</param>
    /// <returns>The job, in a terminal state.</returns>
    /// <exception cref="GenerationRuleException"><c>BATCH_SIZE_INVALID</c>, before any job exists.</exception>
    /// <exception cref="BlueprintRuleException"><c>BLUEPRINT_NOT_FOUND</c>, before any job exists.</exception>
    public async Task<Job> RunAsync(
        GenerationBatch batch,
        Action<Job>? onChange,
        CancellationToken cancellationToken)
    {
        Job queued = await QueueAsync(batch, cancellationToken).ConfigureAwait(false);
        onChange?.Invoke(queued);

        return await RunAsync(batch, queued, onChange, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Validates a batch and returns its job, <c>Queued</c>. Generates nothing.</summary>
    /// <remarks>
    /// The refusals come here, before any job exists: a malformed request is
    /// not a job that fails, it is a job that never existed (§E.4.4). Split
    /// from the run so that a queue can refuse a request at once and run it
    /// later (DEC-085).
    /// </remarks>
    /// <exception cref="GenerationRuleException"><c>BATCH_SIZE_INVALID</c>.</exception>
    /// <exception cref="BlueprintRuleException"><c>BLUEPRINT_NOT_FOUND</c>.</exception>
    public async Task<Job> QueueAsync(GenerationBatch batch, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(batch);
        RequireBatchSize(batch.Seeds.Count);

        LoadedProjectResult loaded = await repository
            .LoadAsync(batch.ProjectDirectory, batch.Calibration, cancellationToken)
            .ConfigureAwait(false);

        BlueprintEditor.Find(loaded.Project, batch.BlueprintId);

        return Job.Queue(batch.BlueprintId, batch.Seeds.Count);
    }

    /// <summary>Runs a queued job to a terminal state.</summary>
    /// <remarks>
    /// The clauses are frozen <b>here, when the batch starts</b>, not when it
    /// was queued: a user who corrects the clause while the batch waits wants
    /// the correction to count. Which is also why the blueprint is looked for
    /// again — it may have been deleted while the job waited, and the job then
    /// ends <c>Failed</c> with <c>BLUEPRINT_NOT_FOUND</c>.
    /// </remarks>
    /// <param name="batch">The batch the job was queued for.</param>
    /// <param name="queued">The job <see cref="QueueAsync"/> returned, still <c>Queued</c>.</param>
    /// <param name="onChange">Told every state from <c>Running</c> on. See the other overload.</param>
    /// <param name="cancellationToken">Cancels the batch. What was produced stays.</param>
    /// <returns>The job, in a terminal state.</returns>
    public async Task<Job> RunAsync(
        GenerationBatch batch,
        Job queued,
        Action<Job>? onChange,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(batch);
        ArgumentNullException.ThrowIfNull(queued);

        Job job = queued.Start();
        onChange?.Invoke(job);

        try
        {
            LoadedProjectResult loaded = await repository
                .LoadAsync(batch.ProjectDirectory, batch.Calibration, cancellationToken)
                .ConfigureAwait(false);

            Blueprint blueprint = BlueprintEditor.Find(loaded.Project, batch.BlueprintId);
            FrozenClauses clauses = Freeze(batch.FramingClause, blueprint, loaded.Project.Style);

            foreach (ulong seed in batch.Seeds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                GeneratedImage image = await generator
                    .GenerateAsync(new GenerationRequest(clauses.Prompt, clauses.Negative, seed), cancellationToken)
                    .ConfigureAwait(false);

                Guid candidateId = await gate
                    .RunAsync(batch.ProjectDirectory, () => PersistAsync(batch, clauses, seed, image), CancellationToken.None)
                    .ConfigureAwait(false);

                job = job.RecordProduced(candidateId);
                onChange?.Invoke(job);
            }

            job = job.Complete();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            job = job.Cancel();
        }
        catch (Exception error) when (error is ICodedException coded)
        {
            // GENERATOR_*, BLUEPRINT_NOT_FOUND, or the code of an
            // Infrastructure exception this layer cannot name (§E.11).
            job = job.Fail(coded.WireCode, error.Message);
        }
        catch (Exception error)
        {
            // Deliberately broad. A background job that ended on an exception
            // nobody caught would never reach a terminal state, and whoever
            // watches it would wait forever. The message travels with the job;
            // T7 will log the rest.
            job = job.Fail(UnexpectedErrorCode, error.Message);
        }

        onChange?.Invoke(job);
        return job;
    }

    private void RequireBatchSize(int count)
    {
        if (count < 1 || count > options.MaxBatchSize)
        {
            throw new GenerationRuleException(
                GenerationRuleCode.BatchSizeInvalid,
                $"A batch asks for between 1 and {options.MaxBatchSize} candidates; this one asks for {count}.");
        }
    }

    /// <summary>
    /// Reloads the project, writes the image, adds the candidate, saves —
    /// none of it cancellable, all of it behind the project's write gate.
    /// </summary>
    /// <remarks>
    /// The cancellation is of the <i>batch</i>, not of the image that has just
    /// cost forty seconds of graphics card. These steps get no token; the next
    /// round of the loop observes the cancellation. The gate is what makes the
    /// reload worth something once another writer exists (DEC-086).
    /// </remarks>
    private async Task<Guid> PersistAsync(GenerationBatch batch, FrozenClauses clauses, ulong seed, GeneratedImage image)
    {
        LoadedProjectResult current = await repository
            .LoadAsync(batch.ProjectDirectory, batch.Calibration, CancellationToken.None)
            .ConfigureAwait(false);

        // Deleted mid-batch: BLUEPRINT_NOT_FOUND, and the image is not written
        // — nothing would reference it.
        Blueprint blueprint = BlueprintEditor.Find(current.Project, batch.BlueprintId);

        var candidateId = Guid.NewGuid();

        string pairedImageFile = await repository
            .WritePairedImageAsync(batch.ProjectDirectory, candidateId, image.Png, CancellationToken.None)
            .ConfigureAwait(false);

        var candidate = new Candidate(
            Id: candidateId,
            Seed: seed,
            FramingClauseUsed: clauses.Framing,
            SubjectClauseUsed: clauses.Subject,
            StyleClauseUsed: clauses.Style,
            Status: CandidateStatus.Draft,
            PairedImageFile: pairedImageFile,
            FrontImageFile: null,
            BackImageFile: null,
            GeneratedAt: TruncateToSecond(clock.GetUtcNow()));

        // Appended last; the election, the other statuses and the clause do
        // not move (DEC-068).
        Blueprint grown = blueprint with { Candidates = [.. blueprint.Candidates, candidate] };

        await repository
            .SaveAsync(batch.ProjectDirectory, BlueprintEditor.Replace(current.Project, grown), CancellationToken.None)
            .ConfigureAwait(false);

        return candidateId;
    }

    /// <summary>The three clauses as they will be sent and frozen, and what is sent beside them.</summary>
    /// <remarks>
    /// Normalised here, once, so that what the candidate freezes and what the
    /// generator receives come from the same strings. The prompt is
    /// <see cref="ResolvedPrompt.From"/> of exactly these three, and nothing
    /// transforms it afterwards (DEC-049, DEC-077).
    /// </remarks>
    private static FrozenClauses Freeze(string framingClause, Blueprint blueprint, Style style)
    {
        string framing = ResolvedPrompt.Normalize(framingClause);
        string subject = ResolvedPrompt.Normalize(blueprint.SubjectClause);
        string styleClause = ResolvedPrompt.Normalize(style.StyleClause);

        return new FrozenClauses(
            framing,
            subject,
            styleClause,
            ResolvedPrompt.From(framing, subject, styleClause),
            ResolvedPrompt.Normalize(style.NegativeClause));
    }

    /// <summary>The second is the precision the file records (C.3.3).</summary>
    /// <remarks>
    /// Without it the candidate in memory would carry milliseconds the file does
    /// not, and the first comparison between the two would disagree for a
    /// reason nobody could see — the reason <c>NewProject</c> truncates too.
    /// </remarks>
    private static DateTimeOffset TruncateToSecond(DateTimeOffset instant)
    {
        DateTimeOffset utc = instant.ToUniversalTime();

        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, utc.Minute, utc.Second, TimeSpan.Zero);
    }

    private sealed record FrozenClauses(string Framing, string Subject, string Style, string Prompt, string Negative);
}
