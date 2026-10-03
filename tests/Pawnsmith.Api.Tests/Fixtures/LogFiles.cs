using System.Text.Json.Nodes;

namespace Pawnsmith.Api.Tests.Fixtures;

/// <summary>Reads the log files a host wrote, while it may still be writing them.</summary>
internal static class LogFiles
{
    /// <summary>Every event of every file in the folder, oldest file first; none when the folder does not exist.</summary>
    public static IReadOnlyList<JsonNode> Events(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        List<JsonNode> events = [];

        foreach (string path in Directory.GetFiles(directory).Order(StringComparer.Ordinal))
        {
            // The sink keeps the current file open for writing; a reader must let it.
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);

            foreach (string line in reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                events.Add(JsonNode.Parse(line)!);
            }
        }

        return events;
    }

    /// <summary>The value of a property of an event, as text, or null when the event does not carry it.</summary>
    public static string? Property(JsonNode logEvent, string name) =>
        logEvent["Properties"]?[name]?.ToString();
}
