namespace Pawnsmith.Infrastructure.Projects;

/// <summary>
/// A project operation that refuses, carrying both a code and a message.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="Code"/> is what an API will return (chapter 10); the message
/// is what a person reads in a log or on a command line. Keeping both on one
/// exception is what stops T6 having to recover a code by matching text.
/// </para>
/// <para>
/// It is deliberately <b>not</b> <see cref="ManifestException"/>. That one means
/// "this input file cannot be used" and carries no code, which was enough for
/// the manifest and the calibration because T1 had no API in view. Reusing it
/// here would either leave the project errors codeless or force a code onto the
/// T1 readers that nothing asks for.
/// </para>
/// </remarks>
public sealed class ProjectException : Exception
{
    public ProjectException(ProjectErrorCode code, string message)
        : base(message)
    {
        Code = code;
    }

    public ProjectException(ProjectErrorCode code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    /// <summary>Why the operation refused, as a code rather than as text.</summary>
    public ProjectErrorCode Code { get; }

    /// <summary>The code in the form C.11 tabulates.</summary>
    public string WireCode => Code.ToWireCode();
}
