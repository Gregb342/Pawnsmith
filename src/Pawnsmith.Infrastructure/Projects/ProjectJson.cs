using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// The one set of JSON rules <c>project.json</c> is written and read with.
/// </summary>
/// <remarks>
/// <para>
/// Every rule here comes from C.3.3, and C.3.3 exists because DEC-011 justifies
/// storing a project in the clear with one precise word: <b>diffable</b>. A file
/// that is unreadable in <c>git diff</c> does not keep that promise, and a file
/// that changes without the project changing makes the diff worthless.
/// </para>
/// <para>
/// There is one options object, used for both directions. Two would eventually
/// disagree, and a file written under one set of rules and read under another is
/// the kind of defect that only shows on someone else's machine.
/// </para>
/// </remarks>
public static class ProjectJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        // Indented with two spaces, so a diff shows the line that changed
        // rather than the whole file.
        WriteIndented = true,

        // The line separator is set explicitly, and it has to be. The default
        // is Environment.NewLine, so the same project saved on Windows and on
        // Linux would differ on every single line - a diff of pure noise, and
        // an archive that never round-trips byte for byte. C.3.3 asks for a
        // bare line feed because a project travels between machines, and a
        // test asserts it: the first version of this file assumed the default
        // was already "\n", and it was not.
        NewLine = "\n",

        // Writes non-ASCII characters literally. The default encoder escapes
        // everything outside ASCII, so "Griffe Noire" survives but an accent
        // becomes é - deterministic, so no test complains, and yet it
        // destroys the readability DEC-011 is built on. The name says "unsafe"
        // because the strict default exists for JSON injected into an HTML page
        // or a script; project.json is written to disk and read back by our own
        // deserialiser, never inlined into a page. If T6 ever serves project
        // content inside a page, escaping it is that context's job and must not
        // rely on the encoder chosen here.
        //
        // One residual, measured rather than assumed: a character outside the
        // Basic Multilingual Plane - an emoji, say - is escaped as a surrogate
        // pair whatever encoder is chosen. JavaScriptEncoder.Create over every
        // Unicode range does not fix it and is strictly worse: it escapes
        // apostrophes and angle brackets too, and "l\\u0027orc" in a French
        // project costs far more readability than one escaped emoji. The round
        // trip is exact either way, which is what test 18 actually asks for.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,

        // An unknown member is refused rather than ignored (C.6.3, DEC-048). It
        // is the opposite of what calibration.json does, and the difference is
        // intentional: the calibration is written by hand and gets annotated,
        // whereas project.json is written exclusively by this application, so an
        // unknown member means either corruption or a manual edit whose intent
        // cannot be honoured - and ignoring it would make it disappear at the
        // next save.
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,

        // Numbers are read strictly. Accepting "12.0" as a quoted number would
        // let two spellings of one value into the file.
        NumberHandling = JsonNumberHandling.Strict,

        // No comments and no trailing commas, unlike the calibration reader.
        // Same reason as above: nobody hand-writes this file.
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,

        PropertyNameCaseInsensitive = false,
    };

    /// <summary>The bytes to write for this document.</summary>
    /// <remarks>
    /// <para>
    /// UTF-8 <b>without a byte order mark</b>, which comes free from serialising
    /// straight to bytes: a BOM breaks byte-for-byte comparison and surprises
    /// Unix tooling.
    /// </para>
    /// <para>
    /// A final line feed is appended. C.3.3 does not ask for one; it is added
    /// because every Unix tool expects a text file to end with a newline, and
    /// without it <c>git diff</c> prints "\ No newline at end of file" on a file
    /// whose whole purpose is to be diffed.
    /// </para>
    /// <para>
    /// No culture appears anywhere in this method, and that is deliberate rather
    /// than lucky: <c>System.Text.Json</c> always writes numbers under the
    /// invariant culture. A <c>fr-FR</c> session writing <c>12,0</c> is the most
    /// banal .NET defect of this slice, and it is invisible for as long as you
    /// develop in English.
    /// </para>
    /// </remarks>
    public static byte[] Serialize(ProjectDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        byte[] json = JsonSerializer.SerializeToUtf8Bytes(document, Options);

        byte[] withNewline = new byte[json.Length + 1];
        json.CopyTo(withNewline, 0);
        withNewline[^1] = (byte)'\n';

        return withNewline;
    }

    /// <summary>Reads a document back from the bytes of a file.</summary>
    /// <remarks>
    /// Failures surface as <see cref="JsonException"/>, deliberately untouched
    /// here. Turning them into the error codes of C.11 needs to know which rule
    /// was broken, and that belongs to the reader, with the rest of the
    /// validation.
    /// </remarks>
    /// <exception cref="JsonException">The bytes are not a valid document.</exception>
    public static ProjectDocument Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        return JsonSerializer.Deserialize<ProjectDocument>(utf8Json, Options)
            ?? throw new JsonException("The project file is empty.");
    }
}
