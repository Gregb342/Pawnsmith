using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Application.Prompts;

/// <summary>
/// The style library: the shipped styles, then the user's own (§I.7, DEC-110).
/// </summary>
/// <remarks>
/// The same shape as <see cref="CatalogBook"/>, for the same reasons: one
/// holder read by everyone, writes serialised by a semaphore, and validate,
/// write, then publish. Applying a style has no method here — the front sends
/// the project's settings with the style copied, and the rule that freezes the
/// style (DEC-112) applies there.
/// </remarks>
public sealed class StyleBook
{
    private readonly Universe universe;
    private readonly IReadOnlyList<StylePreset> shipped;
    private readonly IPersonalStyleStore store;
    private readonly SemaphoreSlim writes = new(1, 1);
    private IReadOnlyList<StylePreset> personal;
    private IReadOnlyList<StylePreset> current;

    private StyleBook(Universe universe, IReadOnlyList<StylePreset> shipped, IPersonalStyleStore store, IReadOnlyList<StylePreset> personal)
    {
        this.universe = universe;
        this.shipped = shipped;
        this.store = store;
        this.personal = personal;
        current = [.. shipped, .. personal];
    }

    /// <summary>The styles, shipped first, each list in its own order.</summary>
    public IReadOnlyList<StylePreset> Current => Volatile.Read(ref current);

    /// <summary>Reads the personal styles and puts them after the shipped ones.</summary>
    /// <remarks>
    /// A personal style whose identifier a shipped one now uses is set aside,
    /// for the reason <see cref="CatalogBook.Dropped"/> gives. Personal
    /// identifiers are generated, so it takes a hand-edited file to get there.
    /// </remarks>
    public static async Task<StyleBook> LoadAsync(
        Universe universe,
        IReadOnlyList<StylePreset> shipped,
        IPersonalStyleStore store,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipped);
        ArgumentNullException.ThrowIfNull(store);

        IReadOnlyList<StylePreset> stored = await store.ReadAsync(universe, cancellationToken).ConfigureAwait(false);

        HashSet<string> ids = new(shipped.Select(preset => preset.Id), StringComparer.Ordinal);
        List<StylePreset> kept = [];

        foreach (StylePreset preset in stored)
        {
            if (ids.Add(preset.Id))
            {
                kept.Add(preset with { Origin = StyleOrigin.Personal });
            }
        }

        return new StyleBook(universe, shipped, store, kept);
    }

    /// <summary>Saves a personal style.</summary>
    /// <param name="name">The user's name for it, in their language; shown under every culture.</param>
    /// <param name="styleClause">English, like every clause.</param>
    /// <param name="negativeClause">May be empty.</param>
    /// <returns>The library after the addition.</returns>
    /// <exception cref="StyleRuleException"><c>STYLE_INVALID</c>: an empty name or style clause.</exception>
    public async Task<IReadOnlyList<StylePreset>> AddAsync(
        string name,
        string styleClause,
        string negativeClause,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(styleClause);
        ArgumentNullException.ThrowIfNull(negativeClause);

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(styleClause))
        {
            throw new StyleRuleException(StyleRuleCode.StyleInvalid, "A style needs a name and a style clause.");
        }

        var preset = new StylePreset(
            Id: Guid.NewGuid().ToString("N"),
            Names: InterfaceCulture.All.ToDictionary(culture => culture, _ => name.Trim(), StringComparer.Ordinal),
            StyleClause: styleClause.Trim(),
            NegativeClause: negativeClause.Trim(),
            Origin: StyleOrigin.Personal);

        await writes.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await PublishAsync([.. personal, preset], cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writes.Release();
        }
    }

    /// <summary>Removes a personal style. Projects that copied it keep their copy.</summary>
    /// <returns>The library after the removal.</returns>
    /// <exception cref="StyleRuleException"><c>STYLE_NOT_FOUND</c> or <c>STYLE_SHIPPED</c>.</exception>
    public async Task<IReadOnlyList<StylePreset>> RemoveAsync(string id, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(id);

        await writes.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (shipped.Any(preset => string.Equals(preset.Id, id, StringComparison.Ordinal)))
            {
                throw new StyleRuleException(StyleRuleCode.StyleShipped, $"The style '{id}' is shipped with the application and is not removed from the interface.");
            }

            if (!personal.Any(preset => string.Equals(preset.Id, id, StringComparison.Ordinal)))
            {
                throw new StyleRuleException(StyleRuleCode.StyleNotFound, $"There is no style '{id}'.");
            }

            return await PublishAsync(
                [.. personal.Where(preset => !string.Equals(preset.Id, id, StringComparison.Ordinal))],
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writes.Release();
        }
    }

    private async Task<IReadOnlyList<StylePreset>> PublishAsync(List<StylePreset> next, CancellationToken cancellationToken)
    {
        // Checked before anything is written, like the catalogue.
        IReadOnlyList<StylePreset> all = StylePresets.Check([.. shipped, .. next]);

        await store.WriteAsync(universe, next, cancellationToken).ConfigureAwait(false);

        personal = next;
        Volatile.Write(ref current, all);

        return all;
    }
}
