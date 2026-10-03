using System.Collections.Concurrent;

namespace Pawnsmith.Application.Projects;

/// <summary>
/// One lock per project folder, so that two "load, modify, save" sequences on
/// the same project never interleave (§G.7.2, DEC-086).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why it exists now.</b> DEC-075 shrank the concurrency window of a batch
/// to a few milliseconds by reloading the project at every seed, and said
/// plainly that this was not a lock. The API creates the second real writer —
/// the user editing a blueprint while a batch appends a candidate — and two
/// sequences that both load, then both save, lose the first save. Everything
/// that writes a project goes through here.
/// </para>
/// <para>
/// <b>Not in the repository.</b> DEC-062 keeps the repository stateless, and a
/// lock is state. It also could not be there: what must not interleave is the
/// whole sequence, and the repository only sees its two ends.
/// </para>
/// <para>
/// <b>In the process only.</b> Two instances of Pawnsmith on one projects root
/// do not see each other's gates. That is not an intended use of a single-user
/// application (§1.5 of the bible), and a cross-process lock would be a file
/// lock with its own failure modes, for a case nobody has.
/// </para>
/// <para>
/// A semaphore per folder is created on first use and kept: there are as many
/// as there are projects touched since start-up, a few bytes each.
/// </para>
/// </remarks>
public sealed class ProjectWriteGate
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> gates = new(StringComparer.Ordinal);

    /// <summary>Runs <paramref name="work"/> alone among the writers of this project folder.</summary>
    /// <param name="projectDirectory">The folder; two spellings of the same full path share a gate.</param>
    /// <param name="work">The whole sequence — load, modify, save.</param>
    /// <param name="cancellationToken">Cancels the wait for the gate, never the work once it has started.</param>
    public async Task<T> RunAsync<T>(string projectDirectory, Func<Task<T>> work, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectDirectory);
        ArgumentNullException.ThrowIfNull(work);

        SemaphoreSlim gate = gates.GetOrAdd(Path.GetFullPath(projectDirectory), _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return await work().ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }
}
