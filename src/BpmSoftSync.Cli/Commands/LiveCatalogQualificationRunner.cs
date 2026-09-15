using BpmSoftSync.Adapters.BpmSoft;
using BpmSoftSync.Adapters.Excel;
using BpmSoftSync.Adapters.FileSystem;
using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Commands;

public interface ILiveCatalogQualificationRunner
{
    ValueTask<SafeResult> ExecuteAsync(string targetAlias, string outputRoot, CancellationToken cancellationToken = default);
}

/// <summary>Opt-in live harness; default dispatch and automated tests never invoke it.</summary>
public sealed class LiveCatalogQualificationRunner(Func<InteractiveCredentials>? credentials = null) : ILiveCatalogQualificationRunner
{
    public async ValueTask<SafeResult> ExecuteAsync(string targetAlias, string outputRoot, CancellationToken cancellationToken = default)
    {
        using var supplied = credentials is null ? new TerminalCredentialPrompt().Read() : credentials();
        using var transport = BpmSoftReadTransport.CreateProduction(supplied.TargetOrigin);
        await transport.LoginAsync(supplied, cancellationToken);
        var store = new AppendOnlyRunStore(outputRoot);
        var policy = CatalogQualificationPolicyFactory.Create(targetAlias, supplied.TargetOrigin);
        var source = new BpmSoftFullCatalogSource(transport, supplied.TargetOrigin);
        var qualification = new CatalogQualificationService(source, policy, store);
        var publisher = new WorkbookPairSnapshotPublisher(new AtomicWorkbookPairPublisher(store, new WorkbookPairMaterializer()));
        return await new CatalogQualificationWorkflow(qualification, publisher).ExecuteAsync(cancellationToken);
    }

}
