using Pawnsmith.Domain.Primitives;

namespace Pawnsmith.Domain.Prompts;

/// <summary>
/// A style of the library: a starting point a project copies (§I.7, DEC-110).
/// </summary>
/// <remarks>
/// <para>
/// <b>Not the project's style.</b> A project holds its own <c>Style</c>, and
/// choosing a preset copies these clauses into it. Nothing links the two
/// afterwards: changing the library changes no project, which keeps every
/// project complete on its own, archive included.
/// </para>
/// <para>
/// The clauses are English, like everything that enters a prompt (DEC-037).
/// The palette is not a field: a palette is said in the style clause, the only
/// part of a style that reaches the generator (DEC-110).
/// </para>
/// </remarks>
/// <param name="Id">Stable identifier: a short name for a shipped style, a generated one for a personal style.</param>
/// <param name="Names">The name shown, by culture. A personal style has the user's one name under every culture.</param>
/// <param name="StyleClause">The style clause the project receives.</param>
/// <param name="NegativeClause">The negative clause the project receives; may be empty.</param>
/// <param name="Origin">Shipped with the application, or saved by the user.</param>
public sealed record StylePreset(
    string Id,
    IReadOnlyDictionary<string, string> Names,
    string StyleClause,
    string NegativeClause,
    StyleOrigin Origin = StyleOrigin.Shipped);

/// <summary>Where a style of the library comes from.</summary>
public enum StyleOrigin
{
    /// <summary>The application's file, under <c>config/</c>. Read-only from the interface.</summary>
    Shipped,

    /// <summary>The user's file, under the user directory.</summary>
    Personal,
}

/// <summary>The coherence of a list of styles.</summary>
public static class StylePresets
{
    /// <summary>Refuses a list that contradicts itself.</summary>
    /// <exception cref="StylePresetException">
    /// An empty or repeated identifier, a missing name for an interface
    /// culture, or an empty style clause. The message names the offender.
    /// </exception>
    public static IReadOnlyList<StylePreset> Check(IReadOnlyList<StylePreset> presets)
    {
        ArgumentNullException.ThrowIfNull(presets);

        HashSet<string> ids = new(StringComparer.Ordinal);

        foreach (StylePreset preset in presets)
        {
            ArgumentNullException.ThrowIfNull(preset);

            if (string.IsNullOrWhiteSpace(preset.Id))
            {
                throw new StylePresetException("A style has an empty identifier.");
            }

            if (!ids.Add(preset.Id))
            {
                throw new StylePresetException($"The style '{preset.Id}' is declared more than once.");
            }

            foreach (string culture in InterfaceCulture.All)
            {
                if (preset.Names is null
                    || !preset.Names.TryGetValue(culture, out string? name)
                    || string.IsNullOrWhiteSpace(name))
                {
                    throw new StylePresetException($"The style '{preset.Id}' has no '{culture}' name.");
                }
            }

            if (string.IsNullOrWhiteSpace(preset.StyleClause))
            {
                throw new StylePresetException($"The style '{preset.Id}' has an empty style clause.");
            }
        }

        return presets;
    }
}

/// <summary>A list of styles that contradicts itself.</summary>
public sealed class StylePresetException : Exception
{
    public StylePresetException(string message)
        : base(message)
    {
    }
}
