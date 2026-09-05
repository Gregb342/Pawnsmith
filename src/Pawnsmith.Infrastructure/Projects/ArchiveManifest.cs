using System.Globalization;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// The <c>archive.json</c> that opens every archive.
/// </summary>
/// <remarks>
/// Four fields, and each has to earn its place rather than let
/// <c>project.json</c> do the work alone (C.8.3).
/// <para>
/// <b><c>archiveVersion</c></b> lets an import reject any old ZIP <i>before
/// extracting anything</i>, which is what MEN-001 requires — validate before
/// writing. <b><c>profile</c></b> tells the recipient what they are holding:
/// without it a <c>Share</c> is indistinguishable from a <c>Backup</c> whose
/// author never produced a PDF and kept no paired image, and re-exporting the
/// former as the latter would produce an archive claiming to be complete when
/// it is not. <b><c>createdAt</c></b> and <b><c>producedBy</c></b> are the least
/// a backup medium can say about itself.
/// </para>
/// <para>
/// It holds <b>neither <c>projectId</c> nor <c>name</c></b>: that would be
/// duplication, and duplication is an opportunity to diverge.
/// </para>
/// </remarks>
/// <param name="ArchiveVersion">Always 1 for now.</param>
/// <param name="Profile">The name of an <see cref="ArchiveProfile"/> member.</param>
/// <param name="CreatedAt">ISO 8601, UTC, <c>Z</c> suffix — same form as the project file.</param>
/// <param name="ProducedBy">The build that wrote it, for example <c>Pawnsmith 0.3.0</c>.</param>
public sealed record ArchiveManifest(
    [property: JsonPropertyName("archiveVersion")]
    int ArchiveVersion,
    [property: JsonPropertyName("profile")]
    string Profile,
    [property: JsonPropertyName("createdAt")]
    string CreatedAt,
    [property: JsonPropertyName("producedBy")]
    string ProducedBy);

/// <summary>Writes and reads <c>archive.json</c>.</summary>
public static class ArchiveManifestFile
{
    /// <summary>Name of the entry, and it is always the first one in the ZIP.</summary>
    /// <remarks>
    /// First so that it can be read from a stream without walking the whole
    /// archive — which is what lets an import refuse before extracting.
    /// </remarks>
    public const string EntryName = "archive.json";

    /// <summary>Archive format this build writes and reads.</summary>
    public const int SupportedArchiveVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        NewLine = "\n",
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>The manifest for an archive being written now.</summary>
    public static ArchiveManifest For(ArchiveProfile profile, DateTimeOffset createdAt) =>
        new(
            ArchiveVersion: SupportedArchiveVersion,
            Profile: profile.ToString(),
            CreatedAt: createdAt.ToUniversalTime().ToString(
                "yyyy-MM-dd'T'HH:mm:ss'Z'",
                CultureInfo.InvariantCulture),
            ProducedBy: ProducedBy());

    public static byte[] Serialize(ArchiveManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        return JsonSerializer.SerializeToUtf8Bytes(manifest, Options);
    }

    public static ArchiveManifest Deserialize(ReadOnlySpan<byte> utf8Json) =>
        JsonSerializer.Deserialize<ArchiveManifest>(utf8Json, Options)
        ?? throw new JsonException("The archive manifest is empty.");

    /// <summary>
    /// The name and version of the build writing the archive.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read <b>by reflection on the assembly</b>, never written as a literal: it
    /// is the only place that cannot drift from the binary actually produced
    /// (DEC-058). The number itself comes from <c>&lt;Version&gt;</c> in
    /// <c>Directory.Build.props</c>.
    /// </para>
    /// <para>
    /// <b>Nothing strips a source revision here, deliberately.</b> The SDK
    /// appends one to the informational version by default, giving
    /// <c>0.3.0+3f9a1c…</c>, and <c>Directory.Build.props</c> turns that off.
    /// Stripping it again at runtime would be a second line of defence that
    /// makes the first untestable — the test asserting no commit id reaches an
    /// archive would pass even if the build property were removed. Leaving the
    /// value raw means that test guards the property itself.
    /// </para>
    /// </remarks>
    private static string ProducedBy()
    {
        Assembly assembly = typeof(ArchiveManifestFile).Assembly;

        string version = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";

        return $"Pawnsmith {version}";
    }
}
