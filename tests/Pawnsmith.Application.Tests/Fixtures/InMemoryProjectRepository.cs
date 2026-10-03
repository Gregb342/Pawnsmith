using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Application.Tests.Fixtures;

/// <summary>
/// A repository that keeps one project in memory, written by hand for the
/// Application tests.
/// </summary>
/// <remarks>
/// <para>
/// Application cannot reference Infrastructure (A.3), so its tests cannot use
/// the real repository; the end-to-end test of E.12 n° 42 does that, on the
/// Infrastructure side. This fake only does what the batch needs — load, save,
/// write an image — and refuses the rest loudly rather than pretending.
/// </para>
/// <para>
/// No mocking library: CLAUDE.md §3 forbids Moq, and a fake of forty lines that
/// can be read is worth more than a configured substitute that cannot.
/// </para>
/// </remarks>
internal sealed class InMemoryProjectRepository : IProjectRepository
{
    private readonly object gate = new();
    private Project project;

    public InMemoryProjectRepository(Project project)
    {
        this.project = project;
    }

    /// <summary>The project as last saved.</summary>
    public Project Project
    {
        get
        {
            lock (gate)
            {
                return project;
            }
        }
    }

    /// <summary>Every image written, by relative path, in order.</summary>
    public List<(string Path, byte[] Png)> Images { get; } = [];

    /// <summary>How many times the project was saved.</summary>
    public int Saves { get; private set; }

    /// <summary>When set, the next save throws this instead of saving.</summary>
    public Exception? FailNextSave { get; set; }

    /// <summary>Changes the stored project as another writer would, between two operations of the batch.</summary>
    public void EditBehindTheBatch(Func<Project, Project> edit)
    {
        lock (gate)
        {
            project = edit(project);
        }
    }

    // Every operation honours its token, as the real one does: otherwise a test
    // could not tell a save made under the batch's token from one made under
    // none, and the rule "an image received is always kept" would go unproven.
    public Task<LoadedProjectResult> LoadAsync(string projectDirectory, Calibration calibration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new LoadedProjectResult(Project, []));
    }

    public Task<Project> SaveAsync(string projectDirectory, Project project, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (FailNextSave is { } failure)
        {
            FailNextSave = null;
            throw failure;
        }

        lock (gate)
        {
            this.project = project;
            Saves++;
        }

        return Task.FromResult(project);
    }

    public Task<string> WritePairedImageAsync(string projectDirectory, Guid candidateId, byte[] png, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string path = $"images/{candidateId}-pair.png";
        Images.Add((path, png));
        return Task.FromResult(path);
    }

    public Task<CreatedProjectResult> CreateAsync(string name, Universe universe, Geometry geometry, string paperFormatName, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not used by the batch.");

    public Task<string> ExportArchiveAsync(string projectDirectory, ArchiveProfileKind profile, string destinationDirectory, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not used by the batch.");

    public Task<ImportedProjectResult> ImportArchiveAsync(string archivePath, string name, Calibration calibration, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Not used by the batch.");
}
