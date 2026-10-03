using System.Threading.Channels;

using Pawnsmith.Api.Errors;
using Pawnsmith.Application.Generation;
using Pawnsmith.Domain.Jobs;

namespace Pawnsmith.Api.Jobs;

/// <summary>A job the registry knows, with what it needs to run and to be cancelled.</summary>
public sealed class JobEntry(Job job, GenerationBatch batch, string folder)
{
    /// <summary>The latest state of the job.</summary>
    public Job Current { get; set; } = job;

    /// <summary>What the job produces.</summary>
    public GenerationBatch Batch { get; } = batch;

    /// <summary>The project folder, as the API addresses it.</summary>
    public string Folder { get; } = folder;

    /// <summary>Cancels the batch, queued or running.</summary>
    public CancellationTokenSource Cancellation { get; } = new();
}

/// <summary>
/// The jobs of the process: a queue for the worker, and the latest state of
/// each, in memory only (DEC-074, DEC-085).
/// </summary>
/// <remarks>
/// <para>
/// <b>A terminal job never changes again.</b> That rule, enforced in
/// <see cref="Update"/>, is what makes cancellation safe without a second lock:
/// a queued job cancelled at the very moment the worker picks it up is already
/// <c>Cancelled</c> here, so the <c>Running</c> report the worker sends next is
/// ignored, and the batch, whose token is cancelled, ends at once.
/// </para>
/// <para>
/// <b>Bounded.</b> The hundred most recent finished jobs are kept, older ones
/// forgotten. The registry lives in memory and nothing justifies letting it
/// grow for the life of the process; a forgotten job's candidates are in their
/// project already.
/// </para>
/// </remarks>
public sealed class JobRegistry
{
    /// <summary>How many finished jobs are kept.</summary>
    public const int KeptFinishedJobs = 100;

    private readonly object gate = new();
    private readonly Dictionary<Guid, JobEntry> entries = [];
    private readonly List<Guid> order = [];
    private readonly Channel<Guid> queue = Channel.CreateUnbounded<Guid>(new UnboundedChannelOptions { SingleReader = true });

    /// <summary>The identifiers waiting for the worker, in arrival order.</summary>
    public ChannelReader<Guid> Queue => queue.Reader;

    /// <summary>Records a queued job and hands it to the worker.</summary>
    public void Enqueue(Job queued, GenerationBatch batch, string folder)
    {
        lock (gate)
        {
            entries[queued.Id] = new JobEntry(queued, batch, folder);
            order.Add(queued.Id);
        }

        // An unbounded channel accepts every write; there is nothing to wait for.
        queue.Writer.TryWrite(queued.Id);
    }

    /// <summary>A job by its identifier.</summary>
    /// <exception cref="ApiException"><c>JOB_NOT_FOUND</c>.</exception>
    public JobEntry Get(Guid id)
    {
        lock (gate)
        {
            return entries.TryGetValue(id, out JobEntry? entry)
                ? entry
                : throw new ApiException(ApiCodes.JobNotFound);
        }
    }

    /// <summary>A job by its identifier, or null when it was never known or has been forgotten.</summary>
    /// <remarks>
    /// For the worker. A job cancelled while it waited is finished, and a
    /// finished job can be forgotten before the worker reaches its identifier
    /// in the queue; throwing there would end the worker's loop, and every
    /// later job would wait forever.
    /// </remarks>
    public JobEntry? Find(Guid id)
    {
        lock (gate)
        {
            return entries.GetValueOrDefault(id);
        }
    }

    /// <summary>Every known job, the most recent first.</summary>
    public IReadOnlyList<JobEntry> List()
    {
        lock (gate)
        {
            return [.. Enumerable.Reverse(order).Select(id => entries[id])];
        }
    }

    /// <summary>Records a new state reported by the batch. Ignored once the job is terminal.</summary>
    public void Update(Guid id, Job job)
    {
        lock (gate)
        {
            if (!entries.TryGetValue(id, out JobEntry? entry) || entry.Current.IsTerminal)
            {
                return;
            }

            entry.Current = job;

            if (job.IsTerminal)
            {
                ForgetOldestFinished();
            }
        }
    }

    /// <summary>
    /// Cancels a job: at once if it is queued, through its token if it runs.
    /// </summary>
    /// <exception cref="ApiException"><c>JOB_NOT_FOUND</c>, or <c>JOB_ALREADY_FINISHED</c>.</exception>
    public JobEntry Cancel(Guid id)
    {
        JobEntry entry;

        lock (gate)
        {
            entry = entries.TryGetValue(id, out JobEntry? found)
                ? found
                : throw new ApiException(ApiCodes.JobNotFound);

            if (entry.Current.IsTerminal)
            {
                throw new ApiException(ApiCodes.JobAlreadyFinished);
            }

            if (entry.Current.State == JobState.Queued)
            {
                // Never ran: Queued -> Cancelled, nothing produced (DEC-074).
                entry.Current = entry.Current.Cancel();
                ForgetOldestFinished();
            }
        }

        // Running: the batch observes its token and ends Cancelled, keeping
        // what it produced (DEC-075).
        entry.Cancellation.Cancel();

        return entry;
    }

    private void ForgetOldestFinished()
    {
        List<Guid> finished = [.. order.Where(id => entries[id].Current.IsTerminal)];

        foreach (Guid id in finished.Take(Math.Max(0, finished.Count - KeptFinishedJobs)))
        {
            entries[id].Cancellation.Dispose();
            entries.Remove(id);
            order.Remove(id);
        }
    }
}
