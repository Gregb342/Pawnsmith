using Pawnsmith.Api.Contracts;
using Pawnsmith.Api.Errors;
using Pawnsmith.Api.Hosting;
using Pawnsmith.Application.Blueprints;
using Pawnsmith.Application.Generation;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Projects;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Api.Endpoints;

/// <summary>
/// The routes of blueprints, candidates and images (§G.6). Each one runs the
/// use case of T3 — or the judgement of G.6.1 — behind the project's write
/// gate, and translates. None decides anything.
/// </summary>
public static class BlueprintEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/projects/{folder}/blueprints", (string folder, BlueprintFieldsRequest request, Edit edit, IPromptComposer composer) =>
            edit.RunAsync(folder, project => BlueprintEditor.Add(project, request.ToDomain(), composer)));

        routes.MapPut("/api/projects/{folder}/blueprints/{id:guid}", (string folder, Guid id, BlueprintFieldsRequest request, Edit edit, IPromptComposer composer) =>
            edit.RunAsync(folder, project => BlueprintEditor.UpdateFields(project, id, request.ToDomain(), composer)));

        routes.MapPut("/api/projects/{folder}/blueprints/{id:guid}/subject-clause", (string folder, Guid id, SubjectClauseRequest request, Edit edit) =>
            edit.RunAsync(folder, project => BlueprintEditor.EditSubjectClause(project, id, request.Clause)));

        // DEC-109 - "back to the automatic text".
        routes.MapDelete("/api/projects/{folder}/blueprints/{id:guid}/subject-clause", (string folder, Guid id, Edit edit, IPromptComposer composer) =>
            edit.RunAsync(folder, project => BlueprintEditor.ResetSubjectClause(project, id, composer)));

        routes.MapPut("/api/projects/{folder}/blueprints/{id:guid}/election", (string folder, Guid id, ElectionRequest request, Edit edit) =>
            edit.RunAsync(folder, project => request.CandidateId is Guid candidateId
                ? CandidateElection.Elect(project, id, candidateId)
                : CandidateElection.Unelect(project, id)));

        // T5 (§F.7): cuts an existing candidate out, replacing its cut-outs.
        // The use case takes the write gate itself, around its save only.
        routes.MapPost("/api/projects/{folder}/blueprints/{id:guid}/candidates/{candidateId:guid}/cutout", async (
            string folder,
            Guid id,
            Guid candidateId,
            PawnsmithSettings settings,
            CandidateCutout cutout,
            Calibration calibration,
            GeneratorHolder generator,
            IPromptComposer composer,
            CancellationToken cancellationToken) =>
        {
            string directory = ProjectAccess.Directory(settings, folder);
            EditedProject edited = await cutout.CutOutAsync(directory, calibration, id, candidateId, cancellationToken);

            return edited.ToDto(new MappingContext(generator.Current.FramingClause, composer));
        });

        routes.MapPut("/api/projects/{folder}/blueprints/{id:guid}/candidates/{candidateId:guid}/status", (string folder, Guid id, Guid candidateId, StatusRequest request, Edit edit) =>
            edit.RunAsync(folder, project => CandidateJudgement.SetStatus(project, id, candidateId, request.Status)));

        routes.MapDelete("/api/projects/{folder}/blueprints/{id:guid}", async (
            string folder,
            Guid id,
            PawnsmithSettings settings,
            IProjectRepository repository,
            ProjectWriteGate gate,
            Calibration calibration,
            CancellationToken cancellationToken) =>
        {
            string directory = ProjectAccess.Directory(settings, folder);

            await gate.RunAsync(directory, async () =>
            {
                LoadedProjectResult loaded = await repository.LoadAsync(directory, calibration, cancellationToken);
                RemovedBlueprint removed = BlueprintRemoval.Remove(loaded.Project, id);

                // Model first, disk second (DEC-070): a save that fails leaves
                // the files and the project intact; a deletion that fails
                // leaves orphans, which are harmless.
                await repository.SaveAsync(directory, removed.Project, cancellationToken);
                return await repository.DeleteImagesAsync(directory, removed.ReferencedFiles, CancellationToken.None);
            }, cancellationToken);

            return Results.NoContent();
        });

        routes.MapGet("/api/projects/{folder}/images/{**fileName}", async (
            string folder,
            string fileName,
            PawnsmithSettings settings,
            IProjectRepository repository,
            Calibration calibration,
            CancellationToken cancellationToken) =>
        {
            string directory = ProjectAccess.Directory(settings, folder);
            LoadedProjectResult loaded = await repository.LoadAsync(directory, calibration, cancellationToken);

            // Only what a candidate references is served (DEC-088): images/
            // and the rest of the route must be stored on a candidate, exactly.
            string? referenced = ReferencedImages.Find(loaded.Project, fileName);

            Stream? image = referenced is null
                ? null
                : await repository.OpenImageAsync(directory, referenced, cancellationToken);

            return image is null
                ? throw new ApiException(ApiCodes.ImageNotFound)
                : Results.Stream(image, "image/png");
        });
    }

    /// <summary>
    /// The one shape every editing route shares: load, apply a use case, save,
    /// read back — behind the project's gate (DEC-086).
    /// </summary>
    /// <remarks>
    /// A class registered in the container rather than a helper taking six
    /// services: the routes stay one line each, and what they have in common
    /// is written once, here, where it can be read.
    /// </remarks>
    public sealed class Edit(
        PawnsmithSettings settings,
        IProjectRepository repository,
        ProjectWriteGate gate,
        Calibration calibration,
        GeneratorHolder generator,
        IPromptComposer composer)
    {
        /// <summary>Applies <paramref name="change"/> to the project of <paramref name="folder"/> and saves it.</summary>
        public async Task<EditedBlueprintDto> RunAsync(string folder, Func<Project, EditedProject> change)
        {
            string directory = ProjectAccess.Directory(settings, folder);

            return await gate.RunAsync(directory, async () =>
            {
                LoadedProjectResult loaded = await repository.LoadAsync(directory, calibration, CancellationToken.None);
                EditedProject edited = change(loaded.Project);

                Project saved = await repository.SaveAsync(directory, edited.Project, CancellationToken.None);

                return (edited with { Project = saved }).ToDto(new MappingContext(generator.Current.FramingClause, composer));
            }, CancellationToken.None);
        }
    }
}
