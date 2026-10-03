using Microsoft.AspNetCore.Http.Features;

using Pawnsmith.Api.Contracts;
using Pawnsmith.Api.Errors;
using Pawnsmith.Api.Hosting;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Projects;
using Pawnsmith.Domain.PhysicalValues;

namespace Pawnsmith.Api.Endpoints;

/// <summary>Export and import of project archives (§G.9).</summary>
public static class ArchiveEndpoints
{
    /// <summary>The one media type the import accepts.</summary>
    private const string Zip = "application/zip";

    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/projects/{folder}/archives", async (
            HttpContext context,
            string folder,
            string? profile,
            PawnsmithSettings settings,
            IProjectRepository repository,
            ProjectWriteGate gate,
            CancellationToken cancellationToken) =>
        {
            string directory = ProjectAccess.Directory(settings, folder);
            ArchiveProfileKind kind = ParseProfile(profile);

            // Outside every projects root, as the exporter requires: a fresh
            // folder in the system's temporary space, deleted once the
            // response is sent.
            string staging = Path.Combine(Path.GetTempPath(), "pawnsmith-exports", Guid.NewGuid().ToString("N"));
            context.Response.OnCompleted(() =>
            {
                DeleteQuietly(staging);
                return Task.CompletedTask;
            });

            // Behind the gate: a blueprint removed halfway through would take
            // away files the archive was about to read.
            string archive = await gate.RunAsync(
                directory,
                () => repository.ExportArchiveAsync(directory, kind, staging, cancellationToken),
                cancellationToken);

            return Results.Stream(File.OpenRead(archive), Zip, Path.GetFileName(archive));
        });

        routes.MapPost("/api/projects/import", async (
            HttpContext context,
            string? name,
            PawnsmithSettings settings,
            IProjectRepository repository,
            Calibration calibration,
            GeneratorSetup generator,
            CancellationToken cancellationToken) =>
        {
            // The raw body, not a multipart form: simpler, and a type no HTML
            // form can send, so a forged form cannot reach here (§G.11).
            if (string.IsNullOrEmpty(name)
                || !string.Equals(context.Request.ContentType, Zip, StringComparison.OrdinalIgnoreCase))
            {
                throw new ApiException(ApiCodes.RequestInvalid);
            }

            // The only route allowed a large body, and only up to its bound,
            // enforced by the server while the archive arrives.
            if (context.Features.Get<IHttpMaxRequestBodySizeFeature>() is { IsReadOnly: false } limit)
            {
                limit.MaxRequestBodySize = settings.MaxUploadBytes;
            }

            string received = Path.Combine(Path.GetTempPath(), "pawnsmith-imports", $"{Guid.NewGuid():N}.zip");
            Directory.CreateDirectory(Path.GetDirectoryName(received)!);

            try
            {
                await using (FileStream file = File.Create(received))
                {
                    await context.Request.Body.CopyToAsync(file, cancellationToken);
                }

                // Every rule of C.9 - whitelist, bounds, zip slip, atomicity -
                // is the importer's; nothing is checked twice here.
                ImportedProjectResult imported = await repository.ImportArchiveAsync(received, name, calibration, cancellationToken);
                string folder = Path.GetFileName(imported.Directory);

                return Results.Created(
                    $"/api/projects/{folder}",
                    new LoadedProjectResult(imported.Project, imported.Diagnostics).ToDto(folder, generator.FramingClause));
            }
            finally
            {
                DeleteQuietly(received);
            }
        });
    }

    /// <summary>The profile, by its exact name.</summary>
    private static ArchiveProfileKind ParseProfile(string? profile) => profile switch
    {
        "Backup" => ArchiveProfileKind.Backup,
        "Share" => ArchiveProfileKind.Share,
        _ => throw new ApiException(ApiCodes.RequestInvalid),
    };

    private static void DeleteQuietly(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
            else if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // A temporary file the system will clean up; not worth an error.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
