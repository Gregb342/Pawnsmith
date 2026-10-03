using System.Text;
using System.Text.RegularExpressions;

namespace Pawnsmith.Infrastructure.Logging;

/// <summary>A log file the viewer may show.</summary>
public sealed record LogFile(string Name, long SizeBytes, DateTimeOffset LastWriteUtc);

/// <summary>The end of a log file, as complete lines in file order.</summary>
/// <param name="Name">The file it was read from.</param>
/// <param name="Lines">The lines, raw, never interpreted (MEN-011).</param>
/// <param name="Truncated">True when the file holds more than what is returned.</param>
public sealed record LogTail(string Name, IReadOnlyList<string> Lines, bool Truncated);

/// <summary>
/// Reads the log folder for the viewer, by whitelist only (§H.6, DEC-094, MEN-002).
/// </summary>
/// <remarks>
/// <para>
/// <b>A name received from a request is never turned into a path.</b> The folder
/// is enumerated; only regular files whose name matches the pattern are kept;
/// a requested name must equal one of them, and the path opened is the one the
/// enumeration produced. <c>../x</c>, <c>/etc/passwd</c> or an encoded variant
/// can only fail to be found — there is no concatenation for them to subvert.
/// </para>
/// <para>
/// <b>A symbolic link is left out even under a valid name</b>, as MEN-008 asks
/// of the export: a link planted in the log folder would otherwise let the
/// viewer read whatever it points to.
/// </para>
/// <para>
/// <b>Read from the end, and bounded.</b> At most <see cref="MaxReadBytes"/>
/// are read, whatever the size of the file: a log of fifty mebibytes is never
/// held whole in memory to show its last lines.
/// </para>
/// </remarks>
public sealed class LogDirectory(string directory)
{
    /// <summary>How many lines a read returns when the caller does not say.</summary>
    public const int DefaultLines = 500;

    /// <summary>The most lines one read returns.</summary>
    public const int MaxLines = 5000;

    /// <summary>The most bytes one read takes from the end of a file: four mebibytes.</summary>
    public const int MaxReadBytes = 4 * 1024 * 1024;

    /// <summary>
    /// <c>pawnsmith-YYYYMMDD.ndjson</c>, or with <c>_NNN</c> when a day rolled on
    /// size — exactly the names the sink writes (DEC-091).
    /// </summary>
    /// <remarks>
    /// <c>[0-9]</c> rather than <c>\d</c>, which in .NET also matches the
    /// digits of every other script; and <c>\z</c> rather than <c>$</c>, which
    /// also matches before a final newline.
    /// </remarks>
    private static readonly Regex FileName = new(
        @"\Apawnsmith-[0-9]{8}(_[0-9]{3,})?\.ndjson\z",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    /// <summary>The log files, newest first; none when the folder does not exist.</summary>
    /// <remarks>
    /// Sorted by name, which is chronological by construction — the date, then
    /// the sequence — rather than by modification time, which a copy or a
    /// restore changes.
    /// </remarks>
    public IReadOnlyList<LogFile> List() =>
        [.. Whitelisted()
            .OrderByDescending(file => file.Name, StringComparer.Ordinal)
            .Select(file => new LogFile(file.Name, file.Length, new DateTimeOffset(file.LastWriteTimeUtc, TimeSpan.Zero)))];

    /// <summary>The last lines of a log file, or null when no whitelisted file has that name.</summary>
    /// <param name="name">The name as the caller sent it.</param>
    /// <param name="lines">How many lines, from 1 to <see cref="MaxLines"/>.</param>
    public async Task<LogTail?> ReadTailAsync(string name, int lines, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentOutOfRangeException.ThrowIfLessThan(lines, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lines, MaxLines);

        // The path comes from the enumeration, never from the name received.
        if (Whitelisted().FirstOrDefault(file => string.Equals(file.Name, name, StringComparison.Ordinal)) is not FileInfo file)
        {
            return null;
        }

        // The sink holds the current file open for writing; reading must let it
        // keep writing, and retention delete an old file under the reader.
        await using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

        long start = Math.Max(0, stream.Length - MaxReadBytes);
        byte[] window = new byte[stream.Length - start];
        stream.Seek(start, SeekOrigin.Begin);
        int read = await stream.ReadAtLeastAsync(window, window.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);

        List<string> complete = CompleteLines(window.AsSpan(0, read), startsMidFile: start > 0);
        int skipped = Math.Max(0, complete.Count - lines);

        return new LogTail(file.Name, complete[skipped..], Truncated: start > 0 || skipped > 0);
    }

    private IEnumerable<FileInfo> Whitelisted()
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        return new DirectoryInfo(directory)
            .EnumerateFiles()
            .Where(file => FileName.IsMatch(file.Name) && file.LinkTarget is null);
    }

    /// <summary>The lines of a window, without the partial ones at its edges.</summary>
    /// <remarks>
    /// Split on the newline byte before decoding: in UTF-8, 0x0A never occurs
    /// inside a multi-byte character, so a window that starts in the middle of
    /// one loses it with the partial first line, and nothing is mis-decoded.
    /// </remarks>
    private static List<string> CompleteLines(ReadOnlySpan<byte> window, bool startsMidFile)
    {
        // A window cut from the middle of the file starts inside a line.
        if (startsMidFile)
        {
            int firstBreak = window.IndexOf((byte)'\n');
            window = firstBreak < 0 ? [] : window[(firstBreak + 1)..];
        }

        // A last line without its newline is still being written: left out.
        int lastBreak = window.LastIndexOf((byte)'\n');
        window = lastBreak < 0 ? [] : window[..lastBreak];

        if (window.IsEmpty)
        {
            return [];
        }

        // The sink ends lines with the platform's newline: \r\n on Windows.
        return [.. Encoding.UTF8.GetString(window).Split('\n').Select(line => line.TrimEnd('\r'))];
    }
}
