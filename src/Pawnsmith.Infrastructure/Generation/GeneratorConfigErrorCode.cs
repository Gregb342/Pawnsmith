namespace Pawnsmith.Infrastructure.Generation;

/// <summary>
/// The reasons the generator's configuration — the workflow template and the
/// generator address — can be refused, as codes rather than messages (§E.11).
/// </summary>
/// <remarks>
/// Same pattern as <see cref="Projects.ProjectErrorCode"/> and
/// <see cref="Prompts.PromptFileErrorCode"/>, for the same reason: chapter 10
/// makes the API return codes, and giving these errors their code where they
/// are born is what stops T6 inventing one by reading text. The wire strings
/// are written out, so renaming a member cannot quietly change an API's answer.
/// <para>
/// These are refused <b>at start-up</b>, when the file or the address is read,
/// never at the first batch. A configuration that cannot work should say so
/// before anyone has waited for a generation.
/// </para>
/// </remarks>
public enum GeneratorConfigErrorCode
{
    /// <summary>Workflow file malformed, field missing, token missing or repeated, token inside a longer text, output node absent, interface format.</summary>
    WorkflowInvalid,

    /// <summary>A <c>{{…}}</c> outside the closed list, named in the message.</summary>
    WorkflowUnknownToken,

    /// <summary>Workflow <c>versionSchema</c> newer than this build understands.</summary>
    WorkflowSchemaTooRecent,

    /// <summary>Generator address refused by §E.10.1.</summary>
    GeneratorUrlInvalid,
}

/// <summary>Turns a code into the string an API would return.</summary>
public static class GeneratorConfigErrorCodeExtensions
{
    /// <summary>The wire form of <paramref name="code"/>, as tabulated in E.11.</summary>
    public static string ToWireCode(this GeneratorConfigErrorCode code) => code switch
    {
        GeneratorConfigErrorCode.WorkflowInvalid => "WORKFLOW_INVALID",
        GeneratorConfigErrorCode.WorkflowUnknownToken => "WORKFLOW_UNKNOWN_TOKEN",
        GeneratorConfigErrorCode.WorkflowSchemaTooRecent => "WORKFLOW_SCHEMA_TOO_RECENT",
        GeneratorConfigErrorCode.GeneratorUrlInvalid => "GENERATOR_URL_INVALID",

        // Same reasoning as ProjectErrorCode: the compiler insists on this arm,
        // and a test walks Enum.GetValues to keep every named member covered.
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "No wire code for this value."),
    };
}

/// <summary>A generator configuration that cannot be used, with its code and message.</summary>
public sealed class GeneratorConfigException : Exception
{
    public GeneratorConfigException(GeneratorConfigErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public GeneratorConfigException(GeneratorConfigErrorCode code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    /// <summary>Why the configuration was refused, as a code rather than as text.</summary>
    public GeneratorConfigErrorCode Code { get; }

    /// <summary>The code in the form E.11 tabulates.</summary>
    public string WireCode => Code.ToWireCode();
}
