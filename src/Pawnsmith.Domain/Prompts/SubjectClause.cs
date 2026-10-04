using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Domain.Prompts;

/// <summary>
/// A composed subject clause, and what the composer had to say about it.
/// </summary>
/// <remarks>
/// A record rather than a bare string because a value the catalogue does not
/// know produces a diagnostic <b>beside</b> the clause, never inside it and
/// never as an exception (DEC-063, §D.6.3). A string would have had nowhere to
/// put it.
/// </remarks>
/// <param name="Clause">The clause, normalised. Never null, possibly empty.</param>
/// <param name="Diagnostics">The values the catalogue did not know, in clause order. Empty in the ordinary case.</param>
public sealed record ComposedSubject(string Clause, IReadOnlyList<CompositionDiagnostic> Diagnostics);

/// <summary>
/// A parameter value the catalogue did not know, and what was said instead.
/// </summary>
/// <remarks>
/// Not an error, and the distinction is DEC-056: a catalogue is a machine's
/// data, a project is a user's, and one never rejects the other. The user can
/// act on this — the catalogue is a file they edit — so the message says
/// exactly what was looked up.
/// </remarks>
/// <param name="Key">The parameter key, as written in the blueprint.</param>
/// <param name="Value">The value, as written in the blueprint.</param>
/// <param name="Message">What to tell the user.</param>
public sealed record CompositionDiagnostic(string Key, string Value, string Message);

/// <summary>
/// Composes the subject clause of a blueprint from its fields, a template and
/// a catalogue (§D.6.2).
/// </summary>
/// <remarks>
/// <para>
/// <b>Deterministic</b>: same blueprint, template and catalogue, same string,
/// on any machine and under any culture. That is the first acceptance
/// criterion of the slice, and nothing here reads the current culture — the
/// one sort is ordinal.
/// </para>
/// <para>
/// <b>The separator and the order are a compatibility surface</b>, like the
/// assembly rule of <see cref="ResolvedPrompt"/>: changing either changes every
/// clause composed afterwards, and therefore what the recomposition rule of
/// DEC-067 compares against. It takes a decision card, not a convenience
/// commit.
/// </para>
/// <para>
/// What this function does <b>not</b> do: read the style or framing clause
/// (DEC-028, DEC-029), assemble the prompt (<see cref="ResolvedPrompt.From"/>
/// does), validate the blueprint against the catalogue (DEC-056), or know the
/// blueprint's size — a <c>Large</c> pawn is large because its cell is, not
/// because the prompt said so (§D.5.2).
/// </para>
/// </remarks>
public static class SubjectClause
{
    /// <summary>
    /// The separator between fragments. The form of the reference subject of
    /// DEC-043: a noun phrase with commas, not sentences.
    /// </summary>
    private const string Separator = ", ";

    /// <summary>Composes the clause a blueprint's fields describe.</summary>
    public static ComposedSubject Compose(Blueprint blueprint, PromptTemplate template, Catalog catalog)
    {
        ArgumentNullException.ThrowIfNull(blueprint);

        return Compose(
            blueprint.Race,
            blueprint.CharacterClass,
            blueprint.OptionalParameters,
            blueprint.Details,
            template,
            catalog);
    }

    /// <summary>Composes the clause these fields describe.</summary>
    /// <remarks>
    /// Head, then the optional fragments in the order of §D.6.2, then the
    /// details; empty pieces omitted rather than joined; the whole normalised
    /// once at the end with the same function the assembly and the misalignment
    /// use.
    /// </remarks>
    /// <param name="race">Mandatory field of the blueprint. Trimmed here.</param>
    /// <param name="characterClass">Mandatory field of the blueprint. Trimmed here.</param>
    /// <param name="optionalParameters">Catalogue keys and their values. Unknown ones are not refused.</param>
    /// <param name="details">Free text, placed last.</param>
    /// <param name="template">The universe's sentence structure.</param>
    /// <param name="catalog">The universe's vocabulary.</param>
    public static ComposedSubject Compose(
        string race,
        string characterClass,
        IReadOnlyDictionary<string, string> optionalParameters,
        string details,
        PromptTemplate template,
        Catalog catalog)
    {
        ArgumentNullException.ThrowIfNull(race);
        ArgumentNullException.ThrowIfNull(characterClass);
        ArgumentNullException.ThrowIfNull(optionalParameters);
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(catalog);

        List<CompositionDiagnostic> diagnostics = [];
        List<string> fragments = [Head(race, characterClass, template, catalog, diagnostics)];

        foreach (string key in OrderedKeys(optionalParameters, template))
        {
            string value = optionalParameters[key];

            // An empty value is read as "not constrained", like an absent key
            // (DEC-024). It produces neither a fragment nor a diagnostic: a
            // warning about an empty string would be noise nobody can act on.
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            fragments.Add(Fragment(key, value, template, catalog, diagnostics));
        }

        fragments.Add(details);

        string clause = string.Join(
            Separator,
            fragments
                .Select(ResolvedPrompt.Normalize)
                .Where(fragment => fragment.Length > 0));

        return new ComposedSubject(ResolvedPrompt.Normalize(clause), diagnostics);
    }

    /// <summary>The head: the template's sentence, its two tokens filled in.</summary>
    /// <remarks>
    /// <para>
    /// <b>Each token receives the fragment of its value</b> when the catalogue
    /// has a list for it (DEC-106): <c>orc</c> becomes "an orc", so the article
    /// belongs to the entry and never to the template — which is what used to
    /// write "a orc".
    /// </para>
    /// <para>
    /// A catalogue <b>without</b> a <c>race</c> or <c>characterClass</c> key
    /// offers no list for that field: the value is inserted as written, and
    /// nothing is said, since there was nothing to look it up in. A catalogue
    /// <b>with</b> the key but not the value says so, like an optional value.
    /// </para>
    /// </remarks>
    private static string Head(
        string race,
        string characterClass,
        PromptTemplate template,
        Catalog catalog,
        List<CompositionDiagnostic> diagnostics)
    {
        return TemplateToken.Substitute(
            template.SubjectHead,
            new Dictionary<string, string>
            {
                [TemplateToken.Race] = HeadValue(TemplateToken.Race, race.Trim(), catalog, diagnostics),
                [TemplateToken.CharacterClass] = HeadValue(TemplateToken.CharacterClass, characterClass.Trim(), catalog, diagnostics),
            });
    }

    private static string HeadValue(string key, string value, Catalog catalog, List<CompositionDiagnostic> diagnostics)
    {
        if (catalog.TryGetFragment(key, value, out string fragment))
        {
            return fragment;
        }

        if (catalog.KnowsKey(key) && value.Length > 0)
        {
            diagnostics.Add(new CompositionDiagnostic(
                key,
                value,
                $"The catalogue knows the key '{key}' but not the value '{value}'; the value was inserted as written."));
        }

        return value;
    }

    /// <summary>
    /// The keys of the blueprint, in output order: those the template lists
    /// first, in its order, then the rest in ordinal order.
    /// </summary>
    /// <remarks>
    /// Two explicit rules rather than one convention. A purely alphabetical
    /// order would put "wearing…" before "wielding…" where English prose wants
    /// the reverse; a purely declared order would let a key the template forgot
    /// come out anywhere, and determinism is an acceptance criterion. The
    /// fallback sort is <b>ordinal, never cultural</b> — the same trap as in
    /// C.3.3: a cultural sort varies with the process culture and the installed
    /// ICU, and two machines would compose two clauses for one blueprint.
    /// </remarks>
    private static IReadOnlyList<string> OrderedKeys(
        IReadOnlyDictionary<string, string> optionalParameters,
        PromptTemplate template)
    {
        List<string> ordered = [];
        HashSet<string> placed = new(StringComparer.Ordinal);

        foreach (string key in template.OptionalOrder)
        {
            if (optionalParameters.ContainsKey(key) && placed.Add(key))
            {
                ordered.Add(key);
            }
        }

        IEnumerable<string> remaining = optionalParameters.Keys
            .Where(key => !placed.Contains(key))
            .OrderBy(key => key, StringComparer.Ordinal);

        ordered.AddRange(remaining);

        return ordered;
    }

    /// <summary>The catalogue's fragment for this value, or the template's fallback and a diagnostic.</summary>
    /// <remarks>
    /// Why a fallback rather than silence: omitting an unknown value outright
    /// would be worse than inserting it plainly. The user ticked "halberd", and
    /// a clause with no trace of it produces an illustration with no halberd
    /// <b>and nothing to explain why</b>. The fallback at least hands the word to
    /// the model, and the diagnostic tells the user their catalogue does not
    /// know the value — which they can fix, since the file is theirs to edit.
    /// </remarks>
    private static string Fragment(
        string key,
        string value,
        PromptTemplate template,
        Catalog catalog,
        List<CompositionDiagnostic> diagnostics)
    {
        if (catalog.TryGetFragment(key, value, out string fragment))
        {
            return fragment;
        }

        string what = catalog.KnowsKey(key)
            ? $"The catalogue knows the key '{key}' but not the value '{value}'; the value was inserted as written."
            : $"The catalogue does not know the key '{key}'; its value '{value}' was inserted as written.";

        diagnostics.Add(new CompositionDiagnostic(key, value, what));

        return TemplateToken.Substitute(
            template.UnknownValueFragment,
            new Dictionary<string, string> { [TemplateToken.Value] = value.Trim() });
    }
}
