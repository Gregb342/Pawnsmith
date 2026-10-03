using Pawnsmith.Application.Ports;

namespace Pawnsmith.Application.Tests.Fixtures;

/// <summary>
/// A generator that follows a script, call by call, and records what it was
/// asked. Written by hand, like <see cref="InMemoryProjectRepository"/>.
/// </summary>
/// <remarks>
/// Call <i>n</i> (from zero) runs <see cref="Script"/>(<i>n</i>, token): the
/// default returns a recognisable image, and a test replaces it to throw, to
/// wait for a cancellation, or to act on the repository mid-batch.
/// </remarks>
internal sealed class ScriptedImageGenerator : IImageGenerator
{
    /// <summary>What call <i>n</i> does.</summary>
    public Func<int, CancellationToken, Task<GeneratedImage>> Script { get; set; } =
        (call, _) => Task.FromResult(ImageFor(call));

    /// <summary>Every request received, in order.</summary>
    public List<GenerationRequest> Requests { get; } = [];

    /// <summary>The bytes the default script returns for call <i>n</i>: distinct per call.</summary>
    public static GeneratedImage ImageFor(int call) => new([0x89, (byte)'P', (byte)call], 16, 8);

    public Task<GeneratorAvailability> CheckAsync(CancellationToken cancellationToken) =>
        Task.FromResult(GeneratorAvailability.Available);

    public Task<GeneratedImage> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken)
    {
        int call = Requests.Count;
        Requests.Add(request);
        return Script(call, cancellationToken);
    }
}

/// <summary>A clock that always says the same thing.</summary>
internal sealed class FixedClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
