namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// Turns a project document into the one a <c>Share</c> archive carries.
/// </summary>
/// <remarks>
/// <para>
/// <b>The <c>Share</c> profile filters <c>project.json</c>; it does not merely
/// leave files out.</b> That is the point worth writing down (DEC-050). Omitting
/// the files while keeping the references would produce an archive whose project
/// points at images that are not there — a half-broken state the import would
/// have to tolerate, and a tolerance that would then spread everywhere.
/// </para>
/// <para>
/// The invariant is simpler and stronger: <b>an archive always holds a
/// <c>project.json</c> consistent with the files it contains. No dangling
/// reference, in any profile.</b>
/// </para>
/// </remarks>
public static class ShareFilter
{
    /// <summary>The document to write into a <c>Share</c> archive.</summary>
    /// <remarks>
    /// Four rules, each with its reason.
    /// <list type="bullet">
    /// <item>
    /// <b>Every blueprint is kept</b>, including those with no candidate — an
    /// empty blueprint is a perfectly normal state and says what the sender
    /// intends to make.
    /// </item>
    /// <item>
    /// <b><c>Rejected</c> candidates go</b>, with their files. They are the
    /// sender's arbitration, not the recipient's, and they carry the bulk of the
    /// weight.
    /// </item>
    /// <item>
    /// <b><c>Draft</c> candidates stay.</b> A project shared while the choice is
    /// still open needs its drafts — that is often the very reason for sharing.
    /// </item>
    /// <item>
    /// <b><c>pairedImageFile</c> becomes <c>null</c> everywhere.</b> The raw
    /// two-view image is 1 to 2 MB and only ever useful for diagnosing its own
    /// split, which is the sender's problem.
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="ProjectException">
    /// An elected candidate would be filtered out. Impossible under the business
    /// rules expected of T3, but the file can have been edited by hand, and
    /// producing a dangling reference is worse than refusing.
    /// </exception>
    public static ProjectDocument Apply(ProjectDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        List<BlueprintDocument> blueprints = [];

        for (int index = 0; index < document.Blueprints.Count; index++)
        {
            blueprints.Add(Filter(document.Blueprints[index], $"blueprints[{index}]"));
        }

        return document with { Blueprints = blueprints };
    }

    private static BlueprintDocument Filter(BlueprintDocument blueprint, string field)
    {
        List<CandidateDocument> kept = [.. blueprint.Candidates
            .Where(candidate => !string.Equals(
                candidate.Status,
                nameof(Domain.Projects.CandidateStatus.Rejected),
                StringComparison.Ordinal))
            .Select(candidate => candidate with { PairedImageFile = null })];

        if (blueprint.ElectedCandidateId is { } elected
            && !kept.Any(candidate => string.Equals(candidate.Id, elected, StringComparison.Ordinal)))
        {
            throw new ProjectException(
                ProjectErrorCode.ArchiveExportFailed,
                $"The elected candidate of {field} is rejected, so filtering it out would leave " +
                "the archive pointing at a candidate it does not contain. An archive never holds " +
                "a dangling reference, so the export refuses rather than producing one.");
        }

        return blueprint with { Candidates = kept };
    }

    /// <summary>Every image file a document still refers to, as stored paths.</summary>
    /// <remarks>
    /// Used by the exporter to build its whitelist: what travels is exactly what
    /// the archive's own <c>project.json</c> names, which is what makes the
    /// invariant above hold by construction rather than by care. An orphan file
    /// sitting in <c>images/</c> — left by a deleted candidate, or dropped there
    /// by hand — has no reason to travel, and this is why it does not.
    /// </remarks>
    public static IReadOnlyList<string> ReferencedImages(ProjectDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return
        [
            .. document.Blueprints
                .SelectMany(blueprint => blueprint.Candidates)
                .SelectMany(candidate => new[]
                {
                    candidate.PairedImageFile,
                    candidate.FrontImageFile,
                    candidate.BackImageFile,
                })
                .OfType<string>()
                .Distinct(StringComparer.Ordinal),
        ];
    }
}
