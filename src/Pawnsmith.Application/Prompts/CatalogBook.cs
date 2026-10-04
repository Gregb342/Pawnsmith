using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Prompts;

namespace Pawnsmith.Application.Prompts;

/// <summary>
/// The catalogue the application serves: the shipped one, followed by the
/// user's complete entries (§I.4, DEC-107).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a holder and not a plain <see cref="Catalog"/>.</b> Until T6 the
/// catalogue was read once and never changed, so every service could hold the
/// instance. Personal entries change it while the application runs. The
/// composer, the routes and this class all read <see cref="Current"/>, and a
/// change replaces it in one assignment: a reader sees the catalogue before or
/// after a change, never half of one.
/// </para>
/// <para>
/// <b>Writes are serialised</b> by a semaphore — a lock that can be awaited.
/// Two additions arriving together would otherwise each start from the same
/// list, and the second write would erase the first.
/// </para>
/// <para>
/// <b>Validate, write, then publish.</b> The new catalogue is built and checked
/// first; the file is written; only then does <see cref="Current"/> change. A
/// write that fails leaves both the file and the served catalogue as they were.
/// </para>
/// </remarks>
public sealed class CatalogBook
{
    private readonly Catalog shipped;
    private readonly IPersonalCatalogStore store;
    private readonly SemaphoreSlim writes = new(1, 1);
    private IReadOnlyList<CatalogParameter> personal;
    private Catalog current;

    private CatalogBook(Catalog shipped, IPersonalCatalogStore store, IReadOnlyList<CatalogParameter> personal, IReadOnlyList<string> dropped)
    {
        this.shipped = shipped;
        this.store = store;
        this.personal = personal;
        current = shipped.WithPersonal(personal);
        Dropped = dropped;
    }

    /// <summary>The catalogue as it is now.</summary>
    public Catalog Current => Volatile.Read(ref current);

    /// <summary>
    /// Personal entries left out at start-up, as <c>key: value</c>: their key
    /// no longer exists, or the shipped catalogue now has the same value.
    /// </summary>
    /// <remarks>
    /// Not an error, and the reason is an upgrade: a later version may ship the
    /// value a user had added, or drop a key. Refusing to start would punish
    /// the user for the application's own change; the shipped entry wins, and
    /// the start-up says what was set aside. The file keeps the entry until the
    /// next change rewrites it.
    /// </remarks>
    public IReadOnlyList<string> Dropped { get; }

    /// <summary>Reads the personal entries and merges them with the shipped catalogue.</summary>
    public static async Task<CatalogBook> LoadAsync(Catalog shipped, IPersonalCatalogStore store, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipped);
        ArgumentNullException.ThrowIfNull(store);

        IReadOnlyList<CatalogParameter> stored = await store.ReadAsync(shipped.Universe, cancellationToken).ConfigureAwait(false);

        List<CatalogParameter> kept = [];
        List<string> dropped = [];

        foreach (CatalogParameter parameter in stored)
        {
            List<CatalogEntry> entries = [];

            foreach (CatalogEntry entry in parameter.Entries)
            {
                bool known = shipped.KnowsKey(parameter.Key);
                bool shadowed = known && shipped.TryGetFragment(parameter.Key, entry.Value, out _);
                bool repeated = entries.Any(other => string.Equals(other.Value, entry.Value, StringComparison.Ordinal));

                if (!known || shadowed || repeated)
                {
                    dropped.Add($"{parameter.Key}: {entry.Value}");
                    continue;
                }

                entries.Add(entry with { Origin = CatalogEntryOrigin.Personal });
            }

            if (entries.Count > 0)
            {
                kept.Add(parameter with { Entries = entries });
            }
        }

        return new CatalogBook(shipped, store, kept, dropped);
    }

    /// <summary>Adds a complete entry to the personal catalogue (§I.4.1).</summary>
    /// <returns>The catalogue after the addition.</returns>
    /// <exception cref="CatalogRuleException"><c>CATALOG_ENTRY_INVALID</c> or <c>CATALOG_ENTRY_DUPLICATE</c>.</exception>
    public async Task<Catalog> AddEntryAsync(
        string key,
        string value,
        IReadOnlyDictionary<string, string> labels,
        string fragment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(fragment);

        CatalogEntry entry = Complete(key, value.Trim(), labels, fragment.Trim());

        await writes.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (Current.TryGetFragment(key, entry.Value, out _))
            {
                throw new CatalogRuleException(
                    CatalogRuleCode.EntryDuplicate,
                    $"The catalogue already has the value '{entry.Value}' for the key '{key}'.");
            }

            List<CatalogParameter> next = [.. personal.Where(parameter => !string.Equals(parameter.Key, key, StringComparison.Ordinal))];
            CatalogParameter? existing = personal.FirstOrDefault(parameter => string.Equals(parameter.Key, key, StringComparison.Ordinal));

            next.Add(existing is null
                ? new CatalogParameter(key, new Dictionary<string, string>(StringComparer.Ordinal), [entry])
                : existing with { Entries = [.. existing.Entries, entry] });

            return await PublishAsync(next, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writes.Release();
        }
    }

    /// <summary>Removes a personal entry. Blueprints that use it are not touched (DEC-056).</summary>
    /// <returns>The catalogue after the removal.</returns>
    /// <exception cref="CatalogRuleException"><c>CATALOG_ENTRY_NOT_FOUND</c> or <c>CATALOG_ENTRY_SHIPPED</c>.</exception>
    public async Task<Catalog> RemoveEntryAsync(string key, string value, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        await writes.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            CatalogEntry? entry = Current.Find(key)?.Entries
                .FirstOrDefault(candidate => string.Equals(candidate.Value, value, StringComparison.Ordinal));

            if (entry is null)
            {
                throw new CatalogRuleException(
                    CatalogRuleCode.EntryNotFound,
                    $"The catalogue has no value '{value}' for the key '{key}'.");
            }

            if (entry.Origin == CatalogEntryOrigin.Shipped)
            {
                throw new CatalogRuleException(
                    CatalogRuleCode.EntryShipped,
                    $"The value '{value}' of the key '{key}' is shipped with the application and is not removed from the interface.");
            }

            List<CatalogParameter> next = [];

            foreach (CatalogParameter parameter in personal)
            {
                CatalogEntry[] remaining = [.. parameter.Entries.Where(candidate =>
                    !(string.Equals(parameter.Key, key, StringComparison.Ordinal)
                      && string.Equals(candidate.Value, value, StringComparison.Ordinal)))];

                if (remaining.Length > 0)
                {
                    next.Add(parameter with { Entries = remaining });
                }
            }

            return await PublishAsync(next, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            writes.Release();
        }
    }

    /// <summary>The four conditions of a complete entry (§I.4.1), each refused by name.</summary>
    private CatalogEntry Complete(string key, string value, IReadOnlyDictionary<string, string> labels, string fragment)
    {
        if (!shipped.KnowsKey(key))
        {
            throw Invalid($"The catalogue has no key '{key}'; an entry joins an existing key, never a new one.");
        }

        if (value.Length == 0)
        {
            throw Invalid("The entry has no value.");
        }

        if (fragment.Length == 0)
        {
            throw Invalid($"The entry '{key}: {value}' has no fragment: it would put nothing into the prompt.");
        }

        Dictionary<string, string> trimmed = new(StringComparer.Ordinal);

        foreach (string culture in InterfaceCulture.All)
        {
            if (!labels.TryGetValue(culture, out string? label) || string.IsNullOrWhiteSpace(label))
            {
                throw Invalid($"The entry '{key}: {value}' has no '{culture}' label.");
            }

            trimmed[culture] = label.Trim();
        }

        return new CatalogEntry(value, fragment, trimmed, CatalogEntryOrigin.Personal);

        static CatalogRuleException Invalid(string message) => new(CatalogRuleCode.EntryInvalid, message);
    }

    private async Task<Catalog> PublishAsync(List<CatalogParameter> next, CancellationToken cancellationToken)
    {
        // Built before anything is written: a catalogue the domain refuses
        // never reaches the disk.
        Catalog merged = shipped.WithPersonal(next);

        await store.WriteAsync(shipped.Universe, next, cancellationToken).ConfigureAwait(false);

        personal = next;
        Volatile.Write(ref current, merged);

        return merged;
    }
}
