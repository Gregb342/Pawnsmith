using System.Text.Json.Serialization;

using Pawnsmith.Api.Endpoints;
using Pawnsmith.Api.Errors;
using Pawnsmith.Api.Jobs;
using Pawnsmith.Application.Generation;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Projects;
using Pawnsmith.Application.Prompts;
using Pawnsmith.Application.Sheets;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;
using Pawnsmith.Infrastructure.Imaging;
using Pawnsmith.Infrastructure.Json;
using Pawnsmith.Infrastructure.Pdf;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Prompts;

namespace Pawnsmith.Api.Hosting;

/// <summary>
/// Builds the application: reads its files, composes its services, wires its
/// routes (§G.2).
/// </summary>
/// <remarks>
/// <para>
/// <b>Everything is registered by hand, one line per service.</b> No assembly
/// scanning, no convention: §G.0 forbids implicit wiring for the same reason
/// DEC-021 forbids implicit mapping — a reviewer must be able to read, here,
/// what the application is made of.
/// </para>
/// <para>
/// A method rather than top-level statements in <c>Program.cs</c>, so that the
/// tests start the very same host on a free local port, with their own folders
/// and their own generator (§G.13) — without a test package to do it.
/// </para>
/// </remarks>
public static class ApiHost
{
    /// <summary>The names a request may be addressed to when nothing else is configured.</summary>
    public const string DefaultAllowedHosts = "localhost;127.0.0.1;[::1]";

    /// <summary>Builds the host, reading every file the application needs before it accepts a request.</summary>
    /// <param name="args">Command-line arguments, which can also override settings.</param>
    /// <param name="replaceServices">
    /// For the tests only: runs after the registrations, to swap a service —
    /// the generator, typically. Null in production.
    /// </param>
    /// <exception cref="Exception">
    /// The calibration, the catalogue or the prompt template cannot be read:
    /// the application does not start (§G.2.2). A generator that is wrong does
    /// not prevent it.
    /// </exception>
    public static async Task<WebApplication> BuildAsync(string[] args, Action<IServiceCollection>? replaceServices = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        // MEN-010 - restricted by default in the code itself, not only in
        // appsettings.json: a deployment that lost the file, or a content root
        // pointed elsewhere, must not silently accept any Host header and so
        // reopen DNS rebinding. An operator who publishes the application on
        // their network writes their own list, knowingly (DEC-089).
        if (string.IsNullOrWhiteSpace(builder.Configuration["AllowedHosts"]))
        {
            builder.Configuration["AllowedHosts"] = DefaultAllowedHosts;
        }

        var settings = PawnsmithSettings.From(builder.Configuration, builder.Environment.ContentRootPath);

        // Read before the first request, so that a broken file stops the
        // start-up with its message rather than failing the first user.
        Calibration calibration = await CalibrationReader
            .ReadAsync(Path.Combine(settings.ConfigDirectory, "calibration.json"), CancellationToken.None);

        // One universe in v1 (DEC-025); TemplatePromptComposer holds one.
        string universeFile = Universe.Fantasy.ToString().ToLowerInvariant();

        Catalog catalog = await CatalogReader.ReadAsync(
            Path.Combine(settings.ConfigDirectory, $"catalog.{universeFile}.json"), Universe.Fantasy, CancellationToken.None);

        PromptTemplate template = await PromptTemplateReader.ReadAsync(
            Path.Combine(settings.ConfigDirectory, $"prompt-template.{universeFile}.json"), Universe.Fantasy, CancellationToken.None);

        GeneratorSetup generator = await GeneratorSetup.LoadAsync(settings, CancellationToken.None);

        Register(builder.Services, settings, calibration, catalog, template, generator);
        replaceServices?.Invoke(builder.Services);

        // Every body is bounded by the server while it arrives, not after it
        // has been held in memory. The bound is small - a JSON request is a
        // few hundred bytes, and an array of a hundred million seeds must not
        // fit (MEN-007) - and the import raises it for itself alone (§G.9).
        builder.WebHost.ConfigureKestrel(kestrel => kestrel.Limits.MaxRequestBodySize = settings.MaxRequestBytes);

        WebApplication app = builder.Build();
        Configure(app);

        return app;
    }

    private static void Register(
        IServiceCollection services,
        PawnsmithSettings settings,
        Calibration calibration,
        Catalog catalog,
        PromptTemplate template,
        GeneratorSetup generator)
    {
        services.AddSingleton(settings);
        services.AddSingleton(calibration);
        services.AddSingleton(catalog);
        services.AddSingleton<IPromptComposer>(new TemplatePromptComposer(template, catalog));

        // A factory rather than the instance, so that the container disposes
        // the HTTP client of the generator when the application stops.
        services.AddSingleton(_ => generator);

        services.AddSingleton<IProjectRepository>(new FileSystemProjectRepository(new ProjectRepositoryOptions(settings.ProjectsRoot)));
        services.AddSingleton(new ProjectWriteGate());
        services.AddSingleton(new GenerationOptions());
        services.AddSingleton<IImageSizeReader>(new FileImageSizeReader());
        services.AddSingleton(provider => new ProjectSheet(
            provider.GetRequiredService<IProjectRepository>(),
            provider.GetRequiredService<IImageSizeReader>(),
            directory => new PdfSharpSheetRenderer(directory)));

        services.AddSingleton<BlueprintEndpoints.Edit>();
        services.AddSingleton(new JobRegistry());
        services.AddHostedService<GenerationWorker>();

        services.ConfigureHttpJsonOptions(options =>
        {
            // Enumerations by name, never by rank (§G.4): a rank changes when a
            // member is inserted, a name only by decision. Unknown names are
            // refused, and so are numbers.
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));

            // A request that omits a field, or sends null where the record
            // says it cannot be null, is refused as REQUEST_INVALID rather
            // than reaching a use case as a null it never expected.
            options.SerializerOptions.RespectNullableAnnotations = true;
            options.SerializerOptions.RespectRequiredConstructorParameters = true;
        });

        // A body that does not bind throws, so that the error middleware gives
        // it its code; by default the framework would answer an empty 400.
        services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);
    }

    private static void Configure(WebApplication app)
    {
        // Outermost: everything below that throws is turned into a code.
        app.UseMiddleware<ErrorMiddleware>();
        app.UseMiddleware<OriginGuard>();

        // A.6 - the API serves the compiled front, from the same origin; there
        // is deliberately no CORS configuration.
        app.UseDefaultFiles();
        app.UseStaticFiles();

        ReferenceEndpoints.Map(app);
        ProjectEndpoints.Map(app);
        BlueprintEndpoints.Map(app);
        JobEndpoints.Map(app);
        SheetEndpoints.Map(app);
        ArchiveEndpoints.Map(app);

        // An /api route that does not exist answers with a code, not with the
        // page of the front: a client calling a wrong route must not receive
        // HTML with a 200.
        app.Map("/api/{**rest}", (HttpContext _) => Results.Json(
            new ErrorMiddleware.ErrorBody(ApiCodes.RouteNotFound),
            statusCode: StatusCodes.Status404NotFound));

        // Single-page application: any other unknown path is handed back to
        // index.html so the client-side router, when there is one, resolves it.
        app.MapFallbackToFile("index.html");
    }
}
