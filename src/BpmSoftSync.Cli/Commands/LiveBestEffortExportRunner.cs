using BpmSoftSync.Adapters.BpmSoft;
using BpmSoftSync.Adapters.Excel;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Commands;

public interface ILiveBestEffortExportRunner
{
    ValueTask<SafeResult> ExecuteAsync(string targetAlias, string outputRoot, CancellationToken cancellationToken = default);
}

/// <summary>Manual read-only export which deliberately omits scope qualification and two-pass reconciliation.</summary>
public sealed class LiveBestEffortExportRunner(Func<InteractiveCredentials>? credentials = null) : ILiveBestEffortExportRunner
{
    public async ValueTask<SafeResult> ExecuteAsync(string targetAlias, string outputRoot, CancellationToken cancellationToken = default)
    {
        using var supplied = credentials is null ? new TerminalCredentialPrompt().Read() : credentials();
        using var transport = BpmSoftReadTransport.CreateProduction(supplied.TargetOrigin);
        await transport.LoginAsync(supplied, cancellationToken);
        var started = DateTimeOffset.UtcNow;
        var policy = CatalogQualificationPolicyFactory.Create(targetAlias, supplied.TargetOrigin);
        var source = new BpmSoftFullCatalogSource(transport, supplied.TargetOrigin);
        var firstRequest = new CatalogReadRequest(CatalogPassOrdinal.A, policy, null, Guid.NewGuid());
        var read = await source.ReadBestEffortAsync(firstRequest, cancellationToken);
        var scope = ScopeDescriptor.Create(policy.TargetAlias, policy.TargetOriginPolicyDigest, policy.AllowlistVersion, policy.CreatePassAContracts(read.Lookups.Collections.Select(item => item.Manifest.CollectionId).ToArray()), policy.SnapshotVersion, policy.WorkbookProjectionVersion);
        if (!CatalogPassBuilder.TryBuild(firstRequest with { SealedScope = scope }, read, out var pass, out var blocker)) return SafeResult.Blocked(blocker!);
        var snapshot = CatalogPassBuilder.CreateBestEffortSnapshot(pass!, Guid.NewGuid(), Guid.NewGuid(), started, DateTimeOffset.UtcNow);
        var staging = Path.Combine(outputRoot, ".best-effort-staging-" + snapshot.RunId.ToString("N"));
        var destination = Path.Combine(outputRoot, "best-effort-" + snapshot.RunId.ToString("N"));
        Directory.CreateDirectory(outputRoot);
        try
        {
            var pair = await new WorkbookPairMaterializer().StageBestEffortPairAsync(snapshot, staging, cancellationToken);
            Directory.Move(staging, destination);
            return new SafeResult(true, null, "BEST_EFFORT_EXPORT_CREATED", "unverified-single-read", $"Excel files: {destination}", $"Model SHA-256: {pair.ModelSha256}; Lookup SHA-256: {pair.LookupSha256}. Do not use this export for Compare or Apply.");
        }
        catch
        {
            if (Directory.Exists(staging)) Directory.Delete(staging, true);
            throw;
        }
    }
}
