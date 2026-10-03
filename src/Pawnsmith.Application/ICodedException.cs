namespace Pawnsmith.Application;

/// <summary>
/// An exception that carries the wire code an API returns for it (chapter 10).
/// </summary>
/// <remarks>
/// <para>
/// <b>One property, and it exists for a reason that arrived with T4.</b> A
/// generation batch runs in the background from T6 on, and when something
/// stops it, the job has to end <c>Failed</c> <i>with the code</i> of what
/// stopped it — <c>PROJECT_INVALID</c> if the project was broken by a hand edit
/// mid-batch, for instance (§E.11). That exception is born in Infrastructure,
/// which Application must not reference (A.3). This interface is how
/// Application reads the code without knowing the type.
/// </para>
/// <para>
/// Every coded exception of the repository implements it. The API of T6 uses
/// it for the same reason, to turn any refusal into its code without a list of
/// exception types to keep in step.
/// </para>
/// </remarks>
public interface ICodedException
{
    /// <summary>The code, as an API returns it — <c>PROJECT_INVALID</c>, <c>GENERATOR_UNREACHABLE</c>…</summary>
    string WireCode { get; }
}
