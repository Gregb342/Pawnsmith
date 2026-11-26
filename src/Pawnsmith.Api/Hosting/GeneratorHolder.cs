using System.Text.Json.Nodes;

using Pawnsmith.Api.Errors;
using Pawnsmith.Infrastructure.Generation;
using Pawnsmith.Infrastructure.Json;

namespace Pawnsmith.Api.Hosting;

/// <summary>
/// The generator in force, replaceable while the application runs (§I.5,
/// DEC-108).
/// </summary>
/// <remarks>
/// <para>
/// <b>Why a holder.</b> Until T6's front the generator was built once, at
/// start-up, and every service held it. The address can now change from the
/// interface. Every reader asks this holder for <see cref="Current"/> at the
/// moment it needs it, and a change replaces it in one assignment.
/// </para>
/// <para>
/// <b>A queued batch keeps its generator.</b> The start route captures
/// <see cref="Current"/> into the job (§I.5.2). Changing the address is
/// therefore for the next batch, and a generator replaced while a batch still
/// uses it must stay alive: replaced generators are kept, and disposed with
/// the holder when the host stops.
/// </para>
/// </remarks>
/// <param name="initial">The setup built at start-up.</param>
/// <param name="settings">The application's settings: the user directory, the configuration's workflow.</param>
/// <param name="initialAddress">
/// The address the initial setup was built with, as it was written — the
/// setup itself only keeps it for display, and drops one it refused. Kept here
/// so that importing a workflow rebuilds the generator on the same address
/// (§I.16.1).
/// </param>
public sealed class GeneratorHolder(GeneratorSetup initial, PawnsmithSettings settings, string? initialAddress) : IDisposable
{
    private readonly SemaphoreSlim changes = new(1, 1);
    private readonly List<GeneratorSetup> built = [];
    private readonly WorkflowLocation workflow = new(settings);
    private GeneratorSetup current = initial;
    private string? address = initialAddress;

    public GeneratorSetup Current => Volatile.Read(ref current);

    /// <summary>Where the workflow in force comes from: <c>Imported</c>, <c>Configuration</c> or <c>None</c> (DEC-119).</summary>
    public string WorkflowSource => workflow.Source;

    /// <summary>Validates, saves and applies a new address; <c>null</c> means no generator.</summary>
    /// <returns>The new setup — configured, or misconfigured when the workflow is the problem.</returns>
    /// <exception cref="ApiException"><c>GENERATOR_ADDRESS_INVALID</c>: nothing was saved or changed.</exception>
    public async Task<GeneratorSetup> ReplaceAsync(string? address, CancellationToken cancellationToken)
    {
        string? trimmed = string.IsNullOrWhiteSpace(address) ? null : address.Trim();

        if (trimmed is not null)
        {
            try
            {
                GeneratorAddress.Parse(trimmed);
            }
            catch (GeneratorConfigException)
            {
                // A code of its own rather than GENERATOR_URL_INVALID, which
                // is a deployment fault (503): this one is a request the user
                // can correct (422). The address is not repeated anywhere.
                throw new ApiException(ApiCodes.GeneratorAddressInvalid);
            }
        }

        await changes.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            GeneratorSetup next = trimmed is null
                ? GeneratorSetup.NotConfigured()
                : await GeneratorSetup.LoadAsync(trimmed, workflow.Current, cancellationToken).ConfigureAwait(false);

            // Saved before it is applied: an address in force that a restart
            // would forget is worse than one refused. A save that fails leaves
            // the setup just built unused: its HTTP client is released here,
            // since nobody else will ever hold it.
            try
            {
                await new GeneratorAddressFile(settings.UserDirectory).WriteAsync(trimmed, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                next.Dispose();
                throw;
            }

            address = trimmed;
            Apply(next);

            return next;
        }
        finally
        {
            changes.Release();
        }
    }

    /// <summary>
    /// Prepares, saves and applies a workflow chosen in the interface (§I.16,
    /// DEC-119, DEC-120): a raw ComfyUI export or a file already prepared.
    /// </summary>
    /// <returns>The new setup, on the same address.</returns>
    /// <exception cref="WorkflowImportException">The file cannot be prepared: nothing was saved or changed.</exception>
    public async Task<GeneratorSetup> ImportWorkflowAsync(string json, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(json);

        // Read before anything is decided, and handed to the preparer as a
        // function: only a raw export needs it. A missing file fails only
        // the import that needs it, with the deployment's code (503).
        IReadOnlyList<string>? framing = null;
        Exception? framingFailure = null;

        try
        {
            framing = await FramingClauseFile.ReadAsync(workflow.FramingClausePath, cancellationToken).ConfigureAwait(false);
        }
        catch (GeneratorConfigException error)
        {
            framingFailure = error;
        }

        JsonObject document = WorkflowPreparer.Prepare(json, () => framing ?? throw framingFailure!);

        await changes.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await UserFile.WriteJsonAsync(workflow.ImportedPath, document, cancellationToken).ConfigureAwait(false);
            await ReloadAsync(cancellationToken).ConfigureAwait(false);

            return Current;
        }
        finally
        {
            changes.Release();
        }
    }

    /// <summary>Removes the imported workflow: the configuration's file applies again (DEC-119).</summary>
    public async Task<GeneratorSetup> RemoveImportedWorkflowAsync(CancellationToken cancellationToken)
    {
        await changes.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            File.Delete(workflow.ImportedPath);
            await ReloadAsync(cancellationToken).ConfigureAwait(false);

            return Current;
        }
        finally
        {
            changes.Release();
        }
    }

    /// <summary>Rebuilds the generator on the current address and the workflow now in force. Inside the lock.</summary>
    private async Task ReloadAsync(CancellationToken cancellationToken)
    {
        GeneratorSetup next = address is null
            ? GeneratorSetup.NotConfigured()
            : await GeneratorSetup.LoadAsync(address, workflow.Current, cancellationToken).ConfigureAwait(false);

        Apply(next);
    }

    /// <summary>
    /// Puts a new setup in force. The one it replaces stays alive: a queued
    /// batch may still use its generator (§I.5.2). All are disposed with the
    /// holder.
    /// </summary>
    private void Apply(GeneratorSetup next)
    {
        built.Add(next);
        Volatile.Write(ref current, next);
    }

    /// <summary>
    /// Disposes the setups this holder built. The initial one belongs to the
    /// container, which disposes it.
    /// </summary>
    public void Dispose()
    {
        foreach (GeneratorSetup setup in built)
        {
            setup.Dispose();
        }

        changes.Dispose();
    }
}
