using System.Globalization;

namespace Pawnsmith.Infrastructure.Tests.Fixtures;

/// <summary>
/// Forces the culture of the current thread for the length of a test, and puts
/// it back afterwards.
/// </summary>
/// <remarks>
/// C.3.3 asks for numbers written under the invariant culture, because a
/// <c>fr-FR</c> session writing <c>12,0</c> produces a project that is
/// unreadable anywhere else. That defect is invisible while developing in
/// English, so the tests have to go and look for it.
/// <para>
/// Restoring in <c>Dispose</c> rather than leaving the culture set matters:
/// xUnit reuses threads across tests, so a leaked culture would make an
/// unrelated test fail later, in a different file, for no visible reason.
/// </para>
/// </remarks>
internal sealed class CultureScope : IDisposable
{
    private readonly CultureInfo previousCulture;
    private readonly CultureInfo previousUiCulture;

    public CultureScope(string name)
    {
        previousCulture = CultureInfo.CurrentCulture;
        previousUiCulture = CultureInfo.CurrentUICulture;

        var culture = CultureInfo.GetCultureInfo(name);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = previousCulture;
        CultureInfo.CurrentUICulture = previousUiCulture;
    }
}
