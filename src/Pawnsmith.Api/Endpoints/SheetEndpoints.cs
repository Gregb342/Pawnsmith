using System.Globalization;

using Pawnsmith.Api.Contracts;
using Pawnsmith.Api.Errors;
using Pawnsmith.Api.Hosting;
using Pawnsmith.Application.Sheets;
using Pawnsmith.Domain.PhysicalValues;

namespace Pawnsmith.Api.Endpoints;

/// <summary>The sheet of a project: its report, its capacities and its PDF (§G.8, DEC-117).</summary>
public static class SheetEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/projects/{folder}/sheet/report", async (
            string folder,
            PawnsmithSettings settings,
            ProjectSheet sheet,
            Calibration calibration,
            GeneratorHolder generator,
            CancellationToken cancellationToken) =>
        {
            string directory = ProjectAccess.Directory(settings, folder);
            SheetReport report = await sheet.ReportAsync(directory, calibration, generator.Current.FramingClause, cancellationToken);

            return report.ToDto();
        });

        // DEC-117: one line per size, zero included, so that the interface
        // can name the size that does not fit (§15.4) without the report
        // having to say more than its code (DEC-084).
        routes.MapGet("/api/projects/{folder}/sheet/capacities", async (
            string folder,
            PawnsmithSettings settings,
            ProjectSheet sheet,
            Calibration calibration,
            CancellationToken cancellationToken) =>
        {
            string directory = ProjectAccess.Directory(settings, folder);
            IReadOnlyList<SizeCapacity> capacities = await sheet.CapacitiesAsync(directory, calibration, cancellationToken);

            return capacities.Select(capacity => capacity.ToDto()).ToList();
        });

        routes.MapGet("/api/projects/{folder}/sheet.pdf", async (
            HttpContext context,
            string folder,
            string? culture,
            string? disposition,
            PawnsmithSettings settings,
            ProjectSheet sheet,
            Calibration calibration,
            GeneratorHolder generator,
            CancellationToken cancellationToken) =>
        {
            string directory = ProjectAccess.Directory(settings, folder);

            // The culture of a sheet is the user's choice at print time
            // (§15.1), never their browser's Accept-Language (§G.10).
            if (culture is null || !ReferenceMapping.Cultures.Contains(culture, StringComparer.Ordinal))
            {
                throw new ApiException(ApiCodes.RequestInvalid);
            }

            // DEC-113: the preview of the Layout step is the PDF itself, shown
            // in the page. "inline" lets the browser display it rather than
            // download it; nothing else changes.
            if (disposition is not null and not "inline")
            {
                throw new ApiException(ApiCodes.RequestInvalid);
            }

            // DEC-082: a misaligned elected candidate is printed; the report
            // route says which, before the user prints.
            ProjectSheetPdf pdf = await sheet.RenderAsync(
                directory, calibration, generator.Current.FramingClause, CultureInfo.GetCultureInfo(culture), cancellationToken);

            if (disposition is "inline")
            {
                context.Response.Headers.ContentDisposition = $"inline; filename=\"{folder}.pdf\"";
                return Results.File(pdf.Pdf, "application/pdf");
            }

            return Results.File(pdf.Pdf, "application/pdf", $"{folder}.pdf");
        });
    }
}
