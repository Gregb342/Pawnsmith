using Pawnsmith.Infrastructure.Logging;

namespace Pawnsmith.Api.Contracts;

/// <summary>The log files, and whether logging is on (§H.6.1).</summary>
/// <remarks>
/// <c>Enabled</c> false does not empty the list: switching logging off stops
/// the writing, it does not erase what was written.
/// </remarks>
public sealed record LogListDto(bool Enabled, IReadOnlyList<LogFileDto> Files);

/// <summary>One log file.</summary>
public sealed record LogFileDto(string Name, long SizeBytes, DateTimeOffset LastWriteUtc);

/// <summary>The end of a log file. Each line is the raw JSON event, as a string.</summary>
public sealed record LogTailDto(string Name, IReadOnlyList<string> Lines, bool Truncated);

/// <summary>The manual mapping of DEC-021.</summary>
public static class LogDtoMapping
{
    public static LogFileDto ToDto(this LogFile file) => new(file.Name, file.SizeBytes, file.LastWriteUtc);

    public static LogTailDto ToDto(this LogTail tail) => new(tail.Name, tail.Lines, tail.Truncated);
}
