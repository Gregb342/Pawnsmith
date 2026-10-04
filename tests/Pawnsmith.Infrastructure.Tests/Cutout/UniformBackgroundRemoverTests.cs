using Pawnsmith.Application.Ports;
using Pawnsmith.Infrastructure.Cutout;
using Pawnsmith.Infrastructure.Imaging;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Tests.Fixtures;

namespace Pawnsmith.Infrastructure.Tests.Cutout;

/// <summary>
/// The adapter splits the paired image and cuts both halves out, and the
/// repository writes them. Covers test 17 of F.9.
/// </summary>
public class UniformBackgroundRemoverTests
{
    private static readonly Rgb Red = new(180, 40, 40);
    private static readonly Rgb Blue = new(40, 60, 170);
    private static readonly Guid CandidateId = new("0c1d2e3f-4a5b-4c6d-8e7f-a0b1c2d3e4f5");

    private static readonly UniformBackgroundRemover Remover = new(new CutoutOptions());

    private static RgbaImage Decode(byte[] png) => PngDecoder.Decode(png, 8192);

    private static IEnumerable<Rgb> OpaqueColours(RgbaImage image)
    {
        for (int i = 0; i < image.Pixels.Length; i += 4)
        {
            if (image.Pixels[i + 3] == 255)
            {
                yield return new Rgb(image.Pixels[i], image.Pixels[i + 1], image.Pixels[i + 2]);
            }
        }
    }

    // --- F.9 n° 17 ---------------------------------------------------------------------------------------

    [Fact]
    public async Task TheFrontIsTheLeftHalfAndTheBackTheRightOneEachCutOut()
    {
        CutoutPair pair = await Remover.CutOutPairAsync(TestScene.PairPng(front: Red, back: Blue), CancellationToken.None);

        RgbaImage front = Decode(pair.FrontPng), back = Decode(pair.BackPng);

        OpaqueColours(front).Distinct().ShouldBe([Red]);
        OpaqueColours(back).Distinct().ShouldBe([Blue]);

        // Cropped to the figure: 20 wide, from the head (y 10) to the bottom.
        front.WidthPx.ShouldBe(20);
        front.HeightPx.ShouldBe(190);
        back.WidthPx.ShouldBe(20);
    }

    [Fact]
    public async Task AnOddWidthLosesItsMiddleColumn()
    {
        // 121 pixels wide: two halves of 60, the middle column dropped
        // (DEC-079). A figure in the dropped column would have shown.
        RgbaImage source = TestScene.Pair(Red, Blue, extraMiddleColumn: true);
        TestScene.Fill(source, 60, 0, 1, source.HeightPx, new Rgb(0, 255, 0));

        CutoutPair pair = await Remover.CutOutPairAsync(PngEncoder.Encode(source), CancellationToken.None);

        OpaqueColours(Decode(pair.FrontPng)).ShouldNotContain(new Rgb(0, 255, 0));
        OpaqueColours(Decode(pair.BackPng)).ShouldNotContain(new Rgb(0, 255, 0));
    }

    [Fact]
    public async Task APairedImageHeavierThanTheBoundIsRefusedBeforeDecoding()
    {
        var light = new UniformBackgroundRemover(new CutoutOptions { MaxImageBytes = 100 });

        CutoutException error = await Should.ThrowAsync<CutoutException>(() =>
            light.CutOutPairAsync(TestScene.PairPng(Red, Blue), CancellationToken.None));

        error.WireCode.ShouldBe("CUTOUT_IMAGE_TOO_LARGE");
    }

    [Fact]
    public async Task AnImageTooNarrowToSplitIsRefused()
    {
        var sliver = RgbaImage.Blank(1, 50);
        TestScene.Fill(sliver, 0, 0, 1, 50, TestScene.Grey);

        CutoutException error = await Should.ThrowAsync<CutoutException>(() =>
            Remover.CutOutPairAsync(PngEncoder.Encode(sliver), CancellationToken.None));

        error.WireCode.ShouldBe("CUTOUT_IMAGE_INVALID");
    }

    [Fact]
    public async Task TheCutOutsHaveATransparentBackgroundAndNoMetadata()
    {
        CutoutPair pair = await Remover.CutOutPairAsync(TestScene.PairPng(Red, Blue), CancellationToken.None);

        RgbaImage front = Decode(pair.FrontPng);

        // The corner beside the head is background: fully transparent.
        front.Pixels[front.Offset(0, 0) + 3].ShouldBe((byte)0);
        PngWriter.ChunkTypes(pair.FrontPng).ShouldBe(["IHDR", "IDAT", "IEND"]);
    }

    // --- Le dépôt écrit les détourages ------------------------------------------------------------------

    [Fact]
    public async Task BothCutOutsAreWrittenUnderTheirCandidateNames()
    {
        using TempWorkspace workspace = new();

        (string front, string back) = await ProjectImageFiles.WriteCutoutsAsync(
            workspace.Root, CandidateId, [1, 2], [3, 4], CancellationToken.None);

        front.ShouldBe($"images/{CandidateId}-front.png");
        back.ShouldBe($"images/{CandidateId}-back.png");
        (await File.ReadAllBytesAsync(Path.Combine(workspace.ImagesDirectory, $"{CandidateId}-back.png"))).ShouldBe([3, 4]);
    }

    [Fact]
    public async Task CuttingOutAgainReplacesTheCutOutsAndLeavesNoTemporary()
    {
        using TempWorkspace workspace = new();
        await ProjectImageFiles.WriteCutoutsAsync(workspace.Root, CandidateId, [1], [2], CancellationToken.None);

        await ProjectImageFiles.WriteCutoutsAsync(workspace.Root, CandidateId, [9], [8], CancellationToken.None);

        (await File.ReadAllBytesAsync(Path.Combine(workspace.ImagesDirectory, $"{CandidateId}-front.png"))).ShouldBe([9]);
        Directory.GetFiles(workspace.ImagesDirectory).Select(Path.GetFileName).Order(StringComparer.Ordinal)
            .ShouldBe([$"{CandidateId}-back.png", $"{CandidateId}-front.png"]);
    }

    [Fact]
    public async Task NothingIsWrittenThroughAnImagesFolderThatIsALink()
    {
        if (!SymbolicLinks.AreSupported)
        {
            return;
        }

        using TempWorkspace workspace = new();
        using TempWorkspace elsewhere = new();
        Directory.Delete(workspace.ImagesDirectory);
        SymbolicLinks.Create(workspace.ImagesDirectory, elsewhere.Root);

        ProjectException error = await Should.ThrowAsync<ProjectException>(() =>
            ProjectImageFiles.WriteCutoutsAsync(workspace.Root, CandidateId, [1], [2], CancellationToken.None));

        error.Code.ShouldBe(ProjectErrorCode.PathEscape);
        Directory.GetFiles(elsewhere.Root).ShouldBeEmpty();
    }
}
