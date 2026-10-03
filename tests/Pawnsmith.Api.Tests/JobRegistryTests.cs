using Pawnsmith.Api.Errors;
using Pawnsmith.Api.Jobs;
using Pawnsmith.Application.Generation;
using Pawnsmith.Domain.Jobs;
using Pawnsmith.Domain.PhysicalValues;

namespace Pawnsmith.Api.Tests;

/// <summary>The bounded, in-memory registry of DEC-085.</summary>
public class JobRegistryTests
{
    private static readonly GenerationBatch Batch = new("/projects/a", Guid.NewGuid(), [1UL], "framing", Calibration: null!);

    private static Job Queued() => Job.Queue(Guid.NewGuid(), 1);

    [Fact]
    public void OnlyTheHundredMostRecentFinishedJobsAreKept()
    {
        var registry = new JobRegistry();
        List<Job> jobs = [.. Enumerable.Range(0, JobRegistry.KeptFinishedJobs + 2).Select(_ => Queued())];

        foreach (Job job in jobs)
        {
            registry.Enqueue(job, Batch, "a");
            registry.Cancel(job.Id);
        }

        registry.List().Count.ShouldBe(JobRegistry.KeptFinishedJobs);
        registry.Find(jobs[0].Id).ShouldBeNull();
        registry.Find(jobs[^1].Id).ShouldNotBeNull();
        Should.Throw<ApiException>(() => registry.Get(jobs[0].Id)).WireCode.ShouldBe("JOB_NOT_FOUND");
    }

    [Fact]
    public void AForgottenJobStillInTheQueueIsNotAnErrorForTheWorker()
    {
        // The worker reads identifiers from the queue; one of them may name a
        // job already forgotten. Find answers null rather than throwing, which
        // would have ended the worker's loop.
        var registry = new JobRegistry();
        List<Job> jobs = [.. Enumerable.Range(0, JobRegistry.KeptFinishedJobs + 1).Select(_ => Queued())];

        foreach (Job job in jobs)
        {
            registry.Enqueue(job, Batch, "a");
            registry.Cancel(job.Id);
        }

        registry.Queue.TryRead(out Guid first).ShouldBeTrue();
        registry.Find(first).ShouldBeNull();
    }

    [Fact]
    public void ATerminalJobNeverChangesAgain()
    {
        var registry = new JobRegistry();
        Job queued = Queued();
        registry.Enqueue(queued, Batch, "a");
        registry.Cancel(queued.Id);

        // A late report from a worker that picked the job up at the same time.
        registry.Update(queued.Id, queued.Start());

        registry.Get(queued.Id).Current.State.ShouldBe(JobState.Cancelled);
    }

    [Fact]
    public void ListingIsMostRecentFirst()
    {
        var registry = new JobRegistry();
        Job first = Queued();
        Job second = Queued();
        registry.Enqueue(first, Batch, "a");
        registry.Enqueue(second, Batch, "a");

        registry.List().Select(entry => entry.Current.Id).ShouldBe([second.Id, first.Id]);
    }
}
