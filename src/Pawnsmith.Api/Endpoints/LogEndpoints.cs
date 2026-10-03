using Pawnsmith.Api.Contracts;
using Pawnsmith.Api.Errors;
using Pawnsmith.Api.Hosting;
using Pawnsmith.Infrastructure.Logging;

namespace Pawnsmith.Api.Endpoints;

/// <summary>
/// The log viewer, server side (§H.6, DEC-094). The screen belongs to the front.
/// </summary>
/// <remarks>
/// It calls the infrastructure directly, with no port in the Application: the
/// viewer has no rule to carry — list a folder, read the end of a file — and a
/// port would be an abstraction written "just in case" (§H.6.4).
/// </remarks>
public static class LogEndpoints
{
    public static void Map(IEndpointRouteBuilder routes)
    {
        routes.MapGet("/api/logs", (PawnsmithSettings settings, LogDirectory logs) =>
            new LogListDto(settings.Logs.Enabled, [.. logs.List().Select(file => file.ToDto())]));

        routes.MapGet("/api/logs/{name}", async (string name, int? lines, LogDirectory logs, CancellationToken cancellationToken) =>
        {
            int count = lines ?? LogDirectory.DefaultLines;

            if (count < 1 || count > LogDirectory.MaxLines)
            {
                throw new ApiException(ApiCodes.RequestInvalid);
            }

            // Unknown, outside the pattern, a traversal, a link: all the same
            // answer. Telling "exists but refused" apart would describe the disk.
            LogTail tail = await logs.ReadTailAsync(name, count, cancellationToken)
                ?? throw new ApiException(ApiCodes.LogNotFound);

            return tail.ToDto();
        });
    }
}
