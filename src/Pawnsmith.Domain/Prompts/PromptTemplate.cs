using Pawnsmith.Domain.Projects;

namespace Pawnsmith.Domain.Prompts;

/// <summary>
/// The sentence structure of one universe's subject clause: how the head is
/// phrased, in what order the optional fragments follow, and what to say about
/// a value the catalogue does not know.
/// </summary>
/// <remarks>
/// <para>
/// One file per universe, shipped with the application and editable
/// (DEC-010), read by the composer of T3 — which is why its schema comes here
/// and not with the ComfyUI workflow template of T4 (DEC-073).
/// </para>
/// <para>
/// <b>Kept apart from the catalogue on purpose</b> (DEC-065). The two have the
/// same scope and not the same blast radius: a mistake here malforms the clause
/// of every blueprint, a mistake in the catalogue touches one value. This file
/// is edited rarely and by someone careful; the catalogue often and by anyone.
/// </para>
/// <para>
/// <b>The tokens are a closed list, and an unknown one is refused.</b> Writing
/// <c>{taille}</c> in the head must not yield a clause containing the literal
/// text "{taille}", which would go to the model with nothing to say so and be
/// discovered on a spoiled illustration. Size is not a token at all, and that
/// is deliberate: a <c>Large</c> pawn is large because its cell is, not because
/// the prompt said so (§D.5.2).
/// </para>
/// </remarks>
public sealed class PromptTemplate
{
    private PromptTemplate(
        Universe universe,
        string subjectHead,
        IReadOnlyList<string> optionalOrder,
        string unknownValueFragment)
    {
        Universe = universe;
        SubjectHead = subjectHead;
        OptionalOrder = optionalOrder;
        UnknownValueFragment = unknownValueFragment;
    }

    /// <summary>The universe this template phrases.</summary>
    public Universe Universe { get; }

    /// <summary>
    /// The head of the clause, with <see cref="TemplateToken.Race"/> and
    /// <see cref="TemplateToken.CharacterClass"/> where the fields go.
    /// </summary>
    public string SubjectHead { get; }

    /// <summary>
    /// The keys whose fragments come first, in this order. Keys not listed
    /// here follow, in ordinal order.
    /// </summary>
    public IReadOnlyList<string> OptionalOrder { get; }

    /// <summary>
    /// What to say about a value the catalogue does not know, with
    /// <see cref="TemplateToken.Value"/> where the raw value goes.
    /// </summary>
    public string UnknownValueFragment { get; }

    /// <summary>Builds a template, refusing one whose tokens are wrong.</summary>
    /// <exception cref="PromptTemplateException">
    /// The head lacks a required token or carries an unknown one, the fallback
    /// carries a token other than <c>{value}</c>, a brace is unbalanced, or the
    /// optional order repeats a key. The message names the token or the key.
    /// </exception>
    public static PromptTemplate Create(
        Universe universe,
        string subjectHead,
        IReadOnlyList<string> optionalOrder,
        string unknownValueFragment)
    {
        ArgumentNullException.ThrowIfNull(subjectHead);
        ArgumentNullException.ThrowIfNull(optionalOrder);
        ArgumentNullException.ThrowIfNull(unknownValueFragment);

        if (string.IsNullOrWhiteSpace(subjectHead))
        {
            throw new PromptTemplateException(
                PromptTemplateFault.Malformed,
                "The template's subject head is empty.");
        }

        IReadOnlySet<string> headTokens = TemplateToken.Find(subjectHead, "subjectHead");

        foreach (string required in TemplateToken.RequiredInHead)
        {
            if (!headTokens.Contains(required))
            {
                throw new PromptTemplateException(
                    PromptTemplateFault.MissingToken,
                    $"The template's subject head does not contain the token '{{{required}}}'.");
            }
        }

        foreach (string token in headTokens)
        {
            if (!TemplateToken.AllowedInHead.Contains(token))
            {
                throw new PromptTemplateException(
                    PromptTemplateFault.UnknownToken,
                    $"The template's subject head contains the unknown token '{{{token}}}'. " +
                    $"Allowed tokens are: {TemplateToken.Describe(TemplateToken.AllowedInHead)}.");
            }
        }

        if (string.IsNullOrWhiteSpace(unknownValueFragment))
        {
            throw new PromptTemplateException(
                PromptTemplateFault.Malformed,
                "The template's fallback fragment for unknown values is empty.");
        }

        foreach (string token in TemplateToken.Find(unknownValueFragment, "unknownValueFragment"))
        {
            if (!TemplateToken.AllowedInFallback.Contains(token))
            {
                throw new PromptTemplateException(
                    PromptTemplateFault.UnknownToken,
                    $"The template's fallback fragment contains the unknown token '{{{token}}}'. " +
                    $"The only allowed token is '{{{TemplateToken.Value}}}'.");
            }
        }

        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (string key in optionalOrder)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new PromptTemplateException(
                    PromptTemplateFault.Malformed,
                    "The template's optional order contains an empty key.");
            }

            if (!seen.Add(key))
            {
                throw new PromptTemplateException(
                    PromptTemplateFault.Malformed,
                    $"The template's optional order lists the key '{key}' more than once.");
            }
        }

        return new PromptTemplate(universe, subjectHead, optionalOrder, unknownValueFragment);
    }
}

/// <summary>
/// The tokens a template may contain, and how to find them in a pattern.
/// </summary>
/// <remarks>
/// Written by hand rather than with a regular expression, so that what counts
/// as a token is visible in the code that reads it: an opening brace, a name,
/// a closing brace, nothing else. Literal braces are not supported in a
/// template, and that is a documented limit rather than an escape syntax
/// nobody asked for.
/// </remarks>
public static class TemplateToken
{
    /// <summary>The blueprint's race.</summary>
    public const string Race = "race";

    /// <summary>The blueprint's class. Named as the field is, since <c>class</c> is reserved in C#.</summary>
    public const string CharacterClass = "characterClass";

    /// <summary>The raw value, in the fallback fragment only.</summary>
    public const string Value = "value";

    /// <summary>Tokens the subject head must contain.</summary>
    public static IReadOnlyList<string> RequiredInHead { get; } = [Race, CharacterClass];

    /// <summary>Tokens the subject head may contain — the same two, and no other.</summary>
    public static IReadOnlySet<string> AllowedInHead { get; } =
        new HashSet<string>([Race, CharacterClass], StringComparer.Ordinal);

    /// <summary>Tokens the fallback fragment may contain.</summary>
    public static IReadOnlySet<string> AllowedInFallback { get; } =
        new HashSet<string>([Value], StringComparer.Ordinal);

    /// <summary>Every distinct token name found in <paramref name="pattern"/>.</summary>
    /// <exception cref="PromptTemplateException">A brace is not closed, or a token is empty.</exception>
    public static IReadOnlySet<string> Find(string pattern, string where)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        HashSet<string> tokens = new(StringComparer.Ordinal);
        int position = 0;

        while (position < pattern.Length)
        {
            int open = pattern.IndexOf('{', position);

            if (open < 0)
            {
                break;
            }

            int close = pattern.IndexOf('}', open + 1);
            int nextOpen = pattern.IndexOf('{', open + 1);

            // "{race {characterClass}" is a brace left open, not a token
            // named "race {characterClass": a second opening brace before the
            // closing one is the sign of it.
            if (close < 0 || (nextOpen >= 0 && nextOpen < close))
            {
                throw new PromptTemplateException(
                    PromptTemplateFault.Malformed,
                    $"The template's {where} has an opening brace without a closing one.");
            }

            string name = pattern[(open + 1)..close];

            if (name.Length == 0)
            {
                throw new PromptTemplateException(
                    PromptTemplateFault.Malformed,
                    $"The template's {where} contains an empty token '{{}}'.");
            }

            tokens.Add(name);
            position = close + 1;
        }

        return tokens;
    }

    /// <summary>Replaces every <c>{token}</c> in <paramref name="pattern"/> by its value.</summary>
    /// <remarks>
    /// Ordinal replacement, one token at a time. The pattern has been validated
    /// by <see cref="PromptTemplate.Create"/>, so every brace pair here is a
    /// known token and none is left behind.
    /// </remarks>
    public static string Substitute(string pattern, IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(values);

        string result = pattern;

        foreach ((string token, string value) in values)
        {
            result = result.Replace("{" + token + "}", value, StringComparison.Ordinal);
        }

        return result;
    }

    internal static string Describe(IEnumerable<string> tokens) =>
        string.Join(", ", tokens.Select(token => "{" + token + "}"));
}

/// <summary>What is wrong with a template.</summary>
/// <remarks>
/// Three faults rather than one, because the reader gives two of them different
/// error codes: an unknown token is <c>TEMPLATE_UNKNOWN_TOKEN</c>, the rest is
/// <c>TEMPLATE_INVALID</c> (§D.10).
/// </remarks>
public enum PromptTemplateFault
{
    /// <summary>A token that is not in the closed list for that field.</summary>
    UnknownToken,

    /// <summary>A token the head must carry and does not.</summary>
    MissingToken,

    /// <summary>An empty field, an unbalanced brace, an empty or repeated key.</summary>
    Malformed,
}

/// <summary>A template that cannot be used, and why.</summary>
public sealed class PromptTemplateException : Exception
{
    public PromptTemplateException(PromptTemplateFault fault, string message)
        : base(message)
    {
        Fault = fault;
    }

    /// <summary>Which rule was broken.</summary>
    public PromptTemplateFault Fault { get; }
}
