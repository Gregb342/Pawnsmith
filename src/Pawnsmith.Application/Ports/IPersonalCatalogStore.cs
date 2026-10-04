using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Application.Ports;

/// <summary>
/// Where the user's own catalogue entries are kept (DEC-107).
/// </summary>
/// <remarks>
/// Only the personal entries cross this port, grouped by key; the shipped
/// catalogue is read once at start-up and never written. The labels of a
/// personal parameter are not kept: the shipped parameter of the same key
/// names it on screen.
/// </remarks>
public interface IPersonalCatalogStore
{
    /// <summary>The personal entries of a universe; empty when the user never added one.</summary>
    Task<IReadOnlyList<CatalogParameter>> ReadAsync(Universe universe, CancellationToken cancellationToken);

    /// <summary>Replaces the personal entries of a universe, all at once.</summary>
    Task WriteAsync(Universe universe, IReadOnlyList<CatalogParameter> personal, CancellationToken cancellationToken);
}
