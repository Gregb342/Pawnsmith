using Pawnsmith.Application;

namespace Pawnsmith.Infrastructure.Generation;

/// <summary>
/// Why a workflow file chosen in the interface is refused (§I.16.2, DEC-120).
/// </summary>
/// <remarks>
/// Distinct from <see cref="GeneratorConfigErrorCode"/>: those are faults of
/// the deployment, answered with a 503; these are a file the user picked, a
/// request they can correct, answered with a 422 — the same split as
/// <c>GENERATOR_URL_INVALID</c> and <c>GENERATOR_ADDRESS_INVALID</c>.
/// </remarks>
public enum WorkflowImportErrorCode
{
    /// <summary>Not JSON, the ordinary export instead of "Export (API)", or a prepared file §E.5 refuses.</summary>
    Invalid,

    /// <summary>No <c>SaveImage</c> node: nothing says which image to bring back.</summary>
    NoOutput,

    /// <summary>No input <c>seed</c> or <c>noise_seed</c> holding a number.</summary>
    NoSeed,

    /// <summary>The positive conditioning leads to no text.</summary>
    NoPrompt,

    /// <summary>Two candidates for one place: two outputs, two seeds, an encoder with two texts.</summary>
    Ambiguous,
}

public static class WorkflowImportErrorCodeExtensions
{
    /// <summary>The wire form of <paramref name="code"/>.</summary>
    public static string ToWireCode(this WorkflowImportErrorCode code) => code switch
    {
        WorkflowImportErrorCode.Invalid => "WORKFLOW_IMPORT_INVALID",
        WorkflowImportErrorCode.NoOutput => "WORKFLOW_IMPORT_NO_OUTPUT",
        WorkflowImportErrorCode.NoSeed => "WORKFLOW_IMPORT_NO_SEED",
        WorkflowImportErrorCode.NoPrompt => "WORKFLOW_IMPORT_NO_PROMPT",
        WorkflowImportErrorCode.Ambiguous => "WORKFLOW_IMPORT_AMBIGUOUS",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "No wire code for this value."),
    };
}

/// <summary>A workflow file refused at import, with its code and a message for the log.</summary>
public sealed class WorkflowImportException : Exception, ICodedException
{
    public WorkflowImportException(WorkflowImportErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public WorkflowImportException(WorkflowImportErrorCode code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    /// <summary>Which rule refused.</summary>
    public WorkflowImportErrorCode Code { get; }

    /// <inheritdoc />
    public string WireCode => Code.ToWireCode();
}
