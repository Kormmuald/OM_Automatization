using System.Security.Cryptography;
using System.Text;
using BpmSoftSync.Adapters.BpmSoft;
using BpmSoftSync.Adapters.Excel;
using BpmSoftSync.Adapters.FileSystem;
using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Commands;

/// <summary>One manual, read-only live composition. It has no retry and no write-capable dependency.</summary>
public sealed class LiveCompareMvpRunner(Func<InteractiveCredentials>? credentials = null) : ILiveCompareMvpRunner
{
    public async ValueTask<SafeResult> ExecuteAsync(string modelPath, string lookupPath, string targetAlias, string outputRoot, CancellationToken cancellationToken = default)
    {
        var pair = CompareWorkbookPairReader.Read(modelPath, lookupPath, targetAlias);
        using var supplied = credentials is null ? new TerminalCredentialPrompt().Read() : credentials();
        using var transport = BpmSoftReadTransport.CreateProduction(supplied.TargetOrigin);
        await transport.LoginAsync(supplied, cancellationToken);
        var policy = CatalogQualificationPolicyFactory.Create(targetAlias, supplied.TargetOrigin);
        var source = new BpmSoftFullCatalogSource(transport, supplied.TargetOrigin);
        var read = await source.ReadBestEffortAsync(new CatalogReadRequest(CatalogPassOrdinal.A, policy, null, Guid.NewGuid()), cancellationToken);
        var result = new CompareMvpWorkflow().Execute(pair, read.Workspace, read.Lookups);
        var targetFingerprint = Fingerprint(read.Workspace, read.Lookups);
        var artifacts = CompareArtifactWriter.WriteNew(outputRoot, pair, result, targetFingerprint);
        return new SafeResult(true, null, result.Status, "compare-mvp", $"operations={result.Counts["operations"]}; blockers={result.Counts["blockers"]}; report-sha256={artifacts.ReportHash}; plan-sha256={artifacts.PlanHash}", $"Review {artifacts.ReportPath}; keep {artifacts.PlanPath} local and confidential.");
    }

    private static string Fingerprint(WorkspaceObjectModel workspace, LookupCatalog lookups)
    {
        var text = string.Join("\n", workspace.Schemas.OrderBy(item => item.Identity.SchemaUId).SelectMany(schema => schema.Columns.OrderBy(column => column.ColumnUId).Select(column => $"c|{schema.Identity.SchemaUId:D}|{column.ColumnUId:D}|{column.RequirementType}|{column.ActualIndexed}"))) + "\n" +
            string.Join("\n", lookups.Collections.OrderBy(item => item.RegistryRecord.SysEntitySchemaUId).SelectMany(collection => collection.Rows.OrderBy(row => row.RecordId).SelectMany(row => row.Values.OrderBy(value => value.ColumnUId).Select(value => $"v|{collection.RegistryRecord.SysEntitySchemaUId:D}|{row.RecordId:D}|{value.ColumnUId:D}|{value.ValueFingerprint}"))));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    }
}
