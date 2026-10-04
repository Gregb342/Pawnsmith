using System.Globalization;

using Pawnsmith.Application.Blueprints;
using Pawnsmith.Application.Generation;
using Pawnsmith.Application.PhysicalValues;
using Pawnsmith.Application.Ports;
using Pawnsmith.Application.Projects;
using Pawnsmith.Application.Prompts;
using Pawnsmith.Application.Sheets;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Jobs;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Prompts;
using Pawnsmith.Domain.Sheets;
using Pawnsmith.Infrastructure;
using Pawnsmith.Infrastructure.Cutout;
using Pawnsmith.Infrastructure.Generation;
using Pawnsmith.Infrastructure.Imaging;
using Pawnsmith.Infrastructure.Json;
using Pawnsmith.Infrastructure.Pdf;
using Pawnsmith.Infrastructure.Projects;
using Pawnsmith.Infrastructure.Prompts;

// B.7 and C.17 — Throwaway command-line harness, not shipped, excluded from the
// Docker image, and without tests. Its reason to exist is DEC-027: a slice with
// no observable output can only be reviewed through its tests, and fifty-four
// tests do not show what a Share archive opened in a file browser shows.
//
// There is no logic here, and none is to be added. It reads arguments, calls the
// repository, prints what came back. Any rule that appeared here would be a rule
// missing from Application.
//
// DEC-059 — the command of T1 became the `sheet` subcommand when the project
// subcommands arrived, because `pawnsmith-cli --manifest …` gave the sheet no
// name and left the four others nowhere to attach.
//
// T3 adds the `blueprint` subcommands and `project sheet`. They exist to show
// what thirty-two tests cannot: a composed clause on screen, a diagnostic beside
// it, and a sheet that names the blueprint it left out. Every one of them goes
// through BlueprintEditor, CandidateElection or BlueprintRemoval - never through
// SaveAsync with a hand-patched blueprint, which is how a harness "without
// logic" exercises a business rule without owning it (D.7.3).
//
// T4 adds `generator check` and `candidate generate` (E.18). The second is the
// only way to see, without writing a test, that a batch interrupted with Ctrl+C
// leaves the candidates it produced in the project.

const string Usage = """
    pawnsmith-cli <command> [options]

      sheet            --manifest <path> --calibration <path> --out <path> [--debug]
      project new      --root <path> --name <text> --geometry <name>
                       --paper-format <name> --calibration <path>
      project check    --path <dir> --calibration <path>
      project export   --path <dir> --profile Backup|Share --out <dir> --calibration <path>
      project import   --archive <file> --root <path> --name <text> --calibration <path>
      project sheet    --path <dir> --out <path> --calibration <path> [--culture <name>] [--debug]
      blueprint add    --path <dir> --race <text> --class <text> --size <name>
                       [--param key=value]... [--details <text>] [--quantity <n>]
                       --template <path> --catalog <path> --calibration <path>
      blueprint edit   --path <dir> --id <guid> (same options as add)
      blueprint clause --path <dir> --id <guid> --clause <text> --calibration <path>
      blueprint elect  --path <dir> --id <guid> --candidate <guid> --calibration <path>
      blueprint remove --path <dir> --id <guid> --calibration <path>
      generator check  --workflow <path> --generator-url <url>
      candidate generate --path <dir> --id <guid> (--count <n> | --seed <n>...)
                       --workflow <path> --generator-url <url> --calibration <path>
      candidate cutout --path <dir> --id <guid> --candidate <guid> --calibration <path>
      cutout           --pair <file> --out <dir>

    Options of the blueprint subcommands:
      --template       Sentence structure of the universe (config/prompt-template.*.json).
      --catalog        Vocabulary of the universe (config/catalog.*.json).
      --param          One optional parameter, as key=value. Repeat for several.
                       A value the catalogue does not know is inserted as written
                       and reported, never refused (DEC-056).
      --quantity       Copies on the sheet. Defaults to 1.

    Options of generator check and candidate generate:
      --workflow       The ComfyUI workflow template (config/workflow.comfyui.json).
                       The repository ships only an example, to replace by the
                       workflow exported from your own machine.
      --generator-url  Address of the ComfyUI server, e.g. http://127.0.0.1:8188.
      --count          Number of candidates, with seeds drawn at random.
      --seed           One candidate with this seed. Repeat for several.
                       Ctrl+C cancels the batch; what was produced stays.

    Options of cutout (T5):
      --pair           Any paired image - front left, back right - such as the
                       T0a images in refs/. No project needed: this is the tool
                       to judge and tune the cut-out on real images (§F.8).
      --out            Folder where front.png and back.png are written.

    project sheet options:
      --culture        Culture of the text printed on the sheet. Defaults to en.

    Options common to the project subcommands:
      --calibration    Physical values, as described in B.2. Required by all of
                       them, check included: the relational diagnostics and the
                       effective calibration both depend on it (DEC-053).
      --root           Folder holding every project.

    sheet options:
      --manifest       Input manifest, as described in B.3.
      --out            PDF file to write.
      --debug          Print "head" and "feet" inside each panel. Diagnostics
                       only: never on a sheet meant to be cut.
    """;

try
{
    return await RunAsync(args).ConfigureAwait(false);
}
catch (ManifestException error)
{
    // Validation failures are the expected kind of failure here, and their
    // message is written for whoever wrote the file. Printing a stack trace over
    // it would bury the one useful line.
    Console.Error.WriteLine($"Invalid input: {error.Message}");
    return 1;
}
catch (ProjectException error)
{
    // The code first, because chapter 10 makes it the contract and the message
    // only the courtesy. Seeing them side by side is also the point of the
    // harness: it is where an error code stops being a table in a document.
    Console.Error.WriteLine($"{error.WireCode}: {error.Message}");
    return 1;
}
catch (PromptFileException error)
{
    Console.Error.WriteLine($"{error.WireCode}: {error.Message}");
    return 1;
}
catch (BlueprintRuleException error)
{
    Console.Error.WriteLine($"{error.WireCode}: {error.Message}");
    return 1;
}
catch (CutoutException error)
{
    Console.Error.WriteLine($"{error.WireCode}: {error.Message}");
    return 1;
}
catch (GeneratorConfigException error)
{
    Console.Error.WriteLine($"{error.WireCode}: {error.Message}");
    return 1;
}
catch (GenerationRuleException error)
{
    Console.Error.WriteLine($"{error.WireCode}: {error.Message}");
    return 1;
}
catch (PageCapacityException error)
{
    Console.Error.WriteLine($"Page capacity: {error.Message}");
    return 1;
}
catch (ArgumentException error)
{
    Console.Error.WriteLine(error.Message);
    Console.Error.WriteLine();
    Console.Error.WriteLine(Usage);
    return 2;
}

async Task<int> RunAsync(string[] arguments)
{
    // The command words are consumed before the options are parsed, so that
    // `project new --name x` and `sheet --out y` reach the same parser with only
    // their own options left.
    return arguments switch
    {
        ["sheet", .. string[] rest] => await SheetAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        ["project", "new", .. string[] rest] => await NewAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        ["project", "check", .. string[] rest] => await CheckAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        ["project", "export", .. string[] rest] => await ExportAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        ["project", "import", .. string[] rest] => await ImportAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        ["project", "sheet", .. string[] rest] => await ProjectSheetAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        ["blueprint", "add", .. string[] rest] => await BlueprintAddAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        ["blueprint", "edit", .. string[] rest] => await BlueprintEditAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        ["blueprint", "clause", .. string[] rest] => await BlueprintClauseAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        ["blueprint", "elect", .. string[] rest] => await BlueprintElectAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        ["blueprint", "remove", .. string[] rest] => await BlueprintRemoveAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        ["generator", "check", .. string[] rest] => await GeneratorCheckAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        ["candidate", "generate", .. string[] rest] => await CandidateGenerateAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        ["candidate", "cutout", .. string[] rest] => await CandidateCutoutAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        ["cutout", .. string[] rest] => await CutoutAsync(Arguments.Parse(rest)).ConfigureAwait(false),
        _ => throw new ArgumentException(
            "Expected one of: sheet, project new, project check, project export, project import, project sheet, " +
            "blueprint add, blueprint edit, blueprint clause, blueprint elect, blueprint remove, " +
            "generator check, candidate generate, candidate cutout, cutout."),
    };
}

// ---- sheet ----------------------------------------------------------------

async Task<int> SheetAsync(Arguments arguments)
{
    Calibration calibration = await ReadCalibrationAsync(arguments).ConfigureAwait(false);

    Manifest manifest = await ManifestReader
        .ReadAsync(arguments.Required("--manifest"), calibration, CancellationToken.None)
        .ConfigureAwait(false);

    RenderSheetUseCase useCase = new(
        new FileImageSizeReader(),
        new PdfSharpSheetRenderer(manifest.ImagesDirectory, arguments.Has("--debug")));

    RenderedSheet sheet = await useCase.ExecuteAsync(
        manifest.Request,
        calibration,
        manifest.ImagesDirectory,
        CultureInfo.GetCultureInfo(manifest.Culture),
        CancellationToken.None).ConfigureAwait(false);

    string output = arguments.Required("--out");
    await File.WriteAllBytesAsync(output, sheet.Pdf, CancellationToken.None).ConfigureAwait(false);

    Console.WriteLine($"Wrote {output} ({sheet.Pdf.Length} bytes).");

    // DEC-042 — a width-limited pawn prints shorter than its size demands and
    // nothing on the sheet reveals it. Report it rather than let it pass.
    foreach (WidthLimitedItem item in sheet.Layout.WidthLimitedItems)
    {
        Console.WriteLine(
            $"  note: '{item.ItemName}' ({item.Size}) prints at " +
            $"{item.HeightUsage:P0} of its height ({item.PrintedHeightMm:F1} of " +
            $"{item.AvailableHeightMm:F1} mm) — its width is the limiting factor.");
    }

    return 0;
}

// ---- project new ----------------------------------------------------------

async Task<int> NewAsync(Arguments arguments)
{
    // The calibration is read and then not used, which looks wasteful and is
    // not: it fails here, with a legible message, rather than at the first
    // attempt to open the project. Same reason C.17 asks every project
    // subcommand for one.
    await ReadCalibrationAsync(arguments).ConfigureAwait(false);

    ProjectCreator creator = new(new ProjectRepositoryOptions(arguments.Required("--root")));

    CreatedProject created = await creator.CreateAsync(
        arguments.Required("--name"),
        Universe.Fantasy,
        ParseEnum<Geometry>(arguments.Required("--geometry"), "--geometry"),
        arguments.Required("--paper-format"),
        CancellationToken.None).ConfigureAwait(false);

    Console.WriteLine(created.Directory);
    Console.WriteLine($"  projectId {created.Project.ProjectId}");

    return 0;
}

// ---- project check --------------------------------------------------------

async Task<int> CheckAsync(Arguments arguments)
{
    Calibration calibration = await ReadCalibrationAsync(arguments).ConfigureAwait(false);
    string directory = Path.GetFullPath(arguments.Required("--path"));

    // The root is the parent, because check is handed a project rather than told
    // where projects live. The reader still verifies the folder resolves inside
    // it, which is what stops a link pointing elsewhere.
    ProjectReader reader = new(new ProjectRepositoryOptions(
        Path.GetDirectoryName(directory) ?? directory));

    LoadedProject loaded = await reader
        .LoadAsync(directory, calibration, CancellationToken.None)
        .ConfigureAwait(false);

    Console.WriteLine($"{loaded.Project.Name} ({loaded.Project.ProjectId})");
    Console.WriteLine($"  {loaded.Project.Blueprints.Count} blueprint(s), {loaded.Project.Geometry}, {loaded.Project.PaperFormatName}");

    PrintDiagnostics(loaded.Diagnostics);

    return 0;
}

// ---- project export -------------------------------------------------------

async Task<int> ExportAsync(Arguments arguments)
{
    await ReadCalibrationAsync(arguments).ConfigureAwait(false);

    string archive = await new ProjectExporter().ExportAsync(
        arguments.Required("--path"),
        ParseEnum<ArchiveProfile>(arguments.Required("--profile"), "--profile"),
        arguments.Required("--out"),
        CancellationToken.None).ConfigureAwait(false);

    Console.WriteLine(archive);

    // Listing the entries is the whole point of the subcommand: MEN-006 is a
    // whitelist, and a whitelist is believed when it is seen.
    using System.IO.Compression.ZipArchive zip = System.IO.Compression.ZipFile.OpenRead(archive);

    foreach (System.IO.Compression.ZipArchiveEntry entry in zip.Entries)
    {
        Console.WriteLine($"  {entry.FullName} ({entry.Length} bytes)");
    }

    return 0;
}

// ---- project import -------------------------------------------------------

async Task<int> ImportAsync(Arguments arguments)
{
    Calibration calibration = await ReadCalibrationAsync(arguments).ConfigureAwait(false);

    ProjectImporter importer = new(new ProjectRepositoryOptions(arguments.Required("--root")));

    ImportedProject imported = await importer.ImportAsync(
        arguments.Required("--archive"),
        arguments.Required("--name"),
        calibration,
        CancellationToken.None).ConfigureAwait(false);

    Console.WriteLine(imported.Directory);
    Console.WriteLine($"  projectId {imported.Project.ProjectId}");

    PrintDiagnostics(imported.Diagnostics);

    return 0;
}

// ---- project sheet --------------------------------------------------------

async Task<int> ProjectSheetAsync(Arguments arguments)
{
    Calibration calibration = await ReadCalibrationAsync(arguments).ConfigureAwait(false);
    (string directory, LoadedProject loaded) = await LoadProjectAsync(arguments, calibration).ConfigureAwait(false);
    Project project = loaded.Project;

    // The effective calibration, never the file's: the tab overrides of the
    // project feed the cell height (DEC-040, DEC-053).
    Calibration effective = EffectiveCalibration.Resolve(calibration, project);

    if (!effective.PaperFormats.TryGetValue(project.PaperFormatName, out PaperFormat? paperFormat))
    {
        throw new ArgumentException(
            $"The project asks for the paper format '{project.PaperFormatName}', which the calibration does not declare. " +
            $"Known formats are: {string.Join(", ", effective.PaperFormats.Keys)}.");
    }

    ProjectSheetRequest built = ProjectSheetRequestBuilder.From(project, paperFormat);

    // DEC-069 - said before anything is rendered, so a sheet with a blueprint
    // missing is never a surprise.
    foreach (SkippedBlueprint skipped in built.Skipped)
    {
        Console.WriteLine($"  skipped [{skipped.Reason}]: {skipped.Message}");
    }

    if (built.Request.Items.Count == 0)
    {
        Console.WriteLine("Nothing to lay out: no blueprint has an elected, cut-out candidate.");
        return 0;
    }

    RenderSheetUseCase useCase = new(
        new FileImageSizeReader(),
        new PdfSharpSheetRenderer(directory, arguments.Has("--debug")));

    RenderedSheet sheet = await useCase.ExecuteAsync(
        built.Request,
        effective,
        directory,
        CultureInfo.GetCultureInfo(arguments.Optional("--culture") ?? "en"),
        CancellationToken.None).ConfigureAwait(false);

    string output = arguments.Required("--out");
    await File.WriteAllBytesAsync(output, sheet.Pdf, CancellationToken.None).ConfigureAwait(false);

    Console.WriteLine($"Wrote {output} ({sheet.Pdf.Length} bytes, {sheet.Layout.Pages.Count} page(s)).");

    foreach (WidthLimitedItem item in sheet.Layout.WidthLimitedItems)
    {
        Console.WriteLine(
            $"  note: '{item.ItemName}' ({item.Size}) prints at " +
            $"{item.HeightUsage:P0} of its height ({item.PrintedHeightMm:F1} of " +
            $"{item.AvailableHeightMm:F1} mm) - its width is the limiting factor.");
    }

    return 0;
}

// ---- blueprint add / edit / clause ----------------------------------------

async Task<int> BlueprintAddAsync(Arguments arguments)
{
    Calibration calibration = await ReadCalibrationAsync(arguments).ConfigureAwait(false);
    (string directory, LoadedProject loaded) = await LoadProjectAsync(arguments, calibration).ConfigureAwait(false);
    IPromptComposer composer = await ReadComposerAsync(arguments, loaded.Project.Universe).ConfigureAwait(false);

    EditedProject edited = BlueprintEditor.Add(loaded.Project, ReadFields(arguments), composer);

    await SaveAsync(directory, edited.Project).ConfigureAwait(false);
    PrintBlueprint(edited);

    return 0;
}

async Task<int> BlueprintEditAsync(Arguments arguments)
{
    Calibration calibration = await ReadCalibrationAsync(arguments).ConfigureAwait(false);
    (string directory, LoadedProject loaded) = await LoadProjectAsync(arguments, calibration).ConfigureAwait(false);
    IPromptComposer composer = await ReadComposerAsync(arguments, loaded.Project.Universe).ConfigureAwait(false);

    // DEC-067 happens inside UpdateFields, and only there: the clause follows
    // the fields if nobody edited it, and is left alone otherwise.
    EditedProject edited = BlueprintEditor.UpdateFields(
        loaded.Project, ReadGuid(arguments, "--id"), ReadFields(arguments), composer);

    await SaveAsync(directory, edited.Project).ConfigureAwait(false);
    PrintBlueprint(edited);

    return 0;
}

async Task<int> BlueprintClauseAsync(Arguments arguments)
{
    Calibration calibration = await ReadCalibrationAsync(arguments).ConfigureAwait(false);
    (string directory, LoadedProject loaded) = await LoadProjectAsync(arguments, calibration).ConfigureAwait(false);

    EditedProject edited = BlueprintEditor.EditSubjectClause(
        loaded.Project, ReadGuid(arguments, "--id"), arguments.Required("--clause"));

    await SaveAsync(directory, edited.Project).ConfigureAwait(false);
    PrintBlueprint(edited);

    return 0;
}

// ---- blueprint elect ------------------------------------------------------

async Task<int> BlueprintElectAsync(Arguments arguments)
{
    Calibration calibration = await ReadCalibrationAsync(arguments).ConfigureAwait(false);
    (string directory, LoadedProject loaded) = await LoadProjectAsync(arguments, calibration).ConfigureAwait(false);

    EditedProject edited = CandidateElection.Elect(
        loaded.Project, ReadGuid(arguments, "--id"), ReadGuid(arguments, "--candidate"));

    await SaveAsync(directory, edited.Project).ConfigureAwait(false);

    Console.WriteLine($"{edited.Blueprint.Race} {edited.Blueprint.CharacterClass} ({edited.Blueprint.Id})");
    Console.WriteLine($"  elected {edited.Blueprint.ElectedCandidateId}");

    // DEC-068 made visible: the statuses are printed so one can see none moved.
    foreach (Candidate candidate in edited.Blueprint.Candidates)
    {
        string mark = candidate.Id == edited.Blueprint.ElectedCandidateId ? "*" : " ";
        Console.WriteLine($"  {mark} {candidate.Id} {candidate.Status}");
    }

    return 0;
}

// ---- blueprint remove -----------------------------------------------------

async Task<int> BlueprintRemoveAsync(Arguments arguments)
{
    Calibration calibration = await ReadCalibrationAsync(arguments).ConfigureAwait(false);
    (string directory, LoadedProject loaded) = await LoadProjectAsync(arguments, calibration).ConfigureAwait(false);

    RemovedBlueprint removed = BlueprintRemoval.Remove(loaded.Project, ReadGuid(arguments, "--id"));

    // Model first, disk second (DEC-070): a save that fails leaves the files
    // where they are; a deletion that fails leaves orphans, which are harmless.
    await SaveAsync(directory, removed.Project).ConfigureAwait(false);
    int deleted = ProjectImageFiles.Delete(directory, removed.ReferencedFiles);

    Console.WriteLine($"Removed {removed.Removed.Race} {removed.Removed.CharacterClass} ({removed.Removed.Id}).");
    Console.WriteLine($"  {removed.Removed.Candidates.Count} candidate(s), {deleted} of {removed.ReferencedFiles.Count} referenced file(s) deleted.");

    return 0;
}

// ---- ce que les sous-commandes de gabarit partagent ----------------------

async Task<(string Directory, LoadedProject Loaded)> LoadProjectAsync(Arguments arguments, Calibration calibration)
{
    string directory = Path.GetFullPath(arguments.Required("--path"));

    ProjectReader reader = new(new ProjectRepositoryOptions(
        Path.GetDirectoryName(directory) ?? directory));

    LoadedProject loaded = await reader
        .LoadAsync(directory, calibration, CancellationToken.None)
        .ConfigureAwait(false);

    return (directory, loaded);
}

async Task SaveAsync(string directory, Project project) =>
    await new ProjectSaver().SaveAsync(directory, project, CancellationToken.None).ConfigureAwait(false);

async Task<IPromptComposer> ReadComposerAsync(Arguments arguments, Universe universe)
{
    PromptTemplate template = await PromptTemplateReader
        .ReadAsync(arguments.Required("--template"), universe, CancellationToken.None)
        .ConfigureAwait(false);

    Catalog catalog = await CatalogReader
        .ReadAsync(arguments.Required("--catalog"), universe, CancellationToken.None)
        .ConfigureAwait(false);

    return new TemplatePromptComposer(template, catalog);
}

BlueprintFields ReadFields(Arguments arguments)
{
    Dictionary<string, string> parameters = new(StringComparer.Ordinal);

    foreach (string pair in arguments.All("--param"))
    {
        int equals = pair.IndexOf('=', StringComparison.Ordinal);

        if (equals <= 0)
        {
            throw new ArgumentException($"'--param {pair}' is not of the form key=value.");
        }

        parameters[pair[..equals]] = pair[(equals + 1)..];
    }

    string quantity = arguments.Optional("--quantity") ?? "1";

    return new BlueprintFields(
        Race: arguments.Required("--race"),
        CharacterClass: arguments.Required("--class"),
        Size: ParseEnum<Size>(arguments.Required("--size"), "--size"),
        OptionalParameters: parameters,
        Details: arguments.Optional("--details") ?? string.Empty,
        Quantity: int.TryParse(quantity, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : throw new ArgumentException($"'--quantity {quantity}' is not an integer."));
}

Guid ReadGuid(Arguments arguments, string option)
{
    string value = arguments.Required(option);

    return Guid.TryParse(value, out Guid parsed)
        ? parsed
        : throw new ArgumentException($"'{option} {value}' is not an identifier.");
}

// The clause and its diagnostics side by side: the point of the harness for
// this slice. A value the catalogue did not know is in the clause as written,
// and named underneath - which is what DEC-056 looks like on screen.
void PrintBlueprint(EditedProject edited)
{
    Blueprint blueprint = edited.Blueprint;

    Console.WriteLine($"{blueprint.Race} {blueprint.CharacterClass} ({blueprint.Id})");
    Console.WriteLine($"  {blueprint.Size}, x{blueprint.Quantity}");
    Console.WriteLine($"  subject: {blueprint.SubjectClause}");

    if (edited.Diagnostics.Count == 0)
    {
        return;
    }

    Console.WriteLine($"  {edited.Diagnostics.Count} composition diagnostic(s) - the clause is usable all the same:");

    foreach (CompositionDiagnostic diagnostic in edited.Diagnostics)
    {
        Console.WriteLine($"    {diagnostic.Key}={diagnostic.Value}: {diagnostic.Message}");
    }
}

// ---- ce que tout le monde partage -----------------------------------------

async Task<Calibration> ReadCalibrationAsync(Arguments arguments) =>
    await CalibrationReader
        .ReadAsync(arguments.Required("--calibration"), CancellationToken.None)
        .ConfigureAwait(false);

// ---- generator, candidate ------------------------------------------------

async Task<int> GeneratorCheckAsync(Arguments arguments)
{
    using ComfyUiImageGenerator generator = await BuildGeneratorAsync(arguments).ConfigureAwait(false);

    Console.WriteLine("framing clause:");

    foreach (string line in generator.FramingClause.Split('\n'))
    {
        Console.WriteLine($"  {line}");
    }

    GeneratorAvailability availability = await generator.CheckAsync(CancellationToken.None).ConfigureAwait(false);
    Console.WriteLine($"generator: {availability}");

    // Unreachable is an ordinary state (E.7.1), so it is not a failure of the
    // command: the exit code says whether the check ran, not what it found.
    return 0;
}

async Task<int> CandidateGenerateAsync(Arguments arguments)
{
    Calibration calibration = await ReadCalibrationAsync(arguments).ConfigureAwait(false);
    string directory = Path.GetFullPath(arguments.Required("--path"));

    using ComfyUiImageGenerator generator = await BuildGeneratorAsync(arguments).ConfigureAwait(false);

    // The projects root only matters to create and import; here it is the
    // folder above the project, so that the reader's bounds apply as usual.
    var repository = new FileSystemProjectRepository(
        new ProjectRepositoryOptions(Path.GetDirectoryName(directory) ?? directory));

    var useCase = new CandidateGeneration(generator, new UniformBackgroundRemover(new CutoutOptions()), repository, new GenerationOptions());

    using var cancellation = new CancellationTokenSource();

    // Ctrl+C cancels the batch instead of killing the process, so that the
    // cancellation reaches ComfyUI and the job reports what it kept.
    Console.CancelKeyPress += (_, press) =>
    {
        press.Cancel = true;
        cancellation.Cancel();
    };

    Job job = await useCase.RunAsync(
        new GenerationBatch(directory, ReadGuid(arguments, "--id"), ReadSeeds(arguments), generator.FramingClause, calibration),
        PrintJob,
        cancellation.Token).ConfigureAwait(false);

    return job.State == JobState.Completed ? 0 : 1;
}

// ---- candidate cutout ------------------------------------------------------

async Task<int> CandidateCutoutAsync(Arguments arguments)
{
    Calibration calibration = await ReadCalibrationAsync(arguments).ConfigureAwait(false);
    string directory = Path.GetFullPath(arguments.Required("--path"));

    var repository = new FileSystemProjectRepository(
        new ProjectRepositoryOptions(Path.GetDirectoryName(directory) ?? directory));
    var useCase = new CandidateCutout(repository, new UniformBackgroundRemover(new CutoutOptions()), new ProjectWriteGate());

    Guid candidateId = ReadGuid(arguments, "--candidate");
    EditedProject edited = await useCase
        .CutOutAsync(directory, calibration, ReadGuid(arguments, "--id"), candidateId, CancellationToken.None)
        .ConfigureAwait(false);

    Candidate candidate = edited.Blueprint.Candidates.Single(each => each.Id == candidateId);
    Console.WriteLine($"{candidate.Id} cut out:");
    Console.WriteLine($"  front {candidate.FrontImageFile}");
    Console.WriteLine($"  back  {candidate.BackImageFile}");

    return 0;
}

// ---- cutout ------------------------------------------------------------------

// No project, no use case: the adapter alone, on any paired image. It exists to
// judge the cut-out on real images and tune CutoutOptions (§F.8).
async Task<int> CutoutAsync(Arguments arguments)
{
    string output = Path.GetFullPath(arguments.Required("--out"));
    byte[] paired = await File.ReadAllBytesAsync(arguments.Required("--pair")).ConfigureAwait(false);

    CutoutPair cutouts = await new UniformBackgroundRemover(new CutoutOptions())
        .CutOutPairAsync(paired, CancellationToken.None)
        .ConfigureAwait(false);

    Directory.CreateDirectory(output);
    await File.WriteAllBytesAsync(Path.Combine(output, "front.png"), cutouts.FrontPng).ConfigureAwait(false);
    await File.WriteAllBytesAsync(Path.Combine(output, "back.png"), cutouts.BackPng).ConfigureAwait(false);

    Console.WriteLine($"front.png and back.png written to {output}");
    return 0;
}

async Task<ComfyUiImageGenerator> BuildGeneratorAsync(Arguments arguments)
{
    WorkflowTemplate workflow = await WorkflowTemplateReader
        .ReadAsync(arguments.Required("--workflow"), CancellationToken.None)
        .ConfigureAwait(false);

    return new ComfyUiImageGenerator(new ComfyUiOptions(arguments.Required("--generator-url")), workflow);
}

IReadOnlyList<ulong> ReadSeeds(Arguments arguments)
{
    IReadOnlyList<string> seeds = arguments.All("--seed");
    string? count = arguments.Optional("--count");

    if (seeds.Count > 0 == (count is not null))
    {
        throw new ArgumentException("Give either --count or one or more --seed, not both and not neither.");
    }

    if (count is not null)
    {
        return int.TryParse(count, NumberStyles.None, CultureInfo.InvariantCulture, out int n)
            ? RandomSeeds.Draw(n)
            : throw new ArgumentException($"'--count {count}' is not a whole number.");
    }

    return [.. seeds.Select(seed => ulong.TryParse(seed, NumberStyles.None, CultureInfo.InvariantCulture, out ulong parsed)
        ? parsed
        : throw new ArgumentException($"'--seed {seed}' is not a seed."))];
}

// Every state of the job, as it happens: the harness's whole reason to exist
// for T4. A cancelled or failed batch prints what it kept.
void PrintJob(Job job)
{
    string line = job.State switch
    {
        JobState.Running when job.Produced.Count > 0 =>
            $"  candidate {job.Produced.Count}/{job.Requested}: {job.Produced[^1]}" +
            (job.CutoutFailures.FirstOrDefault(failure => failure.CandidateId == job.Produced[^1]) is CutoutFailure failure
                ? $" - not cut out: {failure.Code} - {failure.Message}"
                : " - cut out"),
        JobState.Failed => $"{job.State}: {job.Failure!.Code} - {job.Failure.Message}",
        _ => $"{job.State} ({job.Produced.Count}/{job.Requested} produced)",
    };

    Console.WriteLine(line);
}

// Printed apart from the errors, and never mixed with them, because that
// separation *is* DEC-056: an error is returned instead of a project, a
// diagnostic alongside one. Showing them in one list would undo the distinction
// the whole slice was reorganised around.
void PrintDiagnostics(IReadOnlyList<ProjectDiagnostic> diagnostics)
{
    if (diagnostics.Count == 0)
    {
        Console.WriteLine("  no diagnostic.");
        return;
    }

    Console.WriteLine($"  {diagnostics.Count} diagnostic(s) — the project is usable all the same:");

    foreach (ProjectDiagnostic diagnostic in diagnostics)
    {
        Console.WriteLine($"    [{diagnostic.Kind}] {diagnostic.Field}: {diagnostic.Message}");
    }
}

TEnum ParseEnum<TEnum>(string value, string option)
    where TEnum : struct, Enum
{
    return Enum.TryParse(value, ignoreCase: false, out TEnum parsed) && Enum.IsDefined(parsed)
        ? parsed
        : throw new ArgumentException(
            $"'{value}' is not a value of '{option}'. Expected one of: " +
            string.Join(", ", Enum.GetNames<TEnum>()) + ".");
}

/// <summary>The options of one invocation, as they were typed.</summary>
/// <remarks>
/// A dictionary rather than a record per subcommand. Five subcommands share nine
/// option names between them, and five parsers would be five places to forget a
/// required option. This one has a single rule — a name, then its value, unless
/// the name is a known flag — and every subcommand asks for what it needs.
/// </remarks>
internal sealed record Arguments(IReadOnlyDictionary<string, List<string>> Values)
{
    /// <summary>Options that take no value.</summary>
    private static readonly HashSet<string> Flags = new(StringComparer.Ordinal) { "--debug" };

    public static Arguments Parse(string[] args)
    {
        // Every value is kept, in order, because --param repeats. For the
        // others, Required and Optional read the last one given.
        Dictionary<string, List<string>> values = new(StringComparer.Ordinal);

        int index = 0;

        while (index < args.Length)
        {
            string name = args[index];

            if (!name.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Expected an option, found '{name}'.");
            }

            if (Flags.Contains(name))
            {
                Add(values, name, "true");
                index += 1;
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Option '{name}' has no value.");
            }

            Add(values, name, args[index + 1]);
            index += 2;
        }

        return new Arguments(values);
    }

    private static void Add(Dictionary<string, List<string>> values, string name, string value)
    {
        if (!values.TryGetValue(name, out List<string>? list))
        {
            list = [];
            values[name] = list;
        }

        list.Add(value);
    }

    public string Required(string option) =>
        Optional(option) ?? throw new ArgumentException($"Missing required option '{option}'.");

    public string? Optional(string option) =>
        Values.TryGetValue(option, out List<string>? list) ? list[^1] : null;

    public IReadOnlyList<string> All(string option) =>
        Values.TryGetValue(option, out List<string>? list) ? list : [];

    public bool Has(string option) => Values.ContainsKey(option);
}
