using Pawnsmith.Application.Ports;

namespace Pawnsmith.Application.Tests.Fixtures;

/// <summary>
/// A cut-out that follows a script, call by call, and records what it received.
/// Written by hand, like <see cref="ScriptedImageGenerator"/>.
/// </summary>
/// <remarks>
/// The default returns two recognisable byte arrays derived from the input, so
/// that a test can tell which image's cut-outs were written where.
/// </remarks>
internal sealed class ScriptedBackgroundRemover : IBackgroundRemover
{
    /// <summary>What call <i>n</i> does with the paired image it receives.</summary>
    public Func<int, byte[], Task<CutoutPair>> Script { get; set; } =
        (_, paired) => Task.FromResult(CutoutsOf(paired));

    /// <summary>Every paired image received, in order.</summary>
    public List<byte[]> Received { get; } = [];

    /// <summary>The cut-outs the default script returns for a paired image.</summary>
    public static CutoutPair CutoutsOf(byte[] paired) => new([.. paired, (byte)'F'], [.. paired, (byte)'B']);

    /// <summary>A script that refuses every image with the given code.</summary>
    public static Func<int, byte[], Task<CutoutPair>> Refusing(CutoutErrorCode code) =>
        (_, _) => Task.FromException<CutoutPair>(new CutoutException(code, $"Scripted refusal: {code}."));

    public Task<CutoutPair> CutOutPairAsync(byte[] pairedPng, CancellationToken cancellationToken)
    {
        int call = Received.Count;
        Received.Add(pairedPng);
        return Script(call, pairedPng);
    }
}
