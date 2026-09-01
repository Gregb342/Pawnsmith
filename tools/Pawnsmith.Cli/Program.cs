using System.Globalization;

using Pawnsmith.Application;
using Pawnsmith.Domain;
using Pawnsmith.Infrastructure;

// B.7 — Throwaway command-line harness, not shipped and excluded from the
// Docker image. Its only reason to exist is to allow a paper print before the
// UI exists — and to produce the test sheets of T0b (DEC-033).
//
// It reads, calls the use case, writes the file, and prints validation errors
// legibly. There is no logic here, and none is to be added: varying a flap
// height or a silhouette margin for T0b is done with several calibration files
// passed to --calibration, not with new options.

const string Usage = """
    pawnsmith-cli --manifest <path> --calibration <path> --out <path>

      --manifest     Input manifest, as described in B.3.
      --calibration  Physical values, as described in B.2.
      --out          PDF file to write.
      --debug        Print "head" and "feet" inside each panel. Diagnostics
                     only: never on a sheet meant to be cut.
    """;

try
{
    var options = Options.Parse(args);

    Calibration calibration = await CalibrationReader
        .ReadAsync(options.CalibrationPath, CancellationToken.None)
        .ConfigureAwait(false);

    Manifest manifest = await ManifestReader
        .ReadAsync(options.ManifestPath, calibration, CancellationToken.None)
        .ConfigureAwait(false);

    RenderSheetUseCase useCase = new(
        new FileImageSizeReader(),
        new PdfSharpSheetRenderer(manifest.ImagesDirectory, options.AnnotateOrientation));

    byte[] pdf = await useCase.ExecuteAsync(
        manifest.Request,
        calibration,
        manifest.ImagesDirectory,
        CultureInfo.GetCultureInfo(manifest.Culture),
        CancellationToken.None).ConfigureAwait(false);

    await File.WriteAllBytesAsync(options.OutputPath, pdf, CancellationToken.None)
        .ConfigureAwait(false);

    Console.WriteLine($"Wrote {options.OutputPath} ({pdf.Length} bytes).");

    return 0;
}
catch (ManifestException error)
{
    // Validation failures are the expected kind of failure here, and their
    // message is written for whoever wrote the file. Printing a stack trace
    // over it would bury the one useful line.
    Console.Error.WriteLine($"Invalid input: {error.Message}");
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

/// <summary>The three paths the harness needs.</summary>
internal sealed record Options(
    string ManifestPath,
    string CalibrationPath,
    string OutputPath,
    bool AnnotateOrientation)
{
    public static Options Parse(string[] args)
    {
        string? manifest = null;
        string? calibration = null;
        string? output = null;
        bool debug = false;

        int index = 0;

        while (index < args.Length)
        {
            // --debug is the only flag without a value, so it advances by one
            // where every other option advances by two.
            if (args[index] == "--debug")
            {
                debug = true;
                index += 1;
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new ArgumentException($"Option '{args[index]}' has no value.");
            }

            string value = args[index + 1];

            switch (args[index])
            {
                case "--manifest":
                    manifest = value;
                    break;
                case "--calibration":
                    calibration = value;
                    break;
                case "--out":
                    output = value;
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{args[index]}'.");
            }

            index += 2;
        }

        return new Options(
            Required(manifest, "--manifest"),
            Required(calibration, "--calibration"),
            Required(output, "--out"),
            debug);
    }

    private static string Required(string? value, string option)
    {
        return value ?? throw new ArgumentException($"Missing required option '{option}'.");
    }
}
