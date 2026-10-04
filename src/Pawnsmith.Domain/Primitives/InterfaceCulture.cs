namespace Pawnsmith.Domain.Primitives;

/// <summary>
/// The languages of the interface in v1: French and English (chapter 10).
/// </summary>
/// <remarks>
/// A list rather than an enumeration because these are culture names, and
/// they travel as such — in a catalogue's labels, in the culture of a sheet.
/// Adding a language (EVO-007) adds a name here, and every catalogue must then
/// carry a label for it: the loader refuses one that does not (DEC-106).
/// </remarks>
public static class InterfaceCulture
{
    public const string English = "en";

    public const string French = "fr";

    /// <summary>In ordinal order, so that anything listing them is stable.</summary>
    public static IReadOnlyList<string> All { get; } = [English, French];
}
