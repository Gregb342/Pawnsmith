using System.Globalization;

using Pawnsmith.Application.Ports;
using Pawnsmith.Infrastructure.Imaging;

namespace Pawnsmith.Infrastructure.Cutout;

/// <summary>An opaque colour, one byte per channel.</summary>
public readonly record struct Rgb(byte R, byte G, byte B);

/// <summary>
/// Cuts a character out of a uniform background, without any model
/// (§F.3, DEC-098, DEC-100).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this works at all.</b> The framing clause asks the generator for a
/// flat, plain, pale grey background, with no scenery and no cast shadow
/// (DEC-076), and T0a measured it uniform at 83 % at worst. On such a picture,
/// whatever touches the edge of the image and looks like the background
/// <i>is</i> background.
/// </para>
/// <para>
/// <b>The order of the steps matters</b>, and <see cref="CutOut"/> is that
/// order: the colour of the background, then the ground band, then the flood
/// fill, the enclosed holes, the soft edge and the crop. Each step is public
/// so that it can be tested on its own, on images built in code.
/// </para>
/// </remarks>
public static class UniformBackground
{
    /// <summary>Cuts the subject out of one half of a paired image.</summary>
    /// <returns>A new image, cropped to the subject, transparent where the background was.</returns>
    /// <exception cref="CutoutException"><c>CUTOUT_BACKGROUND_NOT_UNIFORM</c> or <c>CUTOUT_SUBJECT_NOT_FOUND</c>.</exception>
    public static RgbaImage CutOut(RgbaImage half, CutoutOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(half);
        ArgumentNullException.ThrowIfNull(options);

        Rgb background = BackgroundColour(half);
        RequireUniform(half, background, options);

        // The ground band is removed first: kept, it would close the gap
        // between the legs and the flood fill could not reach it (§F.3.3).
        int keptRows = RowsAboveGroundBand(half, background, options);
        RgbaImage image = half.Crop(0, 0, half.WidthPx, keptRows);
        cancellationToken.ThrowIfCancellationRequested();

        bool[] isBackground = FloodFill(image, background, options.BackgroundTolerance);
        RemoveEnclosedHoles(image, background, isBackground, options);
        cancellationToken.ThrowIfCancellationRequested();

        int subjectPixels = ApplyAlpha(image, background, isBackground, options.BackgroundTolerance);
        long halfPixels = (long)half.WidthPx * half.HeightPx;

        if (subjectPixels == 0 || subjectPixels < options.MinSubjectFraction * halfPixels)
        {
            throw new CutoutException(
                CutoutErrorCode.SubjectNotFound,
                string.Create(CultureInfo.InvariantCulture,
                    $"Only {subjectPixels} of {halfPixels} pixels are left once the background is removed; under {options.MinSubjectFraction:P1}, no character was found (DEC-100)."));
        }

        return CropToSubject(image);
    }

    /// <summary>
    /// The colour of the background: the median, channel by channel, of the
    /// top, left and right border pixels (§F.3.1).
    /// </summary>
    /// <remarks>
    /// Not the bottom border: the feet and the ground band are there. The
    /// median rather than the mean, so that a sword tip touching the top edge
    /// does not shift the colour.
    /// </remarks>
    public static Rgb BackgroundColour(RgbaImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        // A histogram per channel: the median of 256 possible values without
        // sorting anything, and the same answer on every machine.
        int[] red = new int[256], green = new int[256], blue = new int[256];
        int count = 0;

        foreach ((int x, int y) in BorderPixels(image))
        {
            int at = image.Offset(x, y);
            red[image.Pixels[at]]++;
            green[image.Pixels[at + 1]]++;
            blue[image.Pixels[at + 2]]++;
            count++;
        }

        return new Rgb(Median(red, count), Median(green, count), Median(blue, count));
    }

    /// <summary>The largest difference on one channel, from 0 to 255 (Chebyshev distance).</summary>
    public static int Distance(RgbaImage image, int offset, Rgb colour) =>
        Math.Max(
            Math.Abs(image.Pixels[offset] - colour.R),
            Math.Max(Math.Abs(image.Pixels[offset + 1] - colour.G), Math.Abs(image.Pixels[offset + 2] - colour.B)));

    /// <summary>Refuses a border that is not a uniform background (§F.3.1).</summary>
    /// <exception cref="CutoutException"><c>CUTOUT_BACKGROUND_NOT_UNIFORM</c>.</exception>
    public static void RequireUniform(RgbaImage image, Rgb background, CutoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(options);

        int close = 0, total = 0;

        foreach ((int x, int y) in BorderPixels(image))
        {
            total++;

            if (Distance(image, image.Offset(x, y), background) <= options.BackgroundTolerance)
            {
                close++;
            }
        }

        double share = (double)close / total;

        if (share < options.MinBorderUniformity)
        {
            throw new CutoutException(
                CutoutErrorCode.BackgroundNotUniform,
                string.Create(CultureInfo.InvariantCulture,
                    $"Only {share:P0} of the top, left and right border looks like one background colour, under the {options.MinBorderUniformity:P0} required: the image has no uniform background to remove (DEC-100)."));
        }
    }

    /// <summary>
    /// The number of rows kept once the ground band is removed from the bottom
    /// (§F.3.2).
    /// </summary>
    /// <remarks>
    /// From the bottom up, a row whose pixels differ from the background on at
    /// least <see cref="CutoutOptions.GroundBandMinCoverage"/> of the width is
    /// ground. Removal stops at the first row that is not, or after
    /// <see cref="CutoutOptions.GroundBandMaxFraction"/> of the height. Kept, the
    /// band would become the line of the feet, since T1 aligns an image on its
    /// bottom edge.
    /// </remarks>
    public static int RowsAboveGroundBand(RgbaImage image, Rgb background, CutoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(options);

        int maxRemoved = (int)Math.Floor(image.HeightPx * options.GroundBandMaxFraction);
        int kept = image.HeightPx;

        while (image.HeightPx - kept < maxRemoved)
        {
            int y = kept - 1, differing = 0;

            for (int x = 0; x < image.WidthPx; x++)
            {
                if (Distance(image, image.Offset(x, y), background) > options.BackgroundTolerance)
                {
                    differing++;
                }
            }

            if (differing < options.GroundBandMinCoverage * image.WidthPx)
            {
                break;
            }

            kept--;
        }

        return kept;
    }

    /// <summary>
    /// Marks as background every pixel reachable from the four borders through
    /// neighbours under the tolerance (§F.3.3).
    /// </summary>
    /// <remarks>
    /// Breadth-first, four-connected — up, down, left, right, never diagonal,
    /// so that the background cannot slip between two subject pixels touching
    /// at a corner. The queue is an array of the image's size: every pixel
    /// enters it at most once.
    /// </remarks>
    public static bool[] FloodFill(RgbaImage image, Rgb background, int tolerance)
    {
        ArgumentNullException.ThrowIfNull(image);

        int width = image.WidthPx, height = image.HeightPx;
        bool[] visited = new bool[width * height];
        int[] queue = new int[width * height];
        int head = 0, tail = 0;

        void Seed(int x, int y)
        {
            int index = (y * width) + x;

            if (!visited[index] && Distance(image, index * RgbaImage.Channels, background) <= tolerance)
            {
                visited[index] = true;
                queue[tail++] = index;
            }
        }

        for (int x = 0; x < width; x++)
        {
            Seed(x, 0);
            Seed(x, height - 1);
        }

        for (int y = 0; y < height; y++)
        {
            Seed(0, y);
            Seed(width - 1, y);
        }

        while (head < tail)
        {
            int index = queue[head++];
            int x = index % width, y = index / width;

            if (x > 0)
            {
                Seed(x - 1, y);
            }

            if (x < width - 1)
            {
                Seed(x + 1, y);
            }

            if (y > 0)
            {
                Seed(x, y - 1);
            }

            if (y < height - 1)
            {
                Seed(x, y + 1);
            }
        }

        return visited;
    }

    /// <summary>
    /// Adds to the background the enclosed regions — not reached by the flood
    /// fill — that are very close to its colour and large enough (§F.3.4).
    /// </summary>
    public static void RemoveEnclosedHoles(RgbaImage image, Rgb background, bool[] isBackground, CutoutOptions options)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(isBackground);
        ArgumentNullException.ThrowIfNull(options);

        int width = image.WidthPx, height = image.HeightPx;
        int minArea = (int)Math.Ceiling(options.MinHoleFraction * width * height);
        bool[] seen = new bool[width * height];
        int[] region = new int[width * height];

        for (int start = 0; start < seen.Length; start++)
        {
            if (seen[start] || isBackground[start] || !Close(start))
            {
                continue;
            }

            // Collect one region of close-enough pixels, four-connected.
            int size = 0, head = 0;
            seen[start] = true;
            region[size++] = start;

            while (head < size)
            {
                int index = region[head++], x = index % width, y = index / width;

                if (x > 0)
                {
                    Visit(index - 1);
                }

                if (x < width - 1)
                {
                    Visit(index + 1);
                }

                if (y > 0)
                {
                    Visit(index - width);
                }

                if (y < height - 1)
                {
                    Visit(index + width);
                }
            }

            if (size >= minArea)
            {
                for (int i = 0; i < size; i++)
                {
                    isBackground[region[i]] = true;
                }
            }

            void Visit(int neighbour)
            {
                if (!seen[neighbour] && !isBackground[neighbour] && Close(neighbour))
                {
                    seen[neighbour] = true;
                    region[size++] = neighbour;
                }
            }
        }

        bool Close(int index) => Distance(image, index * RgbaImage.Channels, background) <= options.HoleTolerance;
    }

    /// <summary>
    /// Sets the opacity: background transparent, subject opaque, and the
    /// subject's edge proportional to its distance from the background
    /// (§F.3.5). Returns the number of pixels left visible.
    /// </summary>
    /// <remarks>
    /// An edge pixel — a subject pixel with a background neighbour — goes from
    /// transparent at the tolerance to opaque at twice the tolerance. Without
    /// it, the printed outline would be a staircase with a grey fringe.
    /// </remarks>
    public static int ApplyAlpha(RgbaImage image, Rgb background, bool[] isBackground, int tolerance)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(isBackground);

        int width = image.WidthPx, height = image.HeightPx, visible = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x, alphaAt = (index * RgbaImage.Channels) + 3;

                if (isBackground[index])
                {
                    image.Pixels[alphaAt] = 0;
                    continue;
                }

                bool edge = (x > 0 && isBackground[index - 1]) || (x < width - 1 && isBackground[index + 1])
                    || (y > 0 && isBackground[index - width]) || (y < height - 1 && isBackground[index + width]);

                if (edge)
                {
                    int distance = Distance(image, index * RgbaImage.Channels, background);
                    image.Pixels[alphaAt] = (byte)Math.Clamp((distance - tolerance) * 255 / Math.Max(1, tolerance), 0, 255);
                }
                else
                {
                    image.Pixels[alphaAt] = 255;
                }

                if (image.Pixels[alphaAt] > 0)
                {
                    visible++;
                }
            }
        }

        return visible;
    }

    /// <summary>
    /// Crops to the bounding box of the visible pixels (§F.3.6): its bottom is
    /// the line of the feet, which T1 aligns on (§B.4.4).
    /// </summary>
    public static RgbaImage CropToSubject(RgbaImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        int left = image.WidthPx, right = -1, top = image.HeightPx, bottom = -1;

        for (int y = 0; y < image.HeightPx; y++)
        {
            for (int x = 0; x < image.WidthPx; x++)
            {
                if (image.Pixels[image.Offset(x, y) + 3] > 0)
                {
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                    top = Math.Min(top, y);
                    bottom = Math.Max(bottom, y);
                }
            }
        }

        if (right < 0)
        {
            throw new CutoutException(CutoutErrorCode.SubjectNotFound, "Nothing is left once the background is removed (DEC-100).");
        }

        return image.Crop(left, top, right - left + 1, bottom - top + 1);
    }

    private static IEnumerable<(int X, int Y)> BorderPixels(RgbaImage image)
    {
        for (int x = 0; x < image.WidthPx; x++)
        {
            yield return (x, 0);
        }

        // Left and right columns, below the top row already counted.
        for (int y = 1; y < image.HeightPx; y++)
        {
            yield return (0, y);

            if (image.WidthPx > 1)
            {
                yield return (image.WidthPx - 1, y);
            }
        }
    }

    private static byte Median(int[] histogram, int count)
    {
        int half = (count + 1) / 2, seen = 0;

        for (int value = 0; value < 256; value++)
        {
            seen += histogram[value];

            if (seen >= half)
            {
                return (byte)value;
            }
        }

        return 255;
    }
}
