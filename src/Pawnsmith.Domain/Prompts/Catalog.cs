using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Domain.Prompts;

/// <summary>
/// One value a parameter can take, and the words it puts into the subject
/// clause.
/// </summary>
/// <remarks>
/// <para>
/// <b>The fragment is a group of words, not a noun</b> (DEC-064). <c>axe</c>
/// does not become "axe" in the prompt; it becomes "wielding a large battle axe
/// held vertically against the body". The decision comes from a measurement:
/// T0a asked for a large battle axe and got two daggers (DEC-043). A paraphrase
/// weighs more than a word in a text encoder, and — the part that matters most —
/// the compact-pose constraint of DEC-042 travels with the very object that
/// could widen the silhouette, instead of sitting in a general sentence about
/// objects the model does not yet know it is going to draw.
/// </para>
/// <para>
/// In English, like everything that enters a prompt (DEC-037). What the user
/// sees on screen is one of the <paramref name="Labels"/>, never this text
/// (DEC-106).
/// </para>
/// </remarks>
/// <param name="Value">The value as written in a blueprint — in <c>optionalParameters</c>, or as its race or class.</param>
/// <param name="Fragment">The words inserted into the subject clause, verbatim.</param>
/// <param name="Labels">What the interface shows, by culture name: one non-empty label per <see cref="InterfaceCulture"/>.</param>
/// <param name="Origin">Shipped with the application, or added by the user (DEC-107).</param>
public sealed record CatalogEntry(
    string Value,
    string Fragment,
    IReadOnlyDictionary<string, string> Labels,
    CatalogEntryOrigin Origin = CatalogEntryOrigin.Shipped);

/// <summary>Where a catalogue entry comes from (DEC-107).</summary>
public enum CatalogEntryOrigin
{
    /// <summary>The application's own file, under <c>config/</c>. Read-only from the interface.</summary>
    Shipped,

    /// <summary>The user's file, under the user directory. Added and removed from the interface.</summary>
    Personal,
}

/// <summary>
/// One parameter of a blueprint, with the values the catalogue knows for it.
/// </summary>
/// <remarks>
/// Two keys are reserved and name the required fields rather than optional
/// ones: <see cref="TemplateToken.Race"/> and
/// <see cref="TemplateToken.CharacterClass"/> — the very names of the tokens of
/// the subject head, whose values they translate (DEC-106). Every other key is
/// an optional parameter.
/// </remarks>
/// <param name="Key">The key, as written in a blueprint. <c>race</c>, <c>weapon</c>, <c>armour</c>…</param>
/// <param name="Labels">The parameter's name on screen, by culture.</param>
/// <param name="Entries">The known values, in file order.</param>
public sealed record CatalogParameter(
    string Key,
    IReadOnlyDictionary<string, string> Labels,
    IReadOnlyList<CatalogEntry> Entries);

/// <summary>
/// The vocabulary of one universe: which optional parameters exist, which
/// values each one takes, and what each value says in a prompt.
/// </summary>
/// <remarks>
/// <para>
/// <b>Global to the application, one file per universe, never embedded in a
/// project</b> (DEC-063). The criterion is the one DEC-052 applied to
/// <c>gutterMm</c>: who determines the value. Equipment vocabulary is
/// determined by the universe, and Pawnsmith is single-user, so a property of
/// the universe is a global property here.
/// </para>
/// <para>
/// <b>The catalogue validates nothing about a project.</b> A blueprint whose
/// parameters name a key or a value this catalogue does not know is a perfectly
/// valid blueprint: it loads, composes and exports. That is DEC-056 applied
/// as-is — the catalogue is a machine's data, the project is a user's, and one
/// never rejects the other. An unknown value yields a fallback fragment and a
/// diagnostic, never an error (see <c>SubjectClause</c>).
/// </para>
/// <para>
/// The parameters are an <b>ordered list</b>, not a dictionary, because the
/// composition rule uses their order when the template does not say otherwise.
/// Uniqueness of keys and values is therefore checked here, explicitly, rather
/// than obtained for free from a dictionary — the general rule of C.3.3: a
/// collection picks its side, ordered or sorted, in the schema and not in the
/// code.
/// </para>
/// </remarks>
public sealed class Catalog
{
    private readonly Dictionary<string, Dictionary<string, string>> fragments;

    private Catalog(Universe universe, IReadOnlyList<CatalogParameter> parameters)
    {
        Universe = universe;
        Parameters = parameters;
        fragments = IndexFragments(parameters);
    }

    /// <summary>The universe this vocabulary belongs to.</summary>
    public Universe Universe { get; }

    /// <summary>The parameters, in file order.</summary>
    public IReadOnlyList<CatalogParameter> Parameters { get; }

    /// <summary>A catalogue that knows no parameter at all.</summary>
    /// <remarks>
    /// Legitimate, and worth being able to say: with an empty catalogue every
    /// value is unknown, every fragment is the fallback, and the clause is still
    /// composed (test 14 of D.11).
    /// </remarks>
    public static Catalog Empty(Universe universe) => new(universe, []);

    /// <summary>Builds a catalogue, refusing one that contradicts itself.</summary>
    /// <remarks>
    /// The internal consistency rules live here, in the domain, so that the file
    /// reader has nothing of its own to know about what makes a catalogue
    /// coherent — convention 5 of DEC-038, and the same split as the calibration.
    /// The reader maps <see cref="CatalogException"/> to <c>CATALOG_INVALID</c>.
    /// </remarks>
    /// <exception cref="CatalogException">
    /// A key is empty or repeated, a value is empty or repeated within its key,
    /// a fragment is empty, or a label is missing or empty for one of the
    /// interface cultures. The message names the offender.
    /// </exception>
    public static Catalog Create(Universe universe, IReadOnlyList<CatalogParameter> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        HashSet<string> keys = new(StringComparer.Ordinal);

        foreach (CatalogParameter parameter in parameters)
        {
            ArgumentNullException.ThrowIfNull(parameter);

            if (string.IsNullOrWhiteSpace(parameter.Key))
            {
                throw new CatalogException("A catalogue parameter has an empty key.");
            }

            if (!keys.Add(parameter.Key))
            {
                throw new CatalogException($"The catalogue declares the key '{parameter.Key}' more than once.");
            }

            RequireLabels(parameter.Labels, $"key '{parameter.Key}'");

            HashSet<string> values = new(StringComparer.Ordinal);

            foreach (CatalogEntry entry in parameter.Entries)
            {
                ArgumentNullException.ThrowIfNull(entry);

                if (string.IsNullOrWhiteSpace(entry.Value))
                {
                    throw new CatalogException($"The catalogue key '{parameter.Key}' has an entry with an empty value.");
                }

                if (!values.Add(entry.Value))
                {
                    throw new CatalogException(
                        $"The catalogue key '{parameter.Key}' declares the value '{entry.Value}' more than once.");
                }

                if (string.IsNullOrWhiteSpace(entry.Fragment))
                {
                    throw new CatalogException(
                        $"The catalogue entry '{parameter.Key}: {entry.Value}' has an empty fragment.");
                }

                RequireLabels(entry.Labels, $"entry '{parameter.Key}: {entry.Value}'");
            }
        }

        return new Catalog(universe, parameters);
    }

    /// <summary>The fragment for one key and value, if the catalogue knows them.</summary>
    /// <remarks>
    /// Keys and values are matched <b>ordinally and case-sensitively</b>:
    /// <c>Weapon</c> is not <c>weapon</c>. A blueprint's parameters are free
    /// strings and the project's rule for free strings is ordinal comparison,
    /// everywhere (C.3.3, C.5.4). Being lenient here would make the composed
    /// clause depend on a rule the misalignment check does not share.
    /// </remarks>
    /// <returns><c>true</c> and the fragment, or <c>false</c> and an empty string.</returns>
    public bool TryGetFragment(string key, string value, out string fragment)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        if (fragments.TryGetValue(key, out Dictionary<string, string>? entries)
            && entries.TryGetValue(value, out string? found))
        {
            fragment = found;
            return true;
        }

        fragment = string.Empty;
        return false;
    }

    /// <summary>Whether the catalogue declares this key at all, whatever its values.</summary>
    public bool KnowsKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return fragments.ContainsKey(key);
    }

    /// <summary>The parameter of this key, or <c>null</c>.</summary>
    public CatalogParameter? Find(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return Parameters.FirstOrDefault(parameter => string.Equals(parameter.Key, key, StringComparison.Ordinal));
    }

    /// <summary>
    /// The catalogue the interface serves: this one, followed key by key by the
    /// user's personal entries (DEC-107).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A personal entry joins an existing key, never a new one.</b> A key is
    /// a parameter of the blueprint and of the template's order; inventing one
    /// from the interface would add a field nothing else knows about.
    /// </para>
    /// <para>
    /// The result goes through <see cref="Create"/>, so a personal value that
    /// repeats a shipped one is refused by the same rule that refuses a
    /// repetition inside one file. Every personal entry is marked
    /// <see cref="CatalogEntryOrigin.Personal"/>, whatever it said.
    /// </para>
    /// </remarks>
    /// <exception cref="CatalogException">A personal key the catalogue does not declare, or any rule of <see cref="Create"/>.</exception>
    public Catalog WithPersonal(IReadOnlyList<CatalogParameter> personal)
    {
        ArgumentNullException.ThrowIfNull(personal);

        foreach (CatalogParameter parameter in personal)
        {
            if (!KnowsKey(parameter.Key))
            {
                throw new CatalogException(
                    $"The personal catalogue adds to the key '{parameter.Key}', which the catalogue does not declare.");
            }
        }

        List<CatalogParameter> merged = [];

        foreach (CatalogParameter parameter in Parameters)
        {
            IEnumerable<CatalogEntry> added = personal
                .Where(extra => string.Equals(extra.Key, parameter.Key, StringComparison.Ordinal))
                .SelectMany(extra => extra.Entries)
                .Select(entry => entry with { Origin = CatalogEntryOrigin.Personal });

            merged.Add(parameter with { Entries = [.. parameter.Entries, .. added] });
        }

        return Create(Universe, merged);
    }

    private static void RequireLabels(IReadOnlyDictionary<string, string>? labels, string what)
    {
        // One label per interface culture, none empty: a list the interface
        // shows in French must have a French word for every line (DEC-106).
        foreach (string culture in InterfaceCulture.All)
        {
            if (labels is null || !labels.TryGetValue(culture, out string? label) || string.IsNullOrWhiteSpace(label))
            {
                throw new CatalogException($"The catalogue {what} has no '{culture}' label.");
            }
        }
    }

    private static Dictionary<string, Dictionary<string, string>> IndexFragments(
        IReadOnlyList<CatalogParameter> parameters)
    {
        // Built once, after Create has refused duplicates, so the indexer can
        // never be asked to choose between two fragments for one value.
        Dictionary<string, Dictionary<string, string>> index = new(StringComparer.Ordinal);

        foreach (CatalogParameter parameter in parameters)
        {
            Dictionary<string, string> entries = new(StringComparer.Ordinal);

            foreach (CatalogEntry entry in parameter.Entries)
            {
                entries[entry.Value] = entry.Fragment;
            }

            index[parameter.Key] = entries;
        }

        return index;
    }
}

/// <summary>A catalogue that contradicts itself: an empty or repeated key, value or fragment, or a missing label.</summary>
/// <remarks>
/// A domain exception rather than an <see cref="ArgumentException"/>, so the
/// file reader can map exactly this and nothing else to <c>CATALOG_INVALID</c>
/// without catching the whole family of argument errors.
/// </remarks>
public sealed class CatalogException : Exception
{
    public CatalogException(string message)
        : base(message)
    {
    }
}
