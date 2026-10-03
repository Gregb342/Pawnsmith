using Pawnsmith.Api.Hosting;
using Pawnsmith.Application.Generation;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Projects;
using Pawnsmith.Domain.Jobs;

namespace Pawnsmith.Api.Jobs;

/// <summary>
/// Runs the queued batches, one at a time, in arrival order (DEC-085).
/// </summary>
/// <remarks>
/// <para>
/// <b>One at a time.</b> A local generator has one graphics card. Two batches
/// running together would share ComfyUI's own queue without going any faster,
/// and the order of their candidates would become unreadable. A single reader
/// on the registry's queue is the whole of that guarantee.
/// </para>
/// <para>
/// A job cancelled while it waited is skipped without running. A job that
/// fails ends <c>Failed</c> through the use case itself, which never throws once
/// a job exists (§E.4) — and if something did escape, the worker marks the job
/// failed rather than letting the loop die and every later job wait forever.
/// </para>
/// </remarks>
public sealed class GenerationWorker(
    JobRegistry registry,
    GeneratorSetup setup,
    IProjectRepository repository,
    GenerationOptions options,
    ProjectWriteGate gate) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Nothing to run when generation is not set up: the start route refuses
        // every job before it is queued (§G.7.1).
        if (setup.Generator is not IImageGenerator generator)
        {
            return;
        }

        var generation = new CandidateGeneration(generator, repository, options, TimeProvider.System, gate);

        await foreach (Guid id in registry.Queue.ReadAllAsync(stoppingToken))
        {
            JobEntry entry = registry.Get(id);

            if (entry.Current.State != JobState.Queued)
            {
                continue;
            }

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(entry.Cancellation.Token, stoppingToken);

            try
            {
                Job finished = await generation.RunAsync(entry.Batch, entry.Current, job => registry.Update(id, job), linked.Token);
                registry.Update(id, finished);
            }
            catch (Exception error) when (!stoppingToken.IsCancellationRequested)
            {
                // Should not happen - the use case ends every job itself - but a
                // loop that died here would leave every later job queued forever.
                if (entry.Current.State == JobState.Running)
                {
                    registry.Update(id, entry.Current.Fail(CandidateGeneration.UnexpectedErrorCode, error.Message));
                }
            }
        }
    }
}
