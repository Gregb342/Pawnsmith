namespace Pawnsmith.Domain.Jobs;

/// <summary>
/// Where a job stands. Five states, three of them terminal (DEC-074).
/// </summary>
/// <remarks>
/// <para>
/// There is deliberately no "partly succeeded" state. A batch of five that
/// fails on the third is <see cref="Failed"/> with two candidates produced:
/// the pair (state, number produced) already says everything, and a sixth
/// state would double every <c>switch</c> of the interface without telling it
/// anything new (§E.3.2).
/// </para>
/// </remarks>
public enum JobState
{
    /// <summary>Accepted, not started. Nothing produced.</summary>
    Queued,

    /// <summary>Started. Counts what it has produced so far.</summary>
    Running,

    /// <summary>Terminal. Everything requested was produced.</summary>
    Completed,

    /// <summary>Terminal. Stopped on an error, carrying its code. What was produced stays produced.</summary>
    Failed,

    /// <summary>Terminal. Stopped by the user. What was produced stays produced.</summary>
    Cancelled,
}

/// <summary>Why a job failed, as a code an API returns and a message a person reads.</summary>
/// <param name="Code">The wire code — one of E.11, or the code of the exception that stopped the job.</param>
/// <param name="Message">What happened, for a log or a command line. Never translated.</param>
public sealed record JobFailure(string Code, string Message);

/// <summary>A candidate the batch saved without its cut-outs, and why (DEC-101).</summary>
/// <param name="CandidateId">The candidate, saved with its paired image only.</param>
/// <param name="Code">The wire code of the cut-out's refusal (§F.6).</param>
/// <param name="Message">What happened, for a log. Never translated.</param>
public sealed record CutoutFailure(Guid CandidateId, string Code, string Message);

/// <summary>
/// A traceable unit of asynchronous work: in T4, the generation of a batch of
/// candidates for one blueprint.
/// </summary>
/// <remarks>
/// <para>
/// <b>An immutable value, and every transition returns a new one.</b> The
/// transitions are the whole of DEC-074, and they are the only way to obtain a
/// job in another state: the constructor is private, so a job cannot be
/// assembled in a state it could not have reached. An illegal transition is a
/// programming error and throws at once, rather than producing a job that
/// claims to be both cancelled and running.
/// </para>
/// <para>
/// <b>Nothing ever writes a job to disk</b> (§E.3.4). Every candidate is saved
/// the moment it exists (DEC-075), so after a restart the project holds exactly
/// what was produced and the job had nothing more to say. Persisting it would
/// cost a <c>versionSchema</c> (DEC-048) for data that only means something
/// while the process runs.
/// </para>
/// <para>
/// It names the blueprint it works for, which is specific to generation. The
/// glossary also calls cut-out and export "jobs"; whether those carry a
/// blueprint is for T5 and T6 to decide with the case in front of them, not for
/// this type to anticipate.
/// </para>
/// </remarks>
public sealed record Job
{
    private Job(
        Guid id,
        Guid blueprintId,
        int requested,
        IReadOnlyList<Guid> produced,
        JobState state,
        JobFailure? failure,
        IReadOnlyList<CutoutFailure> cutoutFailures)
    {
        Id = id;
        BlueprintId = blueprintId;
        Requested = requested;
        Produced = produced;
        State = state;
        Failure = failure;
        CutoutFailures = cutoutFailures;
    }

    /// <summary>Identifier, propagated through the logs once T7 exists (chapter 8).</summary>
    public Guid Id { get; }

    /// <summary>The blueprint the batch produces candidates for.</summary>
    public Guid BlueprintId { get; }

    /// <summary>How many candidates were asked for. At least one.</summary>
    public int Requested { get; }

    /// <summary>The candidates saved so far, in the order they were produced.</summary>
    public IReadOnlyList<Guid> Produced { get; }

    /// <summary>Where the job stands.</summary>
    public JobState State { get; }

    /// <summary>Why it failed. Set in <see cref="JobState.Failed"/> and only there.</summary>
    public JobFailure? Failure { get; }

    /// <summary>
    /// The candidates saved without their cut-outs, in the order they were
    /// produced (DEC-101). A failed cut-out does not stop the batch, so it is
    /// not a <see cref="Failure"/>: the job can be <c>Completed</c> and list some.
    /// </summary>
    public IReadOnlyList<CutoutFailure> CutoutFailures { get; }

    /// <summary>Whether no transition can leave this state any more.</summary>
    public bool IsTerminal => State is JobState.Completed or JobState.Failed or JobState.Cancelled;

    /// <summary>A new job, <see cref="JobState.Queued"/>, with nothing produced.</summary>
    /// <param name="blueprintId">The blueprint to produce for.</param>
    /// <param name="requested">How many candidates to produce. At least one.</param>
    public static Job Queue(Guid blueprintId, int requested)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(requested, 1);

        return new Job(Guid.NewGuid(), blueprintId, requested, [], JobState.Queued, failure: null, cutoutFailures: []);
    }

    /// <summary><c>Queued → Running</c>.</summary>
    public Job Start()
    {
        Require(JobState.Queued, nameof(Start));

        return With(JobState.Running);
    }

    /// <summary>Records one more candidate saved. Only while running.</summary>
    /// <remarks>
    /// Recording more than was requested is refused: it would mean the batch
    /// produced a candidate nobody asked for, which no caller can do on purpose.
    /// </remarks>
    public Job RecordProduced(Guid candidateId)
    {
        Require(JobState.Running, nameof(RecordProduced));

        if (Produced.Count >= Requested)
        {
            throw new InvalidOperationException(
                $"Job {Id} already produced the {Requested} candidate(s) it was asked for.");
        }

        return new Job(Id, BlueprintId, Requested, [.. Produced, candidateId], State, Failure, CutoutFailures);
    }

    /// <summary>Records that a candidate already produced was saved without its cut-outs. Only while running.</summary>
    public Job RecordCutoutFailure(Guid candidateId, string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(message);
        Require(JobState.Running, nameof(RecordCutoutFailure));

        if (!Produced.Contains(candidateId))
        {
            throw new InvalidOperationException(
                $"Job {Id} did not produce the candidate {candidateId}; it cannot record its cut-out.");
        }

        return new Job(Id, BlueprintId, Requested, Produced, State, Failure, [.. CutoutFailures, new CutoutFailure(candidateId, code, message)]);
    }

    /// <summary><c>Running → Completed</c>, once everything requested has been produced.</summary>
    public Job Complete()
    {
        Require(JobState.Running, nameof(Complete));

        if (Produced.Count != Requested)
        {
            throw new InvalidOperationException(
                $"Job {Id} cannot complete with {Produced.Count} of {Requested} candidate(s) produced; " +
                "a batch that stops short is Failed or Cancelled.");
        }

        return With(JobState.Completed);
    }

    /// <summary><c>Running → Failed</c>, keeping what was produced.</summary>
    /// <param name="code">Wire code of what stopped the batch.</param>
    /// <param name="message">What happened, in words.</param>
    public Job Fail(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(message);
        Require(JobState.Running, nameof(Fail));

        return new Job(Id, BlueprintId, Requested, Produced, JobState.Failed, new JobFailure(code, message), CutoutFailures);
    }

    /// <summary><c>Queued → Cancelled</c> or <c>Running → Cancelled</c>, keeping what was produced.</summary>
    public Job Cancel()
    {
        if (State is not (JobState.Queued or JobState.Running))
        {
            throw IllegalTransition(nameof(Cancel));
        }

        return With(JobState.Cancelled);
    }

    private Job With(JobState state) => new(Id, BlueprintId, Requested, Produced, state, Failure, CutoutFailures);

    private void Require(JobState expected, string transition)
    {
        if (State != expected)
        {
            throw IllegalTransition(transition);
        }
    }

    private InvalidOperationException IllegalTransition(string transition) =>
        new($"Job {Id} cannot {transition} from the {State} state (DEC-074).");
}
