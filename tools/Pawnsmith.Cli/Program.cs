using System.Globalization;

using Pawnsmith.Application.Sheets;
using Pawnsmith.Domain.PhysicalValues;
using Pawnsmith.Domain.Primitives;
using Pawnsmith.Domain.Projects;
using Pawnsmith.Domain.Sheets;
using Pawnsmith.Infrastructure;
using Pawnsmith.Infrastructure.Imaging;
using Pawnsmith.Infrastructure.Json;
using Pawnsmith.Infrastructure.Pdf;
using Pawnsmith.Infrastructure.Projects;

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

const string Usage = """
    pawnsmith-cli <command> [options]

      sheet            --manifest <path> --calibration <path> --out <path> [--debug]
      project new      --root <path> --name <text> --geometry <name>
                       --paper-format <name> --calibration <path>
      project check    --path <dir> --calibration <path>
      project export   --path <dir> --profile Backup|Share --out <dir> --calibration <path>
      project import   --archive <file> --root <path> --name <text> --calibration <path>

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
        _ => throw new ArgumentException("Expected one of: sheet, project new, project check, project export, project import."),
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

// ---- ce que tout le monde partage -----------------------------------------

async Task<Calibration> ReadCalibrationAsync(Arguments arguments) =>
    await CalibrationReader
        .ReadAsync(arguments.Required("--calibration"), CancellationToken.None)
        .ConfigureAwait(false);

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
internal sealed record Arguments(IReadOnlyDictionary<string, string> Values)
{
    /// <summary>Options that take no value.</summary>
    private static readonly HashSet<string> Flags = new(StringComparer.Ordinal) { "--debug" };

    public static Arguments Parse(string[] args)
    {
        Dictionary<string, string> values = new(StringComparer.Ordinal);

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
                values[name] = "true";
                index += 1;
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Option '{name}' has no value.");
            }

            values[name] = args[index + 1];
            index += 2;
        }

        return new Arguments(values);
    }

    public string Required(string option) =>
        Values.TryGetValue(option, out string? value)
            ? value
            : throw new ArgumentException($"Missing required option '{option}'.");

    public bool Has(string option) => Values.ContainsKey(option);
}
