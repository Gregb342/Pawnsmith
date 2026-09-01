namespace Pawnsmith.Infrastructure;

/// <summary>
/// A manifest or calibration file that cannot be used, with a message saying
/// why in terms the person who wrote the file can act on.
/// </summary>
/// <remarks>
/// B.3 asks for an explicit error on every validation failure. "Explicit" here
/// means naming the file, the field and the offending value — a stack trace
/// about a null reference is not a validation message.
/// </remarks>
public sealed class ManifestException : Exception
{
    public ManifestException(string message)
        : base(message)
    {
    }

    public ManifestException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
