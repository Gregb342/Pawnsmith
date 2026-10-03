using Pawnsmith.Api.Contracts;
using Pawnsmith.Api.Errors;
using Pawnsmith.Api.Hosting;
using Pawnsmith.Application.Ports;
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

        routes.MapGet("/api/universes/{universe}/catalog", (string universe, Catalog catalog) =>
        {
            // Matched against the member names, exactly: Enum.TryParse would
            // also accept "0", and an address that changes meaning with its
            // spelling is the kind of leniency the rest of the project refuses.
            if (!string.Equals(universe, catalog.Universe.ToString(), StringComparison.Ordinal))
            {
                throw new ApiException(ApiCodes.UniverseNotFound);
            }

            return catalog.ToDto();
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
}
