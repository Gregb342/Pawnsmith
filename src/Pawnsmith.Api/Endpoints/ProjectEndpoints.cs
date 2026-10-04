using Pawnsmith.Api.Contracts;
using Pawnsmith.Api.Hosting;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Projects;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Api.Endpoints;

/// <summary>
/// The routes of projects: list, create, read, settings (§G.6). Each one calls
/// the repository and translates; none decides anything.
/// </summary>
public static class ProjectEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/projects", async (IProjectRepository repository, Calibration calibration, CancellationToken cancellationToken) =>
        {
            IReadOnlyList<ProjectListing> listings = await repository.ListAsync(calibration, cancellationToken);

            return listings.Select(listing => listing.ToDto()).ToList();
        });

        routes.MapPost("/api/projects", async (
            CreateProjectRequest request,
            IProjectRepository repository,
            Calibration calibration,
            GeneratorSetup generator,
            CancellationToken cancellationToken) =>
        {
            CreatedProjectResult created = await repository.CreateAsync(
                request.Name, request.Universe, request.Geometry, request.PaperFormat, cancellationToken);

            string folder = Path.GetFileName(created.Directory);

            // Read back rather than mapped from what was created, so that the
            // answer carries the diagnostics a load gives - an unknown paper
            // format, for instance (DEC-056).
            LoadedProjectResult loaded = await repository.LoadAsync(created.Directory, calibration, cancellationToken);

            return Results.Created($"/api/projects/{folder}", loaded.ToDto(folder, generator.FramingClause));
        });

        routes.MapGet("/api/projects/{folder}", async (
            string folder,
            PawnsmithSettings settings,
            IProjectRepository repository,
            Calibration calibration,
            GeneratorSetup generator,
            CancellationToken cancellationToken) =>
        {
            string directory = ProjectAccess.Directory(settings, folder);
            LoadedProjectResult loaded = await repository.LoadAsync(directory, calibration, cancellationToken);

            return loaded.ToDto(folder, generator.FramingClause);
        });

        routes.MapPut("/api/projects/{folder}/settings", async (
            string folder,
            ProjectSettingsRequest request,
            PawnsmithSettings settings,
            IProjectRepository repository,
            ProjectWriteGate gate,
            Calibration calibration,
            GeneratorSetup generator,
            CancellationToken cancellationToken) =>
        {
            string directory = ProjectAccess.Directory(settings, folder);

            // Load, change, save, behind the project's gate (DEC-086). No
            // lock, no warning: changing the style misaligns the candidates,
            // and the answer shows it at once (DEC-030, DEC-055).
            LoadedProjectResult saved = await gate.RunAsync(directory, async () =>
            {
                LoadedProjectResult loaded = await repository.LoadAsync(directory, calibration, cancellationToken);
                // DEC-112: the use case compares with the project it loaded, in
                // the gate; the save still never sees the previous state.
                await repository.SaveAsync(directory, ProjectSettingsEditor.Apply(loaded.Project, request.ToDomain()), cancellationToken);

                return await repository.LoadAsync(directory, calibration, cancellationToken);
            }, cancellationToken);

            return saved.ToDto(folder, generator.FramingClause);
        });

        // §I.8.2 - a new project, the blueprints without their proposals.
        routes.MapPost("/api/projects/{folder}/duplicate", async (
            string folder,
            DuplicateProjectRequest request,
            PawnsmithSettings settings,
            IProjectRepository repository,
            Calibration calibration,
            GeneratorSetup generator,
            CancellationToken cancellationToken) =>
        {
            string directory = ProjectAccess.Directory(settings, folder);

            CreatedProjectResult created = await new ProjectDuplication(repository)
                .DuplicateAsync(directory, calibration, request.Name, request.Style?.ToDomain(), cancellationToken);

            string copy = Path.GetFileName(created.Directory);
            LoadedProjectResult loaded = await repository.LoadAsync(created.Directory, calibration, cancellationToken);

            return Results.Created($"/api/projects/{copy}", loaded.ToDto(copy, generator.FramingClause));
        });
    }
}
