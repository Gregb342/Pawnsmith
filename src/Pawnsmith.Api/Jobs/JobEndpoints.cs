using System.Globalization;

using Pawnsmith.Api.Endpoints;
using Pawnsmith.Api.Errors;
using Pawnsmith.Api.Hosting;
using Pawnsmith.Application.Generation;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Projects;
using Pawnsmith.Domain.Jobs;
using Pawnsmith.Domain.PhysicalValues;

namespace Pawnsmith.Api.Jobs;

/// <summary>Ask for N random seeds, or for these seeds — not both, not neither.</summary>
/// <remarks>
/// Both default to null: the serializer treats a constructor parameter without
/// a default as required, nullable or not, and each of these two is meant to be
/// left out when the other is given.
/// </remarks>
/// <param name="Seeds">Decimal strings, as the API writes them (§G.4).</param>
public sealed record StartJobRequest(int? Count = null, IReadOnlyList<string>? Seeds = null);

/// <summary>A job as the API shows it. A failure carries its code and never its message (DEC-084).</summary>
public sealed record JobDto(
    Guid Id,
    string Folder,
    Guid BlueprintId,
    JobState State,
    int Requested,
    IReadOnlyList<Guid> Produced,
    string? FailureCode);

/// <summary>The routes of the batch queue (§G.7.1).</summary>
public static class JobEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/projects/{folder}/blueprints/{id:guid}/jobs", async (
            string folder,
            Guid id,
            StartJobRequest request,
            PawnsmithSettings settings,
            GeneratorSetup setup,
            IProjectRepository repository,
            GenerationOptions options,
            IBackgroundRemover remover,
            ProjectWriteGate gate,
            Calibration calibration,
            JobRegistry registry,
            CancellationToken cancellationToken) =>
        {
            // Refused before the queue, so that no job exists for a request
            // that cannot run (DEC-085): the generator first, then the folder,
            // then the batch itself through the use case.
            IImageGenerator generator = setup.Generator
                ?? throw new ApiException(setup.ErrorCode ?? ApiCodes.GeneratorNotConfigured);

            string directory = ProjectAccess.Directory(settings, folder);
            var batch = new GenerationBatch(directory, id, Seeds(request, options), setup.FramingClause!, calibration);

            var generation = new CandidateGeneration(generator, remover, repository, options, TimeProvider.System, gate);
            Job queued = await generation.QueueAsync(batch, cancellationToken);

            registry.Enqueue(queued, batch, folder);

            return Results.Accepted($"/api/jobs/{queued.Id}", ToDto(registry.Get(queued.Id)));
        });

        routes.MapGet("/api/jobs", (JobRegistry registry) => registry.List().Select(ToDto).ToList());

        routes.MapGet("/api/jobs/{id:guid}", (Guid id, JobRegistry registry) => ToDto(registry.Get(id)));

        routes.MapPost("/api/jobs/{id:guid}/cancel", (Guid id, JobRegistry registry) => ToDto(registry.Cancel(id)));
    }

    private static JobDto ToDto(JobEntry entry) => new(
        entry.Current.Id,
        entry.Folder,
        entry.Current.BlueprintId,
        entry.Current.State,
        entry.Current.Requested,
        entry.Current.Produced,
        entry.Current.Failure?.Code);

    /// <summary>The seeds of the request, or <c>count</c> random ones.</summary>
    /// <remarks>
    /// <para>
    /// <b>A count above the cap is refused before any seed is drawn.</b> The
    /// use case refuses it anyway (MEN-007); checking first only stops a count
    /// of a billion from allocating a billion seeds on the way to that refusal,
    /// which would be MEN-007 by other means. The bound read is the same
    /// option the use case reads.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<ulong> Seeds(StartJobRequest request, GenerationOptions options)
    {
        if (request.Count is null == request.Seeds is null)
        {
            throw new ApiException(ApiCodes.RequestInvalid);
        }

        if (request.Count is int count)
        {
            return count < 1 || count > options.MaxBatchSize
                ? throw new GenerationRuleException(
                    GenerationRuleCode.BatchSizeInvalid,
                    $"A batch asks for between 1 and {options.MaxBatchSize} candidates; this one asks for {count}.")
                : RandomSeeds.Draw(count);
        }

        return [.. request.Seeds!.Select(seed => ulong.TryParse(seed, NumberStyles.None, CultureInfo.InvariantCulture, out ulong parsed)
            ? parsed
            : throw new ApiException(ApiCodes.RequestInvalid))];
    }
}
