using Pawnsmith.Application.Blueprints;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Projects;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Generation;

/// <summary>
/// Cuts an existing candidate out, on demand (§F.5.3, DEC-101).
/// </summary>
/// <remarks>
/// <para>
/// <b>Who needs it.</b> The batch cuts every new candidate out; this is for the
/// others — candidates generated before T5, a cut-out that failed in its
/// batch, a new attempt after the values of the cut-out were tuned. An
/// existing cut-out is replaced.
/// </para>
/// <para>
/// <b>Read and cut outside the gate, write inside it.</b> The paired image is
/// read and cut out first, which takes a fraction of a second and touches
/// nothing of the project. Then, behind the write gate (DEC-086), the project
/// is reloaded — the candidate may have been removed meanwhile — the cut-outs
/// are written and the candidate saved with their paths. Only the two file
/// fields of the candidate change: not its status, not the election.
/// </para>
/// </remarks>
/// <param name="maxPairedImageBytes">
/// Heaviest paired image read, checked while reading (MEN-005): the
/// infrastructure's <c>CutoutOptions.MaxImageBytes</c>, passed in because
/// Application cannot see it.
/// </param>
public sealed class CandidateCutout(IProjectRepository repository, IBackgroundRemover remover, ProjectWriteGate gate, long maxPairedImageBytes)
{
    /// <exception cref="CutoutException">
    /// <c>CANDIDATE_NO_PAIRED_IMAGE</c> when there is nothing to cut out; any
    /// other code of §F.6 from the cut-out itself.
    /// </exception>
    /// <exception cref="BlueprintRuleException"><c>BLUEPRINT_NOT_FOUND</c> or <c>CANDIDATE_NOT_FOUND</c>.</exception>
    public async Task<EditedProject> CutOutAsync(
        string projectDirectory,
        Calibration calibration,
        Guid blueprintId,
        Guid candidateId,
        CancellationToken cancellationToken)
    {
        LoadedProjectResult loaded = await repository.LoadAsync(projectDirectory, calibration, cancellationToken).ConfigureAwait(false);
        Candidate candidate = Find(loaded.Project, blueprintId, candidateId);

        byte[] paired = await ReadPairedAsync(projectDirectory, candidate, cancellationToken).ConfigureAwait(false);
        CutoutPair cutouts = await remover.CutOutPairAsync(paired, cancellationToken).ConfigureAwait(false);

        return await gate.RunAsync(projectDirectory, async () =>
        {
            LoadedProjectResult current = await repository.LoadAsync(projectDirectory, calibration, CancellationToken.None).ConfigureAwait(false);
            Find(current.Project, blueprintId, candidateId);

            CutoutFiles files = await repository
                .WriteCutoutImagesAsync(projectDirectory, candidateId, cutouts.FrontPng, cutouts.BackPng, CancellationToken.None)
                .ConfigureAwait(false);

            Blueprint blueprint = BlueprintEditor.Find(current.Project, blueprintId);
            Blueprint updated = blueprint with
            {
                Candidates = [.. blueprint.Candidates.Select(each => each.Id == candidateId
                    ? each with { FrontImageFile = files.Front, BackImageFile = files.Back }
                    : each)],
            };

            Project saved = await repository
                .SaveAsync(projectDirectory, BlueprintEditor.Replace(current.Project, updated), CancellationToken.None)
                .ConfigureAwait(false);

            return new EditedProject(saved, BlueprintEditor.Find(saved, blueprintId), []);
        }, CancellationToken.None).ConfigureAwait(false);
    }

    private static Candidate Find(Project project, Guid blueprintId, Guid candidateId) =>
        BlueprintEditor.Find(project, blueprintId).Candidates.FirstOrDefault(each => each.Id == candidateId)
        ?? throw new BlueprintRuleException(
            BlueprintRuleCode.CandidateNotFound,
            $"The blueprint {blueprintId} has no candidate with the identifier {candidateId}.");

    /// <summary>The bytes of the paired image, or <c>CANDIDATE_NO_PAIRED_IMAGE</c>.</summary>
    /// <remarks>
    /// <para>
    /// A <c>Share</c> archive drops the paired image (DEC-050), and a file can
    /// be missing on disk; both leave nothing to cut out.
    /// </para>
    /// <para>
    /// <b>Bounded while it is read.</b> The archive bounds of C.9.3 allow an
    /// entry of gigabytes, and the decoder checks a header only once it holds
    /// the whole file. The copy stops, and refuses, as soon as it passes the
    /// bound — never after.
    /// </para>
    /// </remarks>
    private async Task<byte[]> ReadPairedAsync(string projectDirectory, Candidate candidate, CancellationToken cancellationToken)
    {
        Stream? stream = candidate.PairedImageFile is string paired
            ? await repository.OpenImageAsync(projectDirectory, paired, cancellationToken).ConfigureAwait(false)
            : null;

        if (stream is null)
        {
            throw new CutoutException(
                CutoutErrorCode.NoPairedImage,
                $"The candidate {candidate.Id} has no paired image to cut out: it was dropped by a Share archive, or is missing on disk.");
        }

        await using (stream)
        {
            using var copy = new MemoryStream();
            byte[] buffer = new byte[81920];
            int read;

            while ((read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                if (copy.Length + read > maxPairedImageBytes)
                {
                    throw new CutoutException(
                        CutoutErrorCode.ImageTooLarge,
                        $"The paired image of the candidate {candidate.Id} weighs more than the {maxPairedImageBytes} bytes a generator may send; it is not read further (MEN-005).");
                }

                copy.Write(buffer, 0, read);
            }

            return copy.ToArray();
        }
    }
}
