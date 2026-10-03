using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Sheets;
using Pawnsmith.Domain.Units;
using Pawnsmith.Infrastructure.Imaging;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Imaging;

/// <summary>
/// MEN-005 on the sheet: an image too large is refused on its header, before
/// anything decodes it (DEC-095). Covers test 18 of H.8.
/// </summary>
public sealed class FileImageSizeReaderTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "pawnsmith-image-tests", Guid.NewGuid().ToString("N"));

    public FileImageSizeReaderTests() => Directory.CreateDirectory(directory);

    public void Dispose() => Directory.Delete(directory, recursive: true);

    /// <summary>Measures one image written as a header alone: nothing could decode it, and nothing needs to.</summary>
    private Task<IReadOnlyDictionary<string, SourceImageSize>> MeasureAsync(int widthPx, int heightPx)
    {
        File.WriteAllBytes(Path.Combine(directory, "front.png"), TestPng.HeaderOnly(widthPx, heightPx));
        var item = new SheetItem("goblin", Size.Medium, 1, "front.png", "front.png");

        return new FileImageSizeReader().MeasureAsync(directory, [item], CancellationToken.None);
    }

    [Theory]
    [InlineData(8192, 8192)]
    [InlineData(1, 8192)]
    public async Task AnImageAtTheBoundIsMeasured(int widthPx, int heightPx)
    {
        IReadOnlyDictionary<string, SourceImageSize> sizes = await MeasureAsync(widthPx, heightPx);

        sizes["front.png"].ShouldBe(new SourceImageSize(widthPx, heightPx));
    }

    [Theory]
    [InlineData(8193, 100)]
    [InlineData(100, 8193)]
    [InlineData(60000, 60000)]
    public async Task AnImageLongerThanTheBoundOnASideIsRefusedOnItsHeader(int widthPx, int heightPx)
    {
        ManifestException error = await Should.ThrowAsync<ManifestException>(() => MeasureAsync(widthPx, heightPx));

        error.Message.ShouldContain("front.png");
        error.Message.ShouldContain("8192");
    }
}
