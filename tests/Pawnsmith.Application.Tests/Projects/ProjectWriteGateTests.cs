using Pawnsmith.Application.Projects;

namespace Pawnsmith.Application.Tests.Projects;

/// <summary>The per-project write gate of DEC-086.</summary>
public class ProjectWriteGateTests
{
    [Fact]
    public async Task TwoWritersOfOneProjectNeverOverlap()
    {
        var gate = new ProjectWriteGate();
        int inside = 0;
        int mostInside = 0;

        async Task<int> Work()
        {
            int now = Interlocked.Increment(ref inside);
            mostInside = Math.Max(mostInside, now);
            await Task.Delay(20);
            Interlocked.Decrement(ref inside);
            return now;
        }

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => gate.RunAsync("/projects/a", Work, CancellationToken.None)));

        mostInside.ShouldBe(1);
    }

    [Fact]
    public async Task TwoSpellingsOfOneFolderShareTheGate()
    {
        var gate = new ProjectWriteGate();
        var release = new TaskCompletionSource<int>();

        Task<int> holding = gate.RunAsync("/projects/a", () => release.Task, CancellationToken.None);
        Task<int> waiting = gate.RunAsync("/projects/x/../a", () => Task.FromResult(2), CancellationToken.None);

        await Task.Delay(50);
        waiting.IsCompleted.ShouldBeFalse();

        release.SetResult(1);
        (await waiting).ShouldBe(2);
        (await holding).ShouldBe(1);
    }

    [Fact]
    public async Task TwoProjectsDoNotWaitForEachOther()
    {
        var gate = new ProjectWriteGate();
        var release = new TaskCompletionSource<int>();

        Task<int> holding = gate.RunAsync("/projects/a", () => release.Task, CancellationToken.None);
        int other = await gate.RunAsync("/projects/b", () => Task.FromResult(7), CancellationToken.None);

        other.ShouldBe(7);
        release.SetResult(1);
        await holding;
    }

    [Fact]
    public async Task AFailedWriterReleasesTheGate()
    {
        var gate = new ProjectWriteGate();

        await Should.ThrowAsync<IOException>(() =>
            gate.RunAsync<int>("/projects/a", () => throw new IOException("disk"), CancellationToken.None));

        (await gate.RunAsync("/projects/a", () => Task.FromResult(3), CancellationToken.None)).ShouldBe(3);
    }
}
