namespace Pawnsmith.Infrastructure.Tests.Fixtures;

/// <summary>
/// A clock that always reads the same instant.
/// </summary>
/// <remarks>
/// <para>
/// <c>TimeProvider</c> is the abstraction .NET ships for "where does now come
/// from", and it is abstract precisely so that a test can pin the answer.
/// Pinning it is what lets a save be asserted exactly, instead of checking that
/// a timestamp is "roughly now" — the kind of assertion that goes red once a
/// month on a slow machine and teaches everyone to rerun the suite.
/// </para>
/// <para>
/// Microsoft publishes <c>FakeTimeProvider</c> for this, in a package. Ten lines
/// are cheaper: A.2 says that in any doubt between a dependency and twenty lines
/// of code, write the twenty lines, and a dependency runs on the developer's
/// machine exactly as a production one runs on the server.
/// </para>
/// </remarks>
internal sealed class FixedClock(DateTimeOffset instant) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => instant;
}
