using Pawnsmith.Application;

namespace Pawnsmith.Infrastructure.Prompts;

/// <summary>
/// The reasons a catalogue or template file can be refused, as codes rather
/// than messages (§D.10).
/// </summary>
/// <remarks>
/// Same pattern as <see cref="Projects.ProjectErrorCode"/>, for the same
/// reason: chapter 10 makes the API return codes, and giving these errors a
/// code where they are born is what stops T6 inventing one by reading text.
/// The wire strings are written out rather than derived from member names, so
/// renaming a member cannot quietly change an API's answer.
/// </remarks>
public enum PromptFileErrorCode
{
    /// <summary>Catalogue file malformed, field missing, key or value repeated, fragment empty.</summary>
    CatalogInvalid,

    /// <summary>Catalogue <c>versionSchema</c> newer than this build understands.</summary>
    CatalogSchemaTooRecent,

    /// <summary>Template file malformed, field missing, required token absent, key repeated.</summary>
    TemplateInvalid,

    /// <summary>A token that is not in the closed list, named in the message.</summary>
    TemplateUnknownToken,

    /// <summary>Template <c>versionSchema</c> newer than this build understands.</summary>
    TemplateSchemaTooRecent,

    /// <summary>The file's <c>universe</c> is not the one the caller asked for.</summary>
    UniverseMismatch,

    /// <summary>The style library is malformed or incoherent (DEC-110).</summary>
    StylesInvalid,

    /// <summary>The style library declares a schema newer than this reader.</summary>
    StylesSchemaTooRecent,
}

/// <summary>Turns a code into the string an API would return.</summary>
public static class PromptFileErrorCodeExtensions
{
    /// <summary>The wire form of <paramref name="code"/>, as tabulated in D.10.</summary>
    public static string ToWireCode(this PromptFileErrorCode code) => code switch
    {
        PromptFileErrorCode.CatalogInvalid => "CATALOG_INVALID",
        PromptFileErrorCode.CatalogSchemaTooRecent => "CATALOG_SCHEMA_TOO_RECENT",
        PromptFileErrorCode.TemplateInvalid => "TEMPLATE_INVALID",
        PromptFileErrorCode.TemplateUnknownToken => "TEMPLATE_UNKNOWN_TOKEN",
        PromptFileErrorCode.TemplateSchemaTooRecent => "TEMPLATE_SCHEMA_TOO_RECENT",
        PromptFileErrorCode.UniverseMismatch => "UNIVERSE_MISMATCH",
        PromptFileErrorCode.StylesInvalid => "STYLES_INVALID",
        PromptFileErrorCode.StylesSchemaTooRecent => "STYLES_SCHEMA_TOO_RECENT",

        // Same reasoning as ProjectErrorCode: the compiler insists on this arm,
        // and a test walks Enum.GetValues to keep every named member covered.
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "No wire code for this value."),
    };
}

/// <summary>A catalogue or template file that cannot be used, with its code and message.</summary>
public sealed class PromptFileException : Exception, ICodedException
{
    public PromptFileException(PromptFileErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public PromptFileException(PromptFileErrorCode code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    /// <summary>Why the file was refused, as a code rather than as text.</summary>
    public PromptFileErrorCode Code { get; }

    /// <summary>The code in the form D.10 tabulates.</summary>
    public string WireCode => Code.ToWireCode();
}
