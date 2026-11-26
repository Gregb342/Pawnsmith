namespace Pawnsmith.Application.Projects;

/// <summary>Why a change to a project's settings was refused (§I.8, DEC-112).</summary>
public enum ProjectRuleCode
{
    /// <summary>The project has proposals, and its universe no longer changes.</summary>
    UniverseFrozen,

    /// <summary>The project has proposals, and its style no longer changes.</summary>
    StyleFrozen,
}

public static class ProjectRuleCodeExtensions
{
    public static string ToWireCode(this ProjectRuleCode code) => code switch
    {
        ProjectRuleCode.UniverseFrozen => "UNIVERSE_FROZEN",
        ProjectRuleCode.StyleFrozen => "STYLE_FROZEN",
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "No wire code for this value."),
    };
}

public sealed class ProjectRuleException : Exception, ICodedException
{
    public ProjectRuleException(ProjectRuleCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public ProjectRuleCode Code { get; }

    public string WireCode => Code.ToWireCode();
}
