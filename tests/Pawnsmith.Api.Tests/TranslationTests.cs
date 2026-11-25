using System.Text.Json.Nodes;

using Pawnsmith.Application.Generation;
using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.Jobs;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;
using Pawnsmith.Domain.Sheets;
using Pawnsmith.Infrastructure.Projects;

namespace Pawnsmith.Api.Tests;

/// <summary>
/// Every code the API can send, and every value the screens show by key, has
/// a label in both catalogues of the front (I.15.4, chapter 10).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a .NET test reads the front's JSON.</b> The codes live in the C#;
/// the labels in <c>fr.json</c> and <c>en.json</c>. The parity script of the
/// front checks that both catalogues have the same keys, not that a code
/// added on this side <i>has</i> one: a new code would reach the screen in
/// capitals. The one place that knows every code is here, so the check is
/// here.
/// </para>
/// <para>
/// The keys are the ones the screens build: <c>errors.{CODE}</c>,
/// <c>sizes.{Size}</c>, <c>geometries.{Geometry}.name</c>, and so on. A
/// screen that changed its key pattern would have to change this test with
/// it, which is the point.
/// </para>
/// </remarks>
public class TranslationTests
{
    private static readonly IReadOnlyList<string> Languages = ["fr", "en"];

    /// <summary>Every code that reaches the front, from the API, a failed job, or the front itself.</summary>
    public static TheoryData<string> Codes()
    {
        IEnumerable<string> codes =
        [
            // Every code a request can answer with, the very list of G.13 n° 2.
            .. ((IEnumerable<object[]>)HostTests.KnownCodes()).Select(row => (string)row[0]),
            "INTERNAL_ERROR",

            // A job fails with these, read by the Generation step (DEC-074).
            .. Enum.GetValues<GeneratorErrorCode>().Select(code => code.ToWireCode()),
            CandidateGeneration.UnexpectedErrorCode,

            // Made up by the front itself (api/client.ts, app/SaveStatus.tsx):
            // the server cannot be reached, an answer the server wrote itself
            // without the { code } shape, anything else thrown.
            "NETWORK_ERROR",
            "HTTP_400",
            "HTTP_413",
            "UNEXPECTED",
        ];

        return [.. codes.Distinct(StringComparer.Ordinal)];
    }

    /// <summary>Every value the screens show through a key built from an enumeration.</summary>
    public static TheoryData<string> EnumerationKeys()
    {
        IEnumerable<string> keys =
        [
            .. Enum.GetNames<Size>().Select(name => $"sizes.{name}"),
            .. Enum.GetNames<Geometry>().Select(name => $"geometries.{name}.name"),
            .. Enum.GetNames<Geometry>().Select(name => $"geometries.{name}.hint"),
            .. Enum.GetNames<Universe>().Select(name => $"universes.{name}"),
            .. Enum.GetNames<ClauseKind>().Select(name => $"clauses.{name}"),
            .. Enum.GetNames<CandidateStatus>().Select(name => $"generation.statuses.{name}"),
            .. Enum.GetNames<JobState>().Select(name => $"generation.states.{name}"),
            .. Enum.GetNames<SkipReason>().Select(name => $"layout.skip.{name}"),
            .. Enum.GetNames<ArchiveProfileKind>().Select(name => $"project.profiles.{name}.name"),
            .. Enum.GetNames<ArchiveProfileKind>().Select(name => $"project.profiles.{name}.hint"),

            // What a load says about a project without refusing it (DEC-056), on the Project step.
            .. Enum.GetNames<ProjectDiagnosticKind>().Select(name => $"project.diagnostics.{name}"),

            // The state of GET /api/generator: the live check, or why there is none.
            .. Enum.GetNames<GeneratorAvailability>().Select(name => $"generator.states.{name}"),
            "generator.states.NotConfigured",
            "generator.states.Misconfigured",
        ];

        return [.. keys];
    }

    [Theory]
    [MemberData(nameof(Codes))]
    public void EveryCodeIsTranslatedInBothLanguages(string code)
    {
        foreach (string language in Languages)
        {
            Label(language, $"errors.{code}").ShouldNotBeNullOrWhiteSpace($"errors.{code} is missing in {language}.json");
        }
    }

    [Theory]
    [MemberData(nameof(EnumerationKeys))]
    public void EveryShownValueIsTranslatedInBothLanguages(string key)
    {
        foreach (string language in Languages)
        {
            Label(language, key).ShouldNotBeNullOrWhiteSpace($"{key} is missing in {language}.json");
        }
    }

    /// <summary>The label at a dotted key of a catalogue, or null.</summary>
    private static string? Label(string language, string key)
    {
        JsonNode? node = Catalogue(language);

        foreach (string part in key.Split('.'))
        {
            node = node is JsonObject parent && parent.TryGetPropertyValue(part, out JsonNode? child) ? child : null;
        }

        return node is JsonValue value && value.TryGetValue(out string? text) ? text : null;
    }

    private static JsonNode Catalogue(string language)
    {
        string path = Path.Combine(RepositoryRoot(), "src", "Pawnsmith.Web", "src", "i18n", "locales", $"{language}.json");
        return JsonNode.Parse(File.ReadAllText(path))!;
    }

    private static string RepositoryRoot()
    {
        string? directory = AppContext.BaseDirectory;

        while (directory is not null && !File.Exists(Path.Combine(directory, "Pawnsmith.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        return directory ?? throw new InvalidOperationException("Pawnsmith.sln not found above the test assembly.");
    }
}
