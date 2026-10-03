using Pawnsmith.Domain.Jobs;

namespace Pawnsmith.Domain.Tests.Jobs;

/// <summary>
/// Covers tests 1 to 6 of E.12: the state machine of DEC-074.
/// </summary>
public class JobTests
{
    private static readonly Guid BlueprintId = new("2d6b1f04-9c33-4a71-8e52-0b7d61a9c418");
    private static readonly Guid First = new("b4c7e910-2f88-4d16-9a03-5e1c8b72d055");
    private static readonly Guid Second = new("0a19d5c3-7b64-4e28-b0f7-3c2a91e8d740");

    // --- E.12 n° 1 : naissance ---------------------------------------------

    [Fact]
    public void ANewJobIsQueuedWithNothingProduced()
    {
        var job = Job.Queue(BlueprintId, requested: 3);

        job.State.ShouldBe(JobState.Queued);
        job.Produced.ShouldBeEmpty();
        job.Requested.ShouldBe(3);
        job.BlueprintId.ShouldBe(BlueprintId);
        job.Failure.ShouldBeNull();
        job.IsTerminal.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AJobAsksForAtLeastOneCandidate(int requested)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Job.Queue(BlueprintId, requested));
    }

    // --- E.12 n° 2 et 3 : les vingt-cinq couples (de, vers) ------------------

    /// <summary>
    /// Every (from, to) pair of states, with whether DEC-074 allows it.
    /// </summary>
    /// <remarks>
    /// Written as a full table rather than as five positive tests and a few
    /// negative ones: a table that lists all twenty-five pairs cannot forget
    /// one, and a transition added to the code without being added here fails
    /// the pair it opened.
    /// </remarks>
    public static TheoryData<JobState, JobState, bool> AllPairs()
    {
        HashSet<(JobState, JobState)> allowed =
        [
            (JobState.Queued, JobState.Running),
            (JobState.Queued, JobState.Cancelled),
            (JobState.Running, JobState.Completed),
            (JobState.Running, JobState.Failed),
            (JobState.Running, JobState.Cancelled),
        ];

        TheoryData<JobState, JobState, bool> data = [];

        foreach (JobState from in Enum.GetValues<JobState>())
        {
            foreach (JobState to in Enum.GetValues<JobState>())
            {
                data.Add(from, to, allowed.Contains((from, to)));
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void OnlyTheFiveTransitionsOfDec074AreAllowed(JobState from, JobState to, bool allowed)
    {
        Job job = InState(from);

        if (allowed)
        {
            Move(job, to).State.ShouldBe(to);
        }
        else
        {
            Should.Throw<InvalidOperationException>(() => Move(job, to));
        }
    }

    [Theory]
    [InlineData(JobState.Completed)]
    [InlineData(JobState.Failed)]
    [InlineData(JobState.Cancelled)]
    public void TerminalStatesAreTerminal(JobState state)
    {
        Job job = InState(state);

        job.IsTerminal.ShouldBeTrue();
        Should.Throw<InvalidOperationException>(() => job.RecordProduced(Second));
    }

    // --- E.12 n° 4 : enregistrement, en Running seulement, dans l'ordre -----

    [Fact]
    public void ProducedCandidatesAreRecordedInOrderWhileRunning()
    {
        Job job = Job.Queue(BlueprintId, requested: 2).Start()
            .RecordProduced(First)
            .RecordProduced(Second);

        job.Produced.ShouldBe([First, Second]);
    }

    [Fact]
    public void NothingIsRecordedBeforeTheJobStarts()
    {
        Should.Throw<InvalidOperationException>(() => Job.Queue(BlueprintId, 1).RecordProduced(First));
    }

    [Fact]
    public void AJobDoesNotRecordMoreThanItWasAskedFor()
    {
        Job full = Job.Queue(BlueprintId, requested: 1).Start().RecordProduced(First);

        Should.Throw<InvalidOperationException>(() => full.RecordProduced(Second));
    }

    [Fact]
    public void ATransitionLeavesTheOriginalUntouched()
    {
        // Immutable: the runner of T6 hands the current value to readers on
        // other threads, and a transition must never change what they hold.
        Job running = Job.Queue(BlueprintId, requested: 2).Start();

        running.RecordProduced(First);

        running.Produced.ShouldBeEmpty();
        running.State.ShouldBe(JobState.Running);
    }

    // --- E.12 n° 5 : Completed exige tout ce qui était demandé ---------------

    [Fact]
    public void AJobCannotCompleteShortOfWhatWasRequested()
    {
        Job halfway = Job.Queue(BlueprintId, requested: 2).Start().RecordProduced(First);

        Should.Throw<InvalidOperationException>(() => halfway.Complete());
    }

    // --- E.12 n° 6 : ce que portent Failed et Cancelled ---------------------

    [Fact]
    public void AFailedJobCarriesItsCodeAndMessageAndKeepsWhatItProduced()
    {
        Job failed = Job.Queue(BlueprintId, requested: 3).Start()
            .RecordProduced(First)
            .Fail("GENERATOR_UNREACHABLE", "Connection refused.");

        failed.State.ShouldBe(JobState.Failed);
        failed.Failure.ShouldBe(new JobFailure("GENERATOR_UNREACHABLE", "Connection refused."));
        failed.Produced.ShouldBe([First]);
    }

    [Fact]
    public void ACancelledJobCarriesNoFailureAndKeepsWhatItProduced()
    {
        Job cancelled = Job.Queue(BlueprintId, requested: 3).Start()
            .RecordProduced(First)
            .Cancel();

        cancelled.State.ShouldBe(JobState.Cancelled);
        cancelled.Failure.ShouldBeNull();
        cancelled.Produced.ShouldBe([First]);
    }

    [Fact]
    public void AFailureNeedsACode()
    {
        Job running = Job.Queue(BlueprintId, 1).Start();

        Should.Throw<ArgumentException>(() => running.Fail(" ", "no code"));
    }

    /// <summary>A job of one candidate, brought to the given state by legal transitions only.</summary>
    private static Job InState(JobState state)
    {
        var queued = Job.Queue(BlueprintId, requested: 1);

        return state switch
        {
            JobState.Queued => queued,
            JobState.Running => queued.Start(),
            JobState.Completed => queued.Start().RecordProduced(First).Complete(),
            JobState.Failed => queued.Start().Fail("GENERATOR_FAILED", "Model error."),
            JobState.Cancelled => queued.Cancel(),
            _ => throw new ArgumentOutOfRangeException(nameof(state)),
        };
    }

    /// <summary>
    /// The transition that leads to <paramref name="to"/>. A running job of one
    /// candidate records it first, so that completion is a matter of state and
    /// not of count — the count rule has its own test.
    /// </summary>
    private static Job Move(Job job, JobState to) => to switch
    {
        // No transition leads back to Queued: asking for one is illegal by
        // construction, which is what the table expects.
        JobState.Queued => throw new InvalidOperationException("No transition leads to Queued."),
        JobState.Running => job.Start(),
        JobState.Completed => (job.State == JobState.Running ? job.RecordProduced(First) : job).Complete(),
        JobState.Failed => job.Fail("GENERATOR_FAILED", "Model error."),
        JobState.Cancelled => job.Cancel(),
        _ => throw new ArgumentOutOfRangeException(nameof(to)),
    };
}
