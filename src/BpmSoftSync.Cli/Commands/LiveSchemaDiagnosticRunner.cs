using BpmSoftSync.Adapters.BpmSoft;
using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Commands;

/// <summary>
/// Terminal-only production runner for one bounded schema diagnostic. It composes
/// only login, workspace inventory and schema reads; no output is accepted or made.
/// </summary>
public sealed class LiveSchemaDiagnosticRunner(
    Func<InteractiveCredentials>? credentials = null,
    Func<BpmSoftTargetOrigin, BpmSoftReadTransport>? transportFactory = null) : ILiveSchemaDiagnosticRunner
{
    public async ValueTask<SafeResult> ExecuteAsync(string targetAlias, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetAlias);
        using var supplied = credentials is null ? new TerminalCredentialPrompt().Read() : credentials();
        using var transport = transportFactory is null
            ? BpmSoftReadTransport.CreateProduction(supplied.TargetOrigin)
            : transportFactory(supplied.TargetOrigin);
        await transport.LoginAsync(supplied, cancellationToken);
        return await new BoundedSchemaDiagnosticWorkflow(new BpmSoftSchemaDiagnosticSource(transport)).ExecuteAsync(cancellationToken);
    }
}
