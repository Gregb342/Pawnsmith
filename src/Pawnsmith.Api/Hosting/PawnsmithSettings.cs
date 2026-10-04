using System.Globalization;

using Pawnsmith.Infrastructure.Logging;

namespace Pawnsmith.Api.Hosting;

/// <summary>
/// The settings the host reads, all in one place (§G.2.1, DEC-087).
/// </summary>
/// <remarks>
/// <para>
/// <b>Read key by key, with the default beside each key.</b> ASP.NET could bind
/// the <c>Pawnsmith</c> section onto this record by reflection, matching
/// property names to keys; that is exactly the kind of hidden convention §2 of
/// CLAUDE.md asks to avoid — a renamed property would silently stop reading its
/// key. A dozen explicit reads are shorter to review than one binding to trust.
/// </para>
/// <para>
/// The sources are ASP.NET's own: <c>appsettings.json</c>, then the environment
/// (<c>Pawnsmith__Generator__Url=…</c>), then the command line. Every operator
/// of a container already knows how to pass an environment variable, and it
/// costs no reading code.
/// </para>
/// </remarks>
/// <param name="ProjectsRoot">Absolute folder holding every project.</param>
/// <param name="ConfigDirectory">Absolute folder holding calibration, catalogues and templates.</param>
/// <param name="GeneratorUrl">The ComfyUI address, or null when generation is not configured.</param>
/// <param name="WorkflowFile">Absolute path of the workflow template.</param>
/// <param name="MaxUploadBytes">Largest archive accepted by the import.</param>
/// <param name="MaxRequestBytes">Largest body of any other request.</param>
/// <param name="Logs">Where the log files go, and how many are kept (§H.3.2).</param>
public sealed record PawnsmithSettings(
    string ProjectsRoot,
    string ConfigDirectory,
    string UserDirectory,
    string? GeneratorUrl,
    string WorkflowFile,
    long MaxUploadBytes,
    long MaxRequestBytes,
    LogOptions Logs)
{
    /// <summary>Reads the settings, resolving relative paths against the application's folder.</summary>
    /// <param name="configuration">ASP.NET's configuration.</param>
    /// <param name="contentRoot">The application's folder — <c>/app</c> in the container.</param>
    public static PawnsmithSettings From(IConfiguration configuration, string contentRoot)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrEmpty(contentRoot);

        string? url = configuration["Pawnsmith:Generator:Url"];
        string? upload = configuration["Pawnsmith:MaxUploadBytes"];
        string? request = configuration["Pawnsmith:MaxRequestBytes"];
        string? logsEnabled = configuration["Pawnsmith:Logs:Enabled"];
        string? retained = configuration["Pawnsmith:Logs:RetainedFileCount"];
        string? fileSize = configuration["Pawnsmith:Logs:FileSizeLimitBytes"];

        return new PawnsmithSettings(
            ProjectsRoot: Resolve(contentRoot, configuration["Pawnsmith:ProjectsRoot"] ?? "data/projects"),
            ConfigDirectory: Resolve(contentRoot, configuration["Pawnsmith:ConfigDirectory"] ?? "config"),

            // The user's own files - personal catalogue, personal styles,
            // generator address (§I.4.2). Beside the projects, never inside
            // one: no archive carries them (DEC-022). In the container, the
            // volume /app/data/user.
            UserDirectory: Resolve(contentRoot, configuration["Pawnsmith:UserDirectory"] ?? "data/user"),
            GeneratorUrl: string.IsNullOrWhiteSpace(url) ? null : url.Trim(),
            WorkflowFile: Resolve(contentRoot, configuration["Pawnsmith:Generator:WorkflowFile"] ?? "config/workflow.comfyui.json"),

            // One gibibyte: a Backup archive of a large project is legitimately
            // big, and the import applies its own bounds afterwards (C.9.3).
            MaxUploadBytes: upload is null ? 1024L * 1024 * 1024 : long.Parse(upload, NumberStyles.None, CultureInfo.InvariantCulture),

            // One mebibyte: the largest JSON request - a blueprint, settings, a
            // list of twenty seeds - is a few kilobytes.
            MaxRequestBytes: request is null ? 1024L * 1024 : long.Parse(request, NumberStyles.None, CultureInfo.InvariantCulture),

            Logs: new LogOptions(
                // Beside the projects, never inside one: a shared archive must
                // not carry the logs (DEC-022). In the container, the volume
                // /app/data/logs.
                Directory: Resolve(contentRoot, configuration["Pawnsmith:Logs:Directory"] ?? "data/logs"),
                Enabled: logsEnabled is null || bool.Parse(logsEnabled),

                // A month of daily files, each closed at fifty mebibytes: the
                // logs never take more than about 1.5 GiB (DEC-091).
                RetainedFileCount: retained is null ? 31 : int.Parse(retained, NumberStyles.None, CultureInfo.InvariantCulture),
                FileSizeLimitBytes: fileSize is null ? 50L * 1024 * 1024 : long.Parse(fileSize, NumberStyles.None, CultureInfo.InvariantCulture)));
    }

    // Path.Combine returns the second argument when it is absolute, so an
    // absolute setting is taken as written.
    private static string Resolve(string contentRoot, string path) =>
        Path.GetFullPath(Path.Combine(contentRoot, path));
}
