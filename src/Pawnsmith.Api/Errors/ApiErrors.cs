using System.Text.Json;

using Pawnsmith.Application;
using Pawnsmith.Infrastructure;

namespace Pawnsmith.Api.Errors;

/// <summary>A refusal born in the API itself, with its code (§G.3).</summary>
/// <remarks>
/// The other layers carry their own coded exceptions; this one is for what
/// only the API knows — a route that does not exist, a job identifier nobody
/// gave out, a body too large.
/// </remarks>
public sealed class ApiException(string code) : Exception(code), ICodedException
{
    public string WireCode { get; } = code;
}

/// <summary>The codes the API raises itself (§G.3, DEC-084).</summary>
public static class ApiCodes
{
    public const string RequestInvalid = "REQUEST_INVALID";
    public const string CrossOriginRefused = "CROSS_ORIGIN_REFUSED";
    public const string RouteNotFound = "ROUTE_NOT_FOUND";
    public const string ProjectNotFound = "PROJECT_NOT_FOUND";
    public const string JobNotFound = "JOB_NOT_FOUND";
    public const string JobAlreadyFinished = "JOB_ALREADY_FINISHED";
    public const string ImageNotFound = "IMAGE_NOT_FOUND";
    public const string UniverseNotFound = "UNIVERSE_NOT_FOUND";
    public const string UploadTooLarge = "UPLOAD_TOO_LARGE";
    public const string GeneratorNotConfigured = "GENERATOR_NOT_CONFIGURED";
    public const string SheetInputInvalid = "SHEET_INPUT_INVALID";
    public const string InternalError = "INTERNAL_ERROR";
}

/// <summary>The HTTP status of each code — the one table of §G.3.2.</summary>
/// <remarks>
/// Written out once, as a switch on the code string. A code missing from it
/// falls to 500, and a test walks every known code so that this cannot happen
/// without a red test.
/// </remarks>
public static class ErrorStatus
{
    public static int For(string code) => code switch
    {
        "REQUEST_INVALID" or "BATCH_SIZE_INVALID" => StatusCodes.Status400BadRequest,

        "CROSS_ORIGIN_REFUSED" => StatusCodes.Status403Forbidden,

        "ROUTE_NOT_FOUND" or "PROJECT_NOT_FOUND" or "BLUEPRINT_NOT_FOUND" or "CANDIDATE_NOT_FOUND"
            or "JOB_NOT_FOUND" or "IMAGE_NOT_FOUND" or "UNIVERSE_NOT_FOUND" => StatusCodes.Status404NotFound,

        "IMPORT_DESTINATION_EXISTS" or "JOB_ALREADY_FINISHED" => StatusCodes.Status409Conflict,

        "UPLOAD_TOO_LARGE" => StatusCodes.Status413PayloadTooLarge,

        "PROJECT_INVALID" or "PROJECT_PATH_ESCAPE" or "PROJECT_OVERRIDE_INVALID" or "PROJECT_SCHEMA_TOO_RECENT"
            or "PROJECT_TOO_LARGE" or "ARCHIVE_REJECTED" or "ARCHIVE_LIMIT_EXCEEDED" or "ARCHIVE_EXPORT_FAILED"
            or "CANDIDATE_NOT_CUT_OUT" or "PAPER_FORMAT_UNKNOWN" or "SHEET_CAPACITY_EXCEEDED" or "SHEET_EMPTY"
            or "SHEET_INPUT_INVALID" => StatusCodes.Status422UnprocessableEntity,

        "GENERATOR_NOT_CONFIGURED" or "WORKFLOW_INVALID" or "WORKFLOW_UNKNOWN_TOKEN" or "WORKFLOW_SCHEMA_TOO_RECENT"
            or "GENERATOR_URL_INVALID" => StatusCodes.Status503ServiceUnavailable,

        _ => StatusCodes.Status500InternalServerError,
    };
}

/// <summary>
/// Turns every exception that leaves an endpoint into a status and
/// <c>{ "code": … }</c>, and nothing else (DEC-084).
/// </summary>
/// <remarks>
/// <para>
/// <b>The message is never sent.</b> The interface translates codes, not
/// sentences (chapter 10); and the messages of this code base name absolute
/// paths, which would tell anyone reading a response the layout of the server
/// disk — the very leak chapter 8 keeps the logs away from archives for. The
/// messages are for the logs of T7.
/// </para>
/// <para>
/// Three exceptions have no code of their own and get one here, each for a
/// stated reason: an unreadable request body (<c>REQUEST_INVALID</c>); the
/// exception T1's image reader throws for an elected image missing or
/// unreadable on disk — the only path by which it reaches a request
/// (<c>SHEET_INPUT_INVALID</c>); and anything else (<c>INTERNAL_ERROR</c>).
/// </para>
/// </remarks>
public sealed class ErrorMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // The client went away; there is nobody to answer.
        }
        catch (Exception error) when (!context.Response.HasStarted)
        {
            string code = CodeOf(error);

            context.Response.Clear();
            context.Response.StatusCode = ErrorStatus.For(code);
            await context.Response.WriteAsJsonAsync(new ErrorBody(code));
        }
    }

    /// <summary>The wire code of an exception.</summary>
    public static string CodeOf(Exception error) => error switch
    {
        ICodedException coded => coded.WireCode,
        BadHttpRequestException bad when bad.StatusCode == StatusCodes.Status413PayloadTooLarge => ApiCodes.UploadTooLarge,
        BadHttpRequestException => ApiCodes.RequestInvalid,
        JsonException => ApiCodes.RequestInvalid,
        ManifestException => ApiCodes.SheetInputInvalid,
        _ => ApiCodes.InternalError,
    };

    /// <summary>The whole body of an error: one key.</summary>
    public sealed record ErrorBody(string Code);
}
