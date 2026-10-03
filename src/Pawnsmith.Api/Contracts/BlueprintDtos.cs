using Pawnsmith.Api.Errors;
using Pawnsmith.Application.Blueprints;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Api.Contracts;

/// <summary>The fields of a blueprint the user fills in (§D.3, step 2).</summary>
/// <param name="OptionalParameters">An array of key and value, not an object: the order a client sends is kept, and a repeated key is refused rather than silently collapsed.</param>
public sealed record BlueprintFieldsRequest(
    string Race,
    string CharacterClass,
    Size Size,
    IReadOnlyList<ParameterDto> OptionalParameters,
    string Details,
    int Quantity);

public sealed record SubjectClauseRequest(string Clause);

/// <param name="CandidateId">The candidate to elect, or null to elect none.</param>
public sealed record ElectionRequest(Guid? CandidateId);

public sealed record StatusRequest(CandidateStatus Status);

/// <summary>A blueprint after a change, and what the composer said on the way — never a refusal (DEC-056).</summary>
public sealed record EditedBlueprintDto(BlueprintDto Blueprint, IReadOnlyList<CompositionDiagnosticDto> CompositionDiagnostics);

/// <summary>A catalogue that did not know a key or a value. The value is in the clause as written.</summary>
public sealed record CompositionDiagnosticDto(string Key, string Value);

/// <summary>The manual mappings of the blueprint routes (DEC-021).</summary>
public static class BlueprintMapping
{
    /// <exception cref="ApiException"><c>REQUEST_INVALID</c> when a key is repeated.</exception>
    public static BlueprintFields ToDomain(this BlueprintFieldsRequest request)
    {
        Dictionary<string, string> parameters = new(StringComparer.Ordinal);

        foreach (ParameterDto parameter in request.OptionalParameters)
        {
            // Shape, not a business rule: a request that names one key twice
            // has no single meaning to pass on.
            if (!parameters.TryAdd(parameter.Key, parameter.Value))
            {
                throw new ApiException(ApiCodes.RequestInvalid);
            }
        }

        return new BlueprintFields(
            request.Race,
            request.CharacterClass,
            request.Size,
            parameters,
            request.Details,
            request.Quantity);
    }

    public static EditedBlueprintDto ToDto(this EditedProject edited, Style style, string? framingClause) => new(
        edited.Blueprint.ToDto(style, framingClause),
        [.. edited.Diagnostics.Select(diagnostic => new CompositionDiagnosticDto(diagnostic.Key, diagnostic.Value))]);
}
