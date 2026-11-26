using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Projects;

/// <summary>
/// A new project from an existing one: its blueprints without their proposals,
/// in the style asked for (§I.8.2, DEC-112).
/// </summary>
/// <remarks>
/// <para>
/// <b>What is copied</b>: universe, geometry, paper format, tab dimensions,
/// and each blueprint's fields and subject clause. <b>What is not</b>: the
/// proposals, the elections and every file of <c>images/</c>. The new project
/// has its own identifier and its own folder, created like any other
/// (C.3.2): a name already taken is suffixed.
/// </para>
/// <para>
/// Each blueprint keeps its identifier. Identifiers are unique within a
/// project, and two projects never meet; keeping them lets a reader of both
/// files see which blueprint came from which.
/// </para>
/// </remarks>
public sealed class ProjectDuplication(IProjectRepository repository)
{
    /// <param name="sourceDirectory">The project to copy from; only read.</param>
    /// <param name="calibration">Needed to load the source (DEC-053).</param>
    /// <param name="name">The new project's name.</param>
    /// <param name="style">The new project's style; <c>null</c> keeps the source's.</param>
    /// <returns>The new project, created and saved.</returns>
    public async Task<CreatedProjectResult> DuplicateAsync(
        string sourceDirectory,
        Calibration calibration,
        string name,
        Style? style,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceDirectory);
        ArgumentNullException.ThrowIfNull(calibration);
        ArgumentNullException.ThrowIfNull(name);

        LoadedProjectResult source = await repository.LoadAsync(sourceDirectory, calibration, cancellationToken).ConfigureAwait(false);
        Project from = source.Project;

        CreatedProjectResult created = await repository
            .CreateAsync(name, from.Universe, from.Geometry, from.PaperFormatName, cancellationToken)
            .ConfigureAwait(false);

        Project copy = created.Project with
        {
            Style = style ?? from.Style,
            CalibrationOverrides = from.CalibrationOverrides,
            Blueprints = [.. from.Blueprints.Select(blueprint => blueprint with
            {
                Candidates = [],
                ElectedCandidateId = null,
            })],
        };

        Project saved = await repository.SaveAsync(created.Directory, copy, cancellationToken).ConfigureAwait(false);

        return created with { Project = saved };
    }
}
