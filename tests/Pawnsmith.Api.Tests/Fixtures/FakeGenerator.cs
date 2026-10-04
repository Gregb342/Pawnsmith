using Pawnsmith.Api.Hosting;
using Pawnsmith.Application.Ports;

namespace Pawnsmith.Api.Tests.Fixtures;

/// <summary>
/// A generator for the API tests, written by hand: answers its health check as
/// told, returns a real PNG, and can be held to show what runs at the same time.
/// </summary>
internal sealed class FakeGenerator : IImageGenerator
{
    public const string Framing = "front view on the left and back view on the right";

    private int running;

    /// <summary>What the health check answers.</summary>
    public GeneratorAvailability Availability { get; set; } = GeneratorAvailability.Available;

    /// <summary>When set, generations from <see cref="HoldFromCall"/> on wait for it before returning.</summary>
    public TaskCompletionSource? Hold { get; set; }

    /// <summary>The first call, counted from zero, that waits for <see cref="Hold"/>.</summary>
    public int HoldFromCall { get; set; }

    /// <summary>The most generations ever seen running at once.</summary>
    public int MostAtOnce { get; private set; }

    /// <summary>
    /// The PNG every generation returns: by default a plain grey image, which
    /// the cut-out refuses (no subject); a test sets a real scene when it needs
    /// candidates to come out cut out (T5).
    /// </summary>
    public byte[] Png { get; set; } = TestPng.Create(12, 8);

    /// <summary>When set, thrown by every call — the health check and the generations.</summary>
    public Exception? Failure { get; set; }

    /// <summary>How many generations started.</summary>
    public int Started { get; private set; }

    /// <summary>The setup that composes this generator into the host.</summary>
    public GeneratorSetup Setup() => GeneratorSetup.Configured(this, Framing, "http://comfy.test:8188/");

    public Task<GeneratorAvailability> CheckAsync(CancellationToken cancellationToken) =>
        Failure is null ? Task.FromResult(Availability) : Task.FromException<GeneratorAvailability>(Failure);

    public async Task<GeneratedImage> GenerateAsync(GenerationRequest request, CancellationToken cancellationToken)
    {
        int now = Interlocked.Increment(ref running);
        MostAtOnce = Math.Max(MostAtOnce, now);
        int call = Started++;

        try
        {
            if (Failure is not null)
            {
                throw Failure;
            }

            if (Hold is { } hold && call >= HoldFromCall)
            {
                await hold.Task.WaitAsync(cancellationToken);
            }

            return new GeneratedImage(Png, 12, 8);
        }
        finally
        {
            Interlocked.Decrement(ref running);
        }
    }
}
