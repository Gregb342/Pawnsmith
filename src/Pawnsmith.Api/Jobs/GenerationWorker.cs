using Pawnsmith.Api.Hosting;
using Pawnsmith.Application.Generation;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Projects;
using Pawnsmith.Domain.Jobs;

using Serilog.Context;

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
/// <para>
/// <b>The job identifier is pushed once, here</b> (§H.2.3, DEC-090). Chapter 8
/// wants it pushed at the entry of the use case; the Application cannot see
/// Serilog, so the entry is its call site. <c>LogContext</c> rides the
/// asynchronous flow: every event written while the batch runs — by this
/// worker, or by an adapter called beneath the use case — carries
/// <c>JobId</c>, and no method ever takes it as a parameter.
/// </para>
/// </remarks>
public sealed class GenerationWorker(
    JobRegistry registry,
    IProjectRepository repository,
    IBackgroundRemover remover,
    GenerationOptions options,
    ProjectWriteGate gate,
    ILogger<GenerationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (Guid id in registry.Queue.ReadAllAsync(stoppingToken))
        {
            // Forgotten already, or cancelled while it waited: nothing to run.
            if (registry.Find(id) is not JobEntry entry || entry.Current.State != JobState.Queued)
            {
                continue;
            }

            // Built per job, with the generator the job was accepted with: the
            // address may have changed since (§I.5.2, DEC-108).
            var generation = new CandidateGeneration(entry.Generator, remover, repository, options, TimeProvider.System, gate);

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(entry.Cancellation.Token, stoppingToken);
            using IDisposable jobScope = LogContext.PushProperty("JobId", id);

            logger.LogInformation(
                "Batch started: {Requested} candidate(s) for blueprint {BlueprintId} of project {Folder}",
                entry.Current.Requested,
                entry.Batch.BlueprintId,
                entry.Folder);

            try
            {
                Job finished = await generation.RunAsync(entry.Batch, entry.Current, job => registry.Update(id, job), linked.Token);
                registry.Update(id, finished);
                LogEnd(finished);
            }
            catch (Exception error) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(error, "Batch ended by an exception the use case did not handle");

                // Should not happen - the use case ends every job itself - but a
                // loop that died here would leave every later job queued forever.
                if (entry.Current.State == JobState.Running)
                {
                    registry.Update(id, entry.Current.Fail(CandidateGeneration.UnexpectedErrorCode, error.Message));
                }
            }
        }
    }

    private void LogEnd(Job finished)
    {
        // A failed cut-out does not end the batch (DEC-101); its message,
        // which the API never returns, comes here.
        foreach (CutoutFailure cutout in finished.CutoutFailures)
        {
            logger.LogWarning(
                "Candidate {CandidateId} saved without its cut-outs: {Code}. {Reason}",
                cutout.CandidateId,
                cutout.Code,
                cutout.Message);
        }

        if (finished.Failure is JobFailure failure)
        {
            logger.LogWarning(
                "Batch failed after {Produced} of {Requested} candidate(s): {Code}. {Reason}",
                finished.Produced.Count,
                finished.Requested,
                failure.Code,
                failure.Message);
        }
        else
        {
            logger.LogInformation(
                "Batch ended {State}: {Produced} of {Requested} candidate(s)",
                finished.State,
                finished.Produced.Count,
                finished.Requested);
        }
    }
}
