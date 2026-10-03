using System.Reflection;

namespace Pawnsmith.Api.Hosting;

/// <summary>
/// What the host writes to its log once the server listens (§H.4.1, §H.5).
/// </summary>
/// <remarks>
/// Written once the server has started, not before: only then are the
/// listening addresses known — a port of 0 becomes a real port, and a
/// wildcard stays a wildcard.
/// </remarks>
public static class StartupReport
{
    /// <summary>The canonical way to publish the container's port (MEN-004).</summary>
    public const string CanonicalPublication = "-p 127.0.0.1:8080:8080";

    /// <summary>Writes the summary of what the process read, and the MEN-004 warnings.</summary>
    /// <param name="logger">Where to write.</param>
    /// <param name="settings">The settings the host read.</param>
    /// <param name="generator">The generator as the host found it.</param>
    /// <param name="addresses">The addresses the server listens on.</param>
    /// <param name="inContainer">Whether the process runs in a container (DOTNET_RUNNING_IN_CONTAINER).</param>
    public static void Write(
        ILogger logger,
        PawnsmithSettings settings,
        GeneratorSetup generator,
        IEnumerable<string> addresses,
        bool inContainer)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(generator);
        ArgumentNullException.ThrowIfNull(addresses);

        logger.LogInformation(
            "Pawnsmith {Version} started. Projects: {ProjectsRoot}. Configuration: {ConfigDirectory}. Logs: {LogsDirectory}. Generator: {GeneratorState} at {GeneratorAddress}",
            Version(),
            settings.ProjectsRoot,
            settings.ConfigDirectory,
            settings.Logs.Directory,
            generator.Configuration,
            generator.Address ?? "(none)");

        if (generator.Configuration == GeneratorConfiguration.Misconfigured)
        {
            // The message GET /api/generator does not give (DEC-084): it names
            // a file path or the refused address.
            logger.LogWarning(
                "The generator is misconfigured, generation is unavailable: {Code}. {Reason}",
                generator.ErrorCode,
                generator.ErrorMessage);
        }

        foreach (string address in ListeningAddresses.NonLocal(addresses))
        {
            if (inContainer)
            {
                // A container listens on every interface by necessity and
                // cannot see how its port is published on the host (DEC-093).
                logger.LogWarning(
                    "Listening on {Address}, which is not loopback. Inside a container this is expected, but the container cannot see how its port is published: the application has no authentication, so check that it is published with {Publication} (MEN-004)",
                    address,
                    CanonicalPublication);
            }
            else
            {
                logger.LogWarning(
                    "Listening on {Address}, which is not loopback: anyone who can reach this machine can use the application, which has no authentication (MEN-004)",
                    address);
            }
        }
    }

    /// <summary>The version of this build, read by reflection, never copied into a constant (DEC-058).</summary>
    private static string Version() =>
        typeof(StartupReport).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "unknown";
}
