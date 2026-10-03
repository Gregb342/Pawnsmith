namespace Pawnsmith.Infrastructure.Logging;

/// <summary>
/// Where the log files go, and how many are kept (§H.3.2, DEC-091).
/// </summary>
/// <remarks>
/// These values are arbitrated, not measured: they bound a disk volume, they
/// describe nothing physical. Their defaults are written once, by the host that
/// reads the settings.
/// </remarks>
/// <param name="Directory">Absolute folder of the log files. Never a project folder (DEC-022).</param>
/// <param name="Enabled">False: no file is written and no folder is created.</param>
/// <param name="RetainedFileCount">How many files are kept; the oldest go first.</param>
/// <param name="FileSizeLimitBytes">Size at which a file is closed and the next one opened.</param>
public sealed record LogOptions(
    string Directory,
    bool Enabled,
    int RetainedFileCount,
    long FileSizeLimitBytes);
