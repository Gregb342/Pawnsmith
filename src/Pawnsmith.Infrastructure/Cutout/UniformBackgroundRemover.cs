using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.Generation;
using Pawnsmith.Infrastructure.Imaging;

namespace Pawnsmith.Infrastructure.Cutout;

/// <summary>
/// The cut-out of v1: decode the paired image, split it by the domain's rule,
/// cut each half out of its uniform background, encode both (§F.3, §F.4).
/// </summary>
/// <remarks>
/// <b>Synchronous work behind an asynchronous port.</b> Nothing here waits on
/// a disk or a network: a flood fill on two million pixels is a fraction of a
/// second of processor time. The port is asynchronous so that a model, which
/// might run elsewhere, could sit behind it unchanged; this adapter simply
/// does the work and returns a completed task.
/// </remarks>
public sealed class UniformBackgroundRemover(CutoutOptions options) : IBackgroundRemover
{
    public Task<CutoutPair> CutOutPairAsync(byte[] pairedPng, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(pairedPng);

        RgbaImage pair = PngDecoder.Decode(pairedPng, options.MaxImageDimensionPx);

        if (!PairSplit.IsSplittable(pair.WidthPx, pair.HeightPx))
        {
            throw new CutoutException(
                CutoutErrorCode.ImageInvalid,
                $"The image is {pair.WidthPx} pixel(s) wide and cannot be split into a front and a back view (DEC-079).");
        }

        // Front on the left, back on the right; an odd width loses its middle
        // column, so that both halves have the same width (DEC-079, DEC-041).
        PairHalves halves = PairSplit.Of(pair.WidthPx, pair.HeightPx);

        RgbaImage front = UniformBackground.CutOut(Half(pair, halves.Front), options, cancellationToken);
        RgbaImage back = UniformBackground.CutOut(Half(pair, halves.Back), options, cancellationToken);

        // Re-encoded from the pixels: nothing of the source's metadata goes
        // along (DEC-079, DEC-099).
        return Task.FromResult(new CutoutPair(PngEncoder.Encode(front), PngEncoder.Encode(back)));
    }

    private static RgbaImage Half(RgbaImage pair, PixelRect rect) =>
        pair.Crop(rect.X, rect.Y, rect.Width, rect.Height);
}
