namespace Pawnsmith.Application.Generation;

/// <summary>
/// The bound of the batch that belongs to the application rather than to any
/// generator (§E.9, DEC-080).
/// </summary>
/// <remarks>
/// The timeouts live with the ComfyUI adapter, because they are properties of
/// that adapter: a remote provider (EVO-002) would have others. The cap on a
/// batch is a rule of the application, and a remote provider would share it.
/// </remarks>
public sealed record GenerationOptions
{
    /// <summary>The most candidates one batch may ask for (MEN-007).</summary>
    /// <remarks>
    /// Twenty generations of forty seconds make a quarter of an hour, which a
    /// user launches knowingly. Past that, one careless click keeps the graphics
    /// card busy for the evening.
    /// </remarks>
    public int MaxBatchSize { get; init; } = 20;
}

/// <summary>Draws seeds for a caller that has no preference.</summary>
/// <remarks>
/// <para>
/// The batch never draws its own seeds: replaying a given seed is a legitimate
/// use, and a use case that drew them would be untestable without substituting
/// its random source (§E.4.1). This is for the caller — the API, the command
/// line — that just wants N variations.
/// </para>
/// <para>
/// Uniform over <c>[0, 2^63 − 1)</c>, from <see cref="Random.Shared"/>. The
/// candidate stores a <c>ulong</c> and keeps the full range for a seed coming
/// from elsewhere; drawing takes 63 bits because that is what
/// <see cref="Random.NextInt64()"/> gives without bit manipulation, and nine
/// billion billion seeds are enough. Nothing here needs a cryptographic
/// source: a seed is not a secret, it is printed in every project file.
/// </para>
/// </remarks>
public static class RandomSeeds
{
    /// <summary>Draws <paramref name="count"/> seeds.</summary>
    public static IReadOnlyList<ulong> Draw(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        ulong[] seeds = new ulong[count];

        for (int index = 0; index < count; index++)
        {
            seeds[index] = (ulong)Random.Shared.NextInt64();
        }

        return seeds;
    }
}
