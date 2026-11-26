using System.Text;

using Microsoft.Net.Http.Headers;

using Pawnsmith.Api.Contracts;
using Pawnsmith.Api.Errors;
using Pawnsmith.Api.Hosting;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Prompts;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Api.Endpoints;

/// <summary>
/// The read-only routes the interface needs to offer choices: configuration,
/// catalogue, generator (§G.6).
/// </summary>
public static class ReferenceEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/configuration", (Calibration calibration) => calibration.ToDto());

        routes.MapGet("/api/universes/{universe}/catalog", (string universe, CatalogBook catalog) =>
        {
            RequireUniverse(universe, catalog);

            return catalog.Current.ToDto();
        });

        // §I.4.3 - the personal catalogue. The book validates and writes;
        // these routes only translate (G.0).
        routes.MapPost("/api/universes/{universe}/catalog/entries", async (
            string universe,
            CatalogEntryRequest request,
            CatalogBook catalog,
            CancellationToken cancellationToken) =>
        {
            RequireUniverse(universe, catalog);

            Catalog updated = await catalog.AddEntryAsync(request.Key, request.Value, request.Labels, request.Fragment, cancellationToken);

            return Results.Created($"/api/universes/{universe}/catalog", updated.ToDto());
        });

        routes.MapDelete("/api/universes/{universe}/catalog/entries/{key}/{value}", async (
            string universe,
            string key,
            string value,
            CatalogBook catalog,
            CancellationToken cancellationToken) =>
        {
            RequireUniverse(universe, catalog);

            Catalog updated = await catalog.RemoveEntryAsync(key, value, cancellationToken);

            return updated.ToDto();
        });

        // §I.7.2 - the style library. Applying a style is a settings update.
        routes.MapGet("/api/universes/{universe}/styles", (string universe, CatalogBook catalog, StyleBook styles) =>
        {
            RequireUniverse(universe, catalog);

            return styles.Current.ToDto();
        });

        routes.MapPost("/api/universes/{universe}/styles", async (
            string universe,
            StylePresetRequest request,
            CatalogBook catalog,
            StyleBook styles,
            CancellationToken cancellationToken) =>
        {
            RequireUniverse(universe, catalog);

            IReadOnlyList<StylePreset> updated = await styles.AddAsync(request.Name, request.StyleClause, request.NegativeClause, cancellationToken);

            return Results.Created($"/api/universes/{universe}/styles", updated.ToDto());
        });

        routes.MapDelete("/api/universes/{universe}/styles/{id}", async (
            string universe,
            string id,
            CatalogBook catalog,
            StyleBook styles,
            CancellationToken cancellationToken) =>
        {
            RequireUniverse(universe, catalog);

            return (await styles.RemoveAsync(id, cancellationToken)).ToDto();
        });

        routes.MapGet("/api/generator", (GeneratorHolder generators, CancellationToken cancellationToken) =>
            StateOf(generators.Current, generators, cancellationToken));

        // DEC-108 - the address is set from the interface. Validated, saved,
        // then applied; the answer is the new state, checked live.
        routes.MapPut("/api/generator", async (GeneratorAddressRequest request, GeneratorHolder generators, CancellationToken cancellationToken) =>
        {
            GeneratorSetup setup = await generators.ReplaceAsync(request.Address, cancellationToken);

            return await StateOf(setup, generators, cancellationToken);
        });

        // DEC-119, DEC-120 - the workflow is imported from the interface: a
        // raw ComfyUI export, prepared here, or a file already prepared. The
        // body is the file itself, as JSON - no form, no envelope - bounded
        // like every request body (Pawnsmith:MaxRequestBytes).
        routes.MapPut("/api/generator/workflow", async (HttpContext context, GeneratorHolder generators, CancellationToken cancellationToken) =>
        {
            // JSON only: a type no HTML form can send, so a forged form from
            // another site cannot reach here (§G.11.1).
            if (!MediaTypeHeaderValue.TryParse(context.Request.ContentType, out MediaTypeHeaderValue? type)
                || !string.Equals(type.MediaType.Value, "application/json", StringComparison.OrdinalIgnoreCase))
            {
                throw new ApiException(ApiCodes.RequestInvalid);
            }

            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
            string json = await reader.ReadToEndAsync(cancellationToken);

            GeneratorSetup setup = await generators.ImportWorkflowAsync(json, cancellationToken);

            return await StateOf(setup, generators, cancellationToken);
        });

        routes.MapDelete("/api/generator/workflow", async (GeneratorHolder generators, CancellationToken cancellationToken) =>
        {
            GeneratorSetup setup = await generators.RemoveImportedWorkflowAsync(cancellationToken);

            return await StateOf(setup, generators, cancellationToken);
        });
    }

    /// <summary>
    /// The state of a setup, checked live when there is something to check:
    /// an absent generator is a state, never an exception (§E.7.1).
    /// </summary>
    private static async Task<GeneratorDto> StateOf(GeneratorSetup setup, GeneratorHolder generators, CancellationToken cancellationToken)
    {
        GeneratorAvailability? availability = setup.Generator is null
            ? null
            : await setup.Generator.CheckAsync(cancellationToken);

        return setup.ToDto(availability, generators.WorkflowSource);
    }

    // Matched against the member names, exactly: Enum.TryParse would also
    // accept "0", and an address that changes meaning with its spelling is the
    // kind of leniency the rest of the project refuses.
    private static void RequireUniverse(string universe, CatalogBook catalog)
    {
        if (!string.Equals(universe, catalog.Current.Universe.ToString(), StringComparison.Ordinal))
        {
            throw new ApiException(ApiCodes.UniverseNotFound);
        }
    }
}
