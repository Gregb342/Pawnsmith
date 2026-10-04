namespace Pawnsmith.Application.Ports;

/// <summary>The two cut-outs of a paired image, as PNG files with a transparent background.</summary>
/// <param name="FrontPng">The front view, the left half (DEC-079).</param>
/// <param name="BackPng">The back view, the right half, still the right way up: T1 turns it at print time.</param>
public sealed record CutoutPair(byte[] FrontPng, byte[] BackPng);

/// <summary>
/// Cuts the two views of a paired image out of their background (§F.4, DEC-102).
/// </summary>
/// <remarks>
/// <para>
/// <b>It receives the paired image, not a half.</b> Splitting it (DEC-079)
/// needs decoded pixels, and decoding is Infrastructure's business; the
/// adapter applies the domain's split rule itself. This supersedes the
/// signature of chapter 7, written before the image was known to arrive paired.
/// </para>
/// <para>
/// One adapter in v1, which uses no model (DEC-098). A model would be a second
/// adapter behind this same port, nothing else would change.
/// </para>
/// </remarks>
public interface IBackgroundRemover
{
    /// <summary>Splits the paired image and cuts each half out.</summary>
    /// <exception cref="CutoutException">Any of the codes of §F.6, except <c>CANDIDATE_NO_PAIRED_IMAGE</c>.</exception>
    Task<CutoutPair> CutOutPairAsync(byte[] pairedPng, CancellationToken cancellationToken);
}
