namespace Pawnsmith.Domain;

/// <summary>
/// Turns a request into a fully resolved <see cref="SheetLayout"/>.
/// </summary>
/// <remarks>
/// This is the last domain step. After it, nothing is left to decide: the
/// renderer receives millimetres and traces them.
/// </remarks>
public static class SheetLayoutBuilder
{
    /// <summary>
    /// The calibration mark measures exactly this on paper (B.5.4).
    /// </summary>
    /// <remarks>
    /// <b>This constant is deliberate, and it must not move to the calibration
    /// file.</b> Physical values live in configuration because they are
    /// measured; this one is not measured, it is the yardstick everything else
    /// is measured against. Making it configurable would let someone change the
    /// ruler and produce a sheet whose own mark lies about the scale.
    /// </remarks>
    private const double CalibrationMarkLengthMm = 100.0;

    /// <summary>
    /// Height of the vertical tick at each end of the calibration mark.
    /// </summary>
    /// <remarks>
    /// <b>This value is invented.</b> B.5.4 requires a tick at each end without
    /// giving it a size. It is kept in code rather than in the calibration file
    /// on the grounds that it has no physical consequence: the tick exists so a
    /// ruler can be lined up on the ends of the mark, and its height changes
    /// nothing that gets measured. Should T0b show 4 mm is awkward to aim at,
    /// this becomes a calibration entry.
    /// </remarks>
    private const double CalibrationTickHeightMm = 4.0;

    /// <summary>
    /// Builds the layout.
    /// </summary>
    /// <param name="request">What to lay out.</param>
    /// <param name="calibration">Physical values, read from the calibration file.</param>
    /// <param name="imageSizes">
    /// Pixel dimensions of every image named by the request, keyed by file name.
    /// Measured by the caller: the domain never opens a file.
    /// </param>
    /// <exception cref="PageCapacityException">A size does not fit on the page at all.</exception>
    public static SheetLayout Build(
        SheetRequest request,
        Calibration calibration,
        IReadOnlyDictionary<string, SourceImageSize> imageSizes)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(calibration);
        ArgumentNullException.ThrowIfNull(imageSizes);

        IReadOnlyList<PagePlan> plans = Pagination.Plan(request, calibration);
        PageScale scale = new(
            calibration.Print.ScaleCorrectionFactor,
            request.PaperFormat.WidthMm,
            request.PaperFormat.HeightMm);

        List<SheetPage> pages = [];
        Dictionary<string, WidthLimitedItem> widthLimited = [];

        for (int index = 0; index < plans.Count; index++)
        {
            pages.Add(BuildPage(
                plans[index],
                request,
                calibration,
                imageSizes,
                scale,
                pageNumber: index + 1,
                pageCount: plans.Count,
                widthLimited));
        }

        return new SheetLayout(pages, calibration.Strokes, [.. widthLimited.Values]);
    }

    private static SheetPage BuildPage(
        PagePlan plan,
        SheetRequest request,
        Calibration calibration,
        IReadOnlyDictionary<string, SourceImageSize> imageSizes,
        PageScale scale,
        int pageNumber,
        int pageCount,
        Dictionary<string, WidthLimitedItem> widthLimited)
    {
        var unit = UnfoldedUnit.Create(
            plan.Size,
            calibration.Sizes[plan.Size],
            request.Geometry,
            calibration.Geometry);

        var grid = PageGrid.Create(request.PaperFormat, unit, calibration.Layout);

        List<PlacedUnit> placed = [];

        for (int index = 0; index < plan.Cells.Count; index++)
        {
            placed.Add(PlaceUnit(
                plan.Cells[index],
                unit,
                grid.CellOrigin(index),
                calibration.Layout,
                imageSizes,
                scale,
                widthLimited));
        }

        return new SheetPage(
            Size: plan.Size,
            Geometry: request.Geometry,
            PaperFormat: request.PaperFormat,
            PageNumber: pageNumber,
            PageCount: pageCount,
            Units: placed,
            CalibrationMark: BuildCalibrationMark(request.PaperFormat, calibration.Layout, scale));
    }

    /// <summary>
    /// Moves one unit's geometry from unit coordinates to page coordinates, and
    /// applies the printer correction.
    /// </summary>
    private static PlacedUnit PlaceUnit(
        PlannedCell cell,
        UnfoldedUnit unit,
        PointMm cellOriginMm,
        LayoutSettings layout,
        IReadOnlyDictionary<string, SourceImageSize> imageSizes,
        PageScale scale,
        Dictionary<string, WidthLimitedItem> widthLimited)
    {
        ImagePair pair = ImagePlacement.ForPair(
            unit,
            ImageSize(imageSizes, cell.Item.FrontImageFile),
            ImageSize(imageSizes, cell.Item.BackImageFile),
            layout);

        if (pair.IsWidthLimited)
        {
            // Keyed by item name so several copies of the same pawn report once.
            widthLimited[cell.Item.Name] = new WidthLimitedItem(
                cell.Item.Name,
                cell.Item.Size,
                Math.Max(pair.Front.HeightMm, pair.Back.HeightMm),
                pair.BoxHeightMm);
        }

        return new PlacedUnit(
            Item: cell.Item,
            CutOutlineMm:
            [
                .. unit.CutOutlineMm.Select(point => scale.Point(
                    cellOriginMm.XMm + point.XMm,
                    cellOriginMm.YMm + point.YMm)),
            ],
            FoldLines:
            [
                .. unit.FoldLinesYMm.Select(y => new FoldLine(
                    scale.Point(cellOriginMm.XMm, cellOriginMm.YMm + y),
                    scale.Point(cellOriginMm.XMm + unit.WidthMm, cellOriginMm.YMm + y))),
            ],
            FrontImage: Place(cell.Item.FrontImageFile, pair.Front, cellOriginMm, scale),
            BackImage: Place(cell.Item.BackImageFile, pair.Back, cellOriginMm, scale));
    }

    private static PlacedImage Place(
        string fileName,
        ImagePlacement placement,
        PointMm cellOriginMm,
        PageScale scale)
    {
        PointMm topLeft = scale.Point(
            cellOriginMm.XMm + placement.XMm,
            cellOriginMm.YMm + placement.YMm);

        return new PlacedImage(
            FileName: fileName,
            XMm: topLeft.XMm,
            YMm: topLeft.YMm,
            WidthMm: scale.Length(placement.WidthMm),
            HeightMm: scale.Length(placement.HeightMm),
            Rotation: placement.Rotation);
    }

    /// <summary>
    /// Places the 100 mm mark, centred in the calibration zone at the bottom of
    /// the page (B.5.4).
    /// </summary>
    private static CalibrationMark BuildCalibrationMark(
        PaperFormat paper,
        LayoutSettings layout,
        PageScale scale)
    {
        // The zone sits between the bottom margin and the usable area.
        double zoneTopMm = paper.HeightMm - layout.PageMarginMm - layout.CalibrationZoneHeightMm;
        double zoneCentreYMm = zoneTopMm + (layout.CalibrationZoneHeightMm / 2);
        double startXMm = (paper.WidthMm - CalibrationMarkLengthMm) / 2;

        return new CalibrationMark(
            StartMm: scale.Point(startXMm, zoneCentreYMm),
            EndMm: scale.Point(startXMm + CalibrationMarkLengthMm, zoneCentreYMm),
            TickHeightMm: scale.Length(CalibrationTickHeightMm),
            NominalLengthMm: CalibrationMarkLengthMm);
    }

    private static SourceImageSize ImageSize(
        IReadOnlyDictionary<string, SourceImageSize> imageSizes,
        string fileName)
    {
        if (!imageSizes.TryGetValue(fileName, out SourceImageSize? size))
        {
            throw new ArgumentOutOfRangeException(
                nameof(imageSizes),
                fileName,
                $"No dimensions were supplied for image '{fileName}'.");
        }

        return size;
    }
}
