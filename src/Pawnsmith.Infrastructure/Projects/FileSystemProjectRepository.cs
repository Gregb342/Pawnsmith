using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// The one adapter behind <see cref="IProjectRepository"/>: plain folders on a
/// disk.
/// </summary>
/// <remarks>
/// <para>
/// <b>It holds no rule of its own.</b> Every one of the five operations already
/// exists as its own type — the creator, the reader, the saver, the exporter,
/// the importer — and this only puts one door in front of them and maps the
/// results across the layer boundary. Anything that started deciding here would
/// be a rule with two homes.
/// </para>
/// <para>
/// <b>Why five types and not one class from the start.</b> Each of them is a
/// separate reading: the reader is the nine steps of C.7.1, the importer the
/// eight of C.9.1, and putting them in one file would have made a thousand-line
/// class nobody reviews in one sitting — the exact failure DEC-027 exists to
/// prevent. Composition is what makes that division free.
/// </para>
/// <para>
/// The mapping between the two sides is written out by hand, as DEC-021 requires
/// everywhere. It is four lines and could have been avoided by letting
/// Application reference the infrastructure enumeration — which is precisely the
/// arrow A.3 forbids, and the reason those four lines are cheap at the price.
/// </para>
/// </remarks>
public sealed class FileSystemProjectRepository : IProjectRepository
{
    private readonly ProjectCreator creator;
    private readonly ProjectReader reader;
    private readonly ProjectSaver saver;
    private readonly ProjectExporter exporter;
    private readonly ProjectImporter importer;

    /// <param name="options">Where projects live, and the resource bounds (DEC-057).</param>
    /// <param name="clock">
    /// Where "now" comes from, for <c>createdAt</c> and <c>modifiedAt</c>.
    /// Injected so a test can pin the instant rather than assert a timestamp is
    /// "roughly now".
    /// </param>
    public FileSystemProjectRepository(ProjectRepositoryOptions options, TimeProvider? clock = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        creator = new ProjectCreator(options, clock);
        reader = new ProjectReader(options);
        saver = new ProjectSaver(clock);
        exporter = new ProjectExporter(clock);
        importer = new ProjectImporter(options);
    }

    public async Task<CreatedProjectResult> CreateAsync(
        string name,
        Universe universe,
        Geometry geometry,
        string paperFormatName,
        CancellationToken cancellationToken)
    {
        CreatedProject created = await creator
            .CreateAsync(name, universe, geometry, paperFormatName, cancellationToken)
            .ConfigureAwait(false);

        return new CreatedProjectResult(created.Project, created.Directory);
    }

    public async Task<LoadedProjectResult> LoadAsync(
        string projectDirectory,
        Calibration calibration,
        CancellationToken cancellationToken)
    {
        LoadedProject loaded = await reader
            .LoadAsync(projectDirectory, calibration, cancellationToken)
            .ConfigureAwait(false);

        return new LoadedProjectResult(loaded.Project, ToMismatches(loaded.Diagnostics));
    }

    public Task<Project> SaveAsync(
        string projectDirectory,
        Project project,
        CancellationToken cancellationToken) =>
        saver.SaveAsync(projectDirectory, project, cancellationToken);

    public Task<string> ExportArchiveAsync(
        string projectDirectory,
        ArchiveProfileKind profile,
        string destinationDirectory,
        CancellationToken cancellationToken) =>
        exporter.ExportAsync(
            projectDirectory,
            ToProfile(profile),
            destinationDirectory,
            cancellationToken);

    public async Task<ImportedProjectResult> ImportArchiveAsync(
        string archivePath,
        string name,
        Calibration calibration,
        CancellationToken cancellationToken)
    {
        ImportedProject imported = await importer
            .ImportAsync(archivePath, name, calibration, cancellationToken)
            .ConfigureAwait(false);

        return new ImportedProjectResult(
            imported.Project,
            imported.Directory,
            ToMismatches(imported.Diagnostics));
    }

    /// <summary>The application's profile, as this adapter's own.</summary>
    /// <remarks>
    /// Exhaustive on purpose, with no default arm: adding a profile without
    /// deciding what it means here fails the build rather than silently becoming
    /// a <c>Backup</c>.
    /// </remarks>
    private static ArchiveProfile ToProfile(ArchiveProfileKind profile) => profile switch
    {
        ArchiveProfileKind.Backup => ArchiveProfile.Backup,
        ArchiveProfileKind.Share => ArchiveProfile.Share,
        _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "No archive profile for this value."),
    };

    /// <summary>
    /// The diagnostics, carried across the boundary without their infrastructure
    /// type.
    /// </summary>
    /// <remarks>
    /// The kind travels as its name rather than as a second enumeration mirrored
    /// in Application. It is a compromise and worth naming: a string loses the
    /// compiler's help at the far end. The alternative — a parallel enumeration
    /// and a mapping to maintain — costs more than it buys while the only
    /// consumers are a command line and a test, and T6 will want a richer
    /// representation than either anyway (DEC-056 leaves that open).
    /// </remarks>
    private static IReadOnlyList<ProjectMismatch> ToMismatches(
        IReadOnlyList<ProjectDiagnostic> diagnostics) =>
        [.. diagnostics.Select(diagnostic => new ProjectMismatch(
            diagnostic.Kind.ToString(),
            diagnostic.Field,
            diagnostic.Message))];
}
