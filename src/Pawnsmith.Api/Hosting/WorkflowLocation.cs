using Pawnsmith.Infrastructure.Generation;

namespace Pawnsmith.Api.Hosting;

/// <summary>
/// Where the workflow template is read from: the file imported in the
/// interface, or else the configuration's (§I.16.1, DEC-119).
/// </summary>
/// <remarks>
/// <b>The imported file wins</b>, as the address chosen in the interface wins
/// over the configuration's (DEC-108): it holds the user's last explicit
/// choice. Deleting it brings the configuration's file back.
/// </remarks>
public sealed class WorkflowLocation(PawnsmithSettings settings)
{
    /// <summary>The file name of an imported workflow, in the user directory.</summary>
    public const string ImportedFileName = "workflow.comfyui.json";

    /// <summary>Where an imported workflow is written.</summary>
    public string ImportedPath { get; } = Path.Combine(settings.UserDirectory, ImportedFileName);

    /// <summary>The shipped framing clause given to a raw export (DEC-120).</summary>
    public string FramingClausePath { get; } = Path.Combine(settings.ConfigDirectory, FramingClauseFile.FileName);

    /// <summary>The file in force: the imported one if there is one, else the configuration's.</summary>
    public string Current => File.Exists(ImportedPath) ? ImportedPath : settings.WorkflowFile;

    /// <summary>Where the workflow in force comes from: <c>Imported</c>, <c>Configuration</c>, or <c>None</c>.</summary>
    public string Source =>
        File.Exists(ImportedPath) ? "Imported"
        : File.Exists(settings.WorkflowFile) ? "Configuration"
        : "None";
}
