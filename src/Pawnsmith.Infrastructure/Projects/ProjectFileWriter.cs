namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// Replaces <c>project.json</c> in one step, so an interrupted write can never
/// leave a truncated file behind.
/// </summary>
/// <remarks>
/// <para>
/// <b>The project folder is the user's only copy</b> (C.3.3). Writing in place
/// means that a full disk, a killed process or a crash halfway through leaves a
/// half-written <c>project.json</c> — and the work is gone, with nothing to fall
/// back on. So the bytes go to a temporary file in the <b>same folder</b>, and
/// the temporary file then takes the place of the real one in a single
/// operation.
/// </para>
/// <para>
/// The same folder matters and is not a detail: a temporary file on another
/// volume cannot be moved into place atomically, only copied — which is the very
/// half-written write this class exists to avoid.
/// </para>
/// <para>
/// <b>What this protects against, and what it does not.</b> It protects against
/// <i>corruption</i>. It does not protect against <i>loss</i>: with no locking,
/// the last writer wins. C.7.3 says so explicitly and defers the question to T6,
/// because before an API and two browser tabs there is no concurrent caller.
/// </para>
/// </remarks>
public static class ProjectFileWriter
{
    /// <summary>Name of the project file inside a project folder (DEC-046).</summary>
    public const string FileName = "project.json";

    /// <summary>Name of the file the bytes are staged in before replacement.</summary>
    /// <remarks>
    /// A fixed name rather than a random one. It is predictable, so a leftover
    /// after a crash is recognisable rather than being one of a hundred stray
    /// files, and it makes the atomic replacement testable. The trade-off is
    /// that two simultaneous writes to one project would fight over it — which
    /// C.7.3 already excludes until T6.
    /// </remarks>
    public const string TemporaryFileName = "project.json.tmp";

    /// <summary>Writes the bytes as the project file of <paramref name="projectDirectory"/>.</summary>
    /// <param name="projectDirectory">The project folder. Must exist.</param>
    /// <param name="content">The bytes to write, as produced by <see cref="ProjectJson.Serialize"/>.</param>
    public static async Task WriteAsync(
        string projectDirectory,
        byte[] content,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectDirectory);
        ArgumentNullException.ThrowIfNull(content);

        string target = Path.Combine(projectDirectory, FileName);
        string temporary = Path.Combine(projectDirectory, TemporaryFileName);

        await File.WriteAllBytesAsync(temporary, content, cancellationToken).ConfigureAwait(false);

        try
        {
            Replace(temporary, target);
        }
        catch
        {
            // The staged file is of no use to anyone once the replacement has
            // failed, and leaving it behind would make the next attempt look
            // like it had crashed. Deleting it must not mask the real failure,
            // hence the swallow and the rethrow.
            TryDelete(temporary);
            throw;
        }
    }

    private static void Replace(string temporary, string target)
    {
        if (File.Exists(target))
        {
            // File.Replace rather than a delete followed by a move: it is the
            // one call that swaps the two in a single filesystem operation, so
            // there is no instant at which the project has no file at all.
            // The third argument is the backup path, and null means no backup -
            // C.7.3 rules out a .bak, because the user's backup is the archive
            // of C.8 and two half-reliable mechanisms are worse than one.
            File.Replace(temporary, target, destinationBackupFileName: null);
        }
        else
        {
            File.Move(temporary, target);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            // Nothing useful to do: the caller is already being handed the
            // failure that matters.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
