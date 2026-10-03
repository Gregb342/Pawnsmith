using Pawnsmith.Api.Hosting;
using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Api.Contracts;

/// <summary>What the interface needs to offer choices: formats, sizes, geometries, universes, cultures.</summary>
public sealed record ConfigurationDto(
    IReadOnlyList<PaperFormatDto> PaperFormats,
    IReadOnlyList<SizeDto> Sizes,
    IReadOnlyList<string> Geometries,
    IReadOnlyList<string> Universes,
    IReadOnlyList<string> Cultures);

public sealed record PaperFormatDto(string Name, double WidthMm, double HeightMm);

/// <summary>A size and its two independent dimensions — never derive one from the other (§3.1).</summary>
public sealed record SizeDto(Size Size, double GridFootprintMm, double PawnWidthMm, double PawnHeightMm);

/// <summary>The catalogue of a universe, in the order of its file (§D.4.5).</summary>
public sealed record CatalogDto(Universe Universe, IReadOnlyList<CatalogParameterDto> Parameters);

public sealed record CatalogParameterDto(string Key, IReadOnlyList<CatalogEntryDto> Entries);

public sealed record CatalogEntryDto(string Value, string Fragment);

/// <summary>The state of the generator, as shown — never changed — by the API (DEC-081).</summary>
/// <param name="State"><c>Available</c>, <c>Unreachable</c>, <c>Unhealthy</c>, <c>NotConfigured</c> or <c>Misconfigured</c>.</param>
/// <param name="Code">Why it is misconfigured.</param>
/// <param name="Address">The configured address.</param>
/// <param name="FramingClause">The framing clause of the workflow, read-only (DEC-029).</param>
public sealed record GeneratorDto(string State, string? Code, string? Address, string? FramingClause);

/// <summary>The manual mappings of these DTOs (DEC-021): one method per type, nothing inferred.</summary>
public static class ReferenceMapping
{
    /// <summary>The cultures a sheet can be printed in (chapter 10).</summary>
    public static readonly IReadOnlyList<string> Cultures = ["en", "fr"];

    public static ConfigurationDto ToDto(this Calibration calibration) => new(
        // Sorted by name, ordinal: a dictionary has no order of its own, and
        // two identical calls must give identical bodies (§G.4).
        PaperFormats: [.. calibration.PaperFormats.Values
            .OrderBy(format => format.Name, StringComparer.Ordinal)
            .Select(format => new PaperFormatDto(format.Name, format.WidthMm, format.HeightMm))],

        // In the enumeration's order, Small to Gargantuan.
        Sizes: [.. Enum.GetValues<Size>()
            .Where(calibration.Sizes.ContainsKey)
            .Select(size => new SizeDto(
                size,
                calibration.Sizes[size].GridFootprintMm,
                calibration.Sizes[size].PawnWidthMm,
                calibration.Sizes[size].PawnHeightMm))],
        Geometries: Enum.GetNames<Geometry>(),
        Universes: Enum.GetNames<Universe>(),
        Cultures: Cultures);

    public static CatalogDto ToDto(this Catalog catalog) => new(
        catalog.Universe,
        [.. catalog.Parameters.Select(parameter => new CatalogParameterDto(
            parameter.Key,
            [.. parameter.Entries.Select(entry => new CatalogEntryDto(entry.Value, entry.Fragment))]))]);

    /// <summary>The generator's state: from the start-up setup, and from a live check when it is configured.</summary>
    public static GeneratorDto ToDto(this GeneratorSetup setup, GeneratorAvailability? availability) => new(
        State: setup.Configuration switch
        {
            GeneratorConfiguration.Configured => availability!.Value.ToString(),
            GeneratorConfiguration.NotConfigured => "NotConfigured",
            GeneratorConfiguration.Misconfigured => "Misconfigured",
            _ => throw new ArgumentOutOfRangeException(nameof(setup), setup.Configuration, "No state for this configuration."),
        },
        Code: setup.ErrorCode,
        Address: setup.Address,
        FramingClause: setup.FramingClause);
}
