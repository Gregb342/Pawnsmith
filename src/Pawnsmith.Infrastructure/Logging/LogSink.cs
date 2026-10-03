using Serilog;
using Serilog.Core;
using Serilog.Formatting.Json;

namespace Pawnsmith.Infrastructure.Logging;

/// <summary>
/// Builds the one Serilog logger of the process: JSON lines in rotating files
/// (§H.3, DEC-091).
/// </summary>
/// <remarks>
/// <para>
/// <b>One JSON object per line.</b> The JSON formatter of Serilog's core writes
/// each event as a single line, every value escaped as a JSON string. That is
/// what chapter 8 asks for — Graylog can take it without parsing work — and it
/// is also the whole defence against forged log lines (MEN-011): a newline in a
/// project name becomes <c>\n</c> inside a string, never a second event.
/// <c>Serilog.Formatting.Compact</c> would give a terser shape for one more
/// package; the core formatter is enough.
/// </para>
/// <para>
/// <b>A file per day, and per size.</b> <c>pawnsmith-20261003.ndjson</c>, then
/// <c>pawnsmith-20261003_001.ndjson</c> when the size limit is reached. Without
/// rolling on size, the sink's default is to <i>stop writing</i> once the limit
/// is hit — a log that falls silent on the day something happens.
/// </para>
/// <para>
/// <b>The level is not decided here.</b> The logger accepts every level, and
/// ASP.NET's own <c>Logging:LogLevel</c> filters before an event reaches it:
/// one setting for the console and the files alike.
/// </para>
/// </remarks>
public static class LogSink
{
    /// <summary>The start of every log file name.</summary>
    public const string FilePrefix = "pawnsmith-";

    /// <summary>
    /// The extension of every log file: newline-delimited JSON. Not <c>.json</c>,
    /// because a whole file is a sequence of documents, not one document, and a
    /// tool that sees <c>.json</c> tries to read it in one piece and fails.
    /// </summary>
    public const string FileExtension = ".ndjson";

    /// <summary>
    /// The logger, or null when logging is disabled — in which case nothing is
    /// written and the folder is not even created.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">A count or a size below one.</exception>
    public static Logger? Create(LogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Enabled)
        {
            return null;
        }

        // Refused here, by the name of the setting, rather than by the sink
        // with a parameter name nobody configured.
        ArgumentOutOfRangeException.ThrowIfLessThan(options.RetainedFileCount, 1, "Pawnsmith:Logs:RetainedFileCount");
        ArgumentOutOfRangeException.ThrowIfLessThan(options.FileSizeLimitBytes, 1, "Pawnsmith:Logs:FileSizeLimitBytes");

        // The sink inserts the date, and the sequence number, between the
        // prefix and the extension of this template.
        string template = Path.Combine(options.Directory, FilePrefix + FileExtension);

        return new LoggerConfiguration()
            .MinimumLevel.Verbose()

            // What LogContext.PushProperty pushed - the job identifier - is
            // added to every event written while it is in scope (DEC-090).
            .Enrich.FromLogContext()
            .WriteTo.File(
                new JsonFormatter(renderMessage: true),
                template,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: options.RetainedFileCount,
                fileSizeLimitBytes: options.FileSizeLimitBytes,
                rollOnFileSizeLimit: true)
            .CreateLogger();
    }
}
