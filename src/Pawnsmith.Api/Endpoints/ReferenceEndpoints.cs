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

        routes.MapGet("/api/generator", async (GeneratorSetup setup, CancellationToken cancellationToken) =>
        {
            // A live check when there is something to check: an absent
            // generator is a state, never an exception (§E.7.1).
            GeneratorAvailability? availability = setup.Generator is null
                ? null
                : await setup.Generator.CheckAsync(cancellationToken);

            return setup.ToDto(availability);
        });
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
