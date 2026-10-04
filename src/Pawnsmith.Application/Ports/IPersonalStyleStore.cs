using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Application.Ports;

/// <summary>Where the user's own styles are kept (§I.7.2, DEC-110).</summary>
public interface IPersonalStyleStore
{
    /// <summary>The personal styles of a universe; empty when the user never saved one.</summary>
    Task<IReadOnlyList<StylePreset>> ReadAsync(Universe universe, CancellationToken cancellationToken);

    /// <summary>Replaces the personal styles of a universe, all at once.</summary>
    Task WriteAsync(Universe universe, IReadOnlyList<StylePreset> personal, CancellationToken cancellationToken);
}
