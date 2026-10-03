using System.Globalization;

using Pawnsmith.Application.PhysicalValues;
using Pawnsmith.Application.Ports;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;
using Pawnsmith.Domain.Sheets;
using Pawnsmith.Domain.Units;

namespace Pawnsmith.Application.Sheets;

/// <summary>One page of the sheet as the capacity indicator of §15.4 shows it.</summary>
/// <param name="Number">One-based, across the whole sheet.</param>
/// <param name="Size">The single size of the page (DEC-005).</param>
/// <param name="Capacity">How many cells the page holds, on the project's effective calibration.</param>
/// <param name="Used">How many of them are occupied.</param>
public sealed record ReportedPage(int Number, Size Size, int Capacity, int Used);

/// <summary>An elected candidate produced under clauses that are no longer the current ones.</summary>
/// <param name="BlueprintId">Its blueprint.</param>
/// <param name="CandidateId">The elected candidate.</param>
/// <param name="Clauses">Which clauses moved — never empty.</param>
public sealed record MisalignedElection(Guid BlueprintId, Guid CandidateId, IReadOnlyList<ClauseKind> Clauses);

/// <summary>Everything the sheet of a project has to say about itself before it is printed.</summary>
/// <param name="Pages">The pages, in order, with capacity and occupation.</param>
/// <param name="Skipped">Blueprints left off the sheet, and why (DEC-069).</param>
/// <param name="MisalignedElections">Elected candidates exported although misaligned (DEC-082).</param>
/// <param name="MisalignmentKnown">
/// False when the framing clause is unknown — no workflow configured — so that
/// misalignment could not be computed. Saying nothing then would read as
/// "everything is aligned".
/// </param>
/// <param name="WidthLimited">Images that print short because their width limits them (DEC-042).</param>
public sealed record SheetReport(
    IReadOnlyList<ReportedPage> Pages,
    IReadOnlyList<SkippedBlueprint> Skipped,
    IReadOnlyList<MisalignedElection> MisalignedElections,
    bool MisalignmentKnown,
    IReadOnlyList<WidthLimitedItem> WidthLimited);

/// <summary>The PDF of a project's sheet, and its report.</summary>
public sealed record ProjectSheetPdf(byte[] Pdf, SheetReport Report);

/// <summary>
/// The sheet of a project, from <c>project.json</c> to the report and the PDF
/// (§G.8).
/// </summary>
/// <remarks>
/// <para>
/// <b>What the CLI of T3 did by hand, given a home.</b> Effective calibration,
/// paper format, <see cref="ProjectSheetRequestBuilder"/>, measuring, layout,
/// rendering: the command line strung them together itself. It had no logic of
/// its own — it had borrowed one that had nowhere else to live. The API needs
/// the same sequence, and two copies of it would drift.
/// </para>
/// <para>
/// <b>An elected candidate that is misaligned is exported, and said</b>
/// (DEC-082, which closes question C). The image is not wrong; it was made
/// under another prompt, which is information, not a defect. Blocking would
/// force a regeneration to print; a confirmation would be the consent mechanism
/// DEC-030 rejected. The report lists each such candidate with the clauses
/// that moved — and says so when it cannot know.
/// </para>
/// <para>
/// <b>Capacity is computed on the effective calibration</b> (DEC-053): the tab
/// overrides of the project change the cell height, so the same sheet holds
/// different numbers of pawns in two projects.
/// </para>
/// </remarks>
public sealed class ProjectSheet
{
    private readonly IProjectRepository repository;
    private readonly IImageSizeReader imageSizeReader;
    private readonly Func<string, ISheetRenderer> rendererFor;

    /// <param name="repository">Where the project comes from.</param>
    /// <param name="imageSizeReader">Measures the elected images.</param>
    /// <param name="rendererFor">
    /// Builds the renderer for one project folder. A function and not a
    /// renderer, because the PDF renderer of T1 opens the images relative to a
    /// folder given at construction, and the folder changes with every project.
    /// </param>
    public ProjectSheet(
        IProjectRepository repository,
        IImageSizeReader imageSizeReader,
        Func<string, ISheetRenderer> rendererFor)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentNullException.ThrowIfNull(imageSizeReader);
        ArgumentNullException.ThrowIfNull(rendererFor);

        this.repository = repository;
        this.imageSizeReader = imageSizeReader;
        this.rendererFor = rendererFor;
    }

    /// <summary>Lays the sheet out, without rendering it, and reports on it.</summary>
    /// <param name="framingClause">The framing clause in force, or null when no workflow is configured.</param>
    /// <exception cref="SheetRuleException"><c>PAPER_FORMAT_UNKNOWN</c>, <c>SHEET_CAPACITY_EXCEEDED</c>.</exception>
    public async Task<SheetReport> ReportAsync(
        string projectDirectory,
        Calibration calibration,
        string? framingClause,
        CancellationToken cancellationToken)
    {
        Prepared prepared = await PrepareAsync(projectDirectory, calibration, framingClause, cancellationToken)
            .ConfigureAwait(false);

        if (prepared.Request.Items.Count == 0)
        {
            return prepared.ReportOf(layout: null);
        }

        IReadOnlyDictionary<string, SourceImageSize> sizes = await imageSizeReader
            .MeasureAsync(projectDirectory, prepared.Request.Items, cancellationToken)
            .ConfigureAwait(false);

        SheetLayout layout = Guard(() => SheetLayoutBuilder.Build(prepared.Request, prepared.Effective, sizes));

        return prepared.ReportOf(layout);
    }

    /// <summary>Renders the sheet, in the given culture, with its report.</summary>
    /// <exception cref="SheetRuleException">
    /// <c>PAPER_FORMAT_UNKNOWN</c>, <c>SHEET_CAPACITY_EXCEEDED</c>, or
    /// <c>SHEET_EMPTY</c> when no blueprint has an elected, cut-out candidate.
    /// </exception>
    public async Task<ProjectSheetPdf> RenderAsync(
        string projectDirectory,
        Calibration calibration,
        string? framingClause,
        CultureInfo culture,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(culture);

        Prepared prepared = await PrepareAsync(projectDirectory, calibration, framingClause, cancellationToken)
            .ConfigureAwait(false);

        if (prepared.Request.Items.Count == 0)
        {
            // Never an empty page (DEC-069): an empty sheet is paper lost.
            throw new SheetRuleException(
                SheetRuleCode.Empty,
                "No blueprint of the project has an elected, cut-out candidate; there is nothing to print.");
        }

        var useCase = new RenderSheetUseCase(imageSizeReader, rendererFor(projectDirectory));

        RenderedSheet rendered = await GuardAsync(() => useCase.ExecuteAsync(
            prepared.Request,
            prepared.Effective,
            projectDirectory,
            culture,
            cancellationToken)).ConfigureAwait(false);

        return new ProjectSheetPdf(rendered.Pdf, prepared.ReportOf(rendered.Layout));
    }

    private async Task<Prepared> PrepareAsync(
        string projectDirectory,
        Calibration calibration,
        string? framingClause,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(projectDirectory);
        ArgumentNullException.ThrowIfNull(calibration);

        LoadedProjectResult loaded = await repository
            .LoadAsync(projectDirectory, calibration, cancellationToken)
            .ConfigureAwait(false);

        Project project = loaded.Project;

        // The effective calibration, never the file's: the tab overrides feed
        // the cell height (DEC-040, DEC-053).
        Calibration effective = EffectiveCalibration.Resolve(calibration, project);

        if (!effective.PaperFormats.TryGetValue(project.PaperFormatName, out PaperFormat? paperFormat))
        {
            // A diagnostic when the project loads (DEC-056); an error only
            // here, where somebody asks for a sheet.
            throw new SheetRuleException(
                SheetRuleCode.PaperFormatUnknown,
                $"The project asks for the paper format '{project.PaperFormatName}', which the calibration does not declare.");
        }

        ProjectSheetRequest built = ProjectSheetRequestBuilder.From(project, paperFormat);

        return new Prepared(
            built.Request,
            effective,
            built.Skipped,
            MisalignedElections(project, framingClause),
            MisalignmentKnown: framingClause is not null);
    }

    /// <summary>The elected candidates whose frozen clauses differ from the current ones.</summary>
    private static List<MisalignedElection> MisalignedElections(Project project, string? framingClause)
    {
        if (framingClause is null)
        {
            return [];
        }

        List<MisalignedElection> misaligned = [];

        foreach (Blueprint blueprint in project.Blueprints)
        {
            Candidate? elected = blueprint.Candidates.FirstOrDefault(candidate => candidate.Id == blueprint.ElectedCandidateId);

            if (elected is null)
            {
                continue;
            }

            IReadOnlySet<ClauseKind> drifted = Misalignment.Of(elected, blueprint, project.Style, framingClause);

            if (drifted.Count > 0)
            {
                // In the enumeration's order, so that two reports of the same
                // project list the clauses the same way.
                misaligned.Add(new MisalignedElection(
                    blueprint.Id,
                    elected.Id,
                    [.. Enum.GetValues<ClauseKind>().Where(drifted.Contains)]));
            }
        }

        return misaligned;
    }

    /// <summary>A capacity of zero becomes a coded refusal (§5.4: it is a normal case, not an anomaly).</summary>
    private static T Guard<T>(Func<T> layout)
    {
        try
        {
            return layout();
        }
        catch (PageCapacityException error)
        {
            throw new SheetRuleException(SheetRuleCode.CapacityExceeded, error.Message, error);
        }
    }

    private static async Task<T> GuardAsync<T>(Func<Task<T>> layout)
    {
        try
        {
            return await layout().ConfigureAwait(false);
        }
        catch (PageCapacityException error)
        {
            throw new SheetRuleException(SheetRuleCode.CapacityExceeded, error.Message, error);
        }
    }

    /// <summary>What both operations compute before measuring anything.</summary>
    private sealed record Prepared(
        SheetRequest Request,
        Calibration Effective,
        IReadOnlyList<SkippedBlueprint> Skipped,
        IReadOnlyList<MisalignedElection> Misaligned,
        bool MisalignmentKnown)
    {
        public SheetReport ReportOf(SheetLayout? layout) => new(
            Pages: layout is null ? [] : [.. layout.Pages.Select(Report)],
            Skipped: Skipped,
            MisalignedElections: Misaligned,
            MisalignmentKnown: MisalignmentKnown,
            WidthLimited: layout?.WidthLimitedItems ?? []);

        private ReportedPage Report(SheetPage page)
        {
            // The capacity of the page's grid, through the T1 engine unchanged:
            // the same unit and the same grid the layout itself used.
            var unit = UnfoldedUnit.Create(page.Size, Effective.Sizes[page.Size], Request.Geometry, Effective.Geometry);
            int capacity = PageGrid.Create(Request.PaperFormat, unit, Effective.Layout).Capacity;

            return new ReportedPage(page.PageNumber, page.Size, capacity, page.Units.Count);
        }
    }
}

/// <summary>The reasons a sheet is refused, as codes (§G.3).</summary>
public enum SheetRuleCode
{
    /// <summary>The project's paper format is not declared by the calibration.</summary>
    PaperFormatUnknown,

    /// <summary>A size does not fit on the page at all (§5.4, §5.7).</summary>
    CapacityExceeded,

    /// <summary>Nothing to print: no blueprint has an elected, cut-out candidate.</summary>
    Empty,
}

/// <summary>Turns a code into the string an API would return.</summary>
public static class SheetRuleCodeExtensions
{
    /// <summary>The wire form of <paramref name="code"/>.</summary>
    public static string ToWireCode(this SheetRuleCode code) => code switch
    {
        SheetRuleCode.PaperFormatUnknown => "PAPER_FORMAT_UNKNOWN",
        SheetRuleCode.CapacityExceeded => "SHEET_CAPACITY_EXCEEDED",
        SheetRuleCode.Empty => "SHEET_EMPTY",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "No wire code for this value."),
    };
}

/// <summary>A sheet that cannot be produced, with its code and message.</summary>
public sealed class SheetRuleException : Exception, ICodedException
{
    public SheetRuleException(SheetRuleCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public SheetRuleException(SheetRuleCode code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    /// <summary>Which rule refused.</summary>
    public SheetRuleCode Code { get; }

    /// <summary>The code an API returns.</summary>
    public string WireCode => Code.ToWireCode();
}
