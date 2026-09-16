using System.Text;
using System.Text.Json;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.FileSystem;

public sealed record CompareArtifactPaths(string ReportPath, string PlanPath, string ReportHash, string PlanHash);

/// <summary>Writes one local comparison result. The report is value-free; raw desired values appear only in the local plan.</summary>
public static class CompareArtifactWriter
{
    public static CompareArtifactPaths WriteNew(string outputRoot, CompareWorkbookPair pair, CompareMvpResult result, string targetFingerprint)
    {
        if (!Path.IsPathFullyQualified(outputRoot) || Directory.Exists(outputRoot) || File.Exists(outputRoot)) throw new InvalidDataException("COMPARE_OUTPUT_ROOT_MUST_BE_NEW");
        Directory.CreateDirectory(outputRoot);
        var reportPath = Path.Combine(outputRoot, "compare-report.md");
        var planPath = Path.Combine(outputRoot, "compare-plan.json");
        var report = RenderReport(pair, result, targetFingerprint);
        var plan = new
        {
            schema = "ComparePlan/v1", status = result.Status, targetAlias = pair.TargetAlias, binding = new { workbookBinding = pair.BindingDigest, modelWorkbookSha256 = pair.ModelHash, lookupWorkbookSha256 = pair.LookupHash, currentTargetFingerprint = targetFingerprint },
            counts = result.Counts, blockers = result.Blockers.OrderBy(item => item.Code, StringComparer.Ordinal),
            operations = result.Operations.OrderBy(item => item.Kind, StringComparer.Ordinal).ThenBy(item => item.SchemaUId).ThenBy(item => item.RecordId).ThenBy(item => item.ColumnUId).Select(item => new { item.Kind, schemaUId = item.SchemaUId.ToString("D"), recordId = item.RecordId == Guid.Empty ? null : item.RecordId.ToString("D"), columnUId = item.ColumnUId.ToString("D"), item.DesiredState, item.ValueKind, desiredValue = item.DesiredValue, item.DesiredValueHash, item.ExpectedCurrentFingerprint })
        };
        File.WriteAllText(reportPath, report, new UTF8Encoding(false));
        File.WriteAllText(planPath, JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        return new(reportPath, planPath, HashFile(reportPath), HashFile(planPath));
    }

    private static string RenderReport(CompareWorkbookPair pair, CompareMvpResult result, string targetFingerprint)
    {
        var lines = new List<string> { "# Compare MVP report", "", $"Status: `{result.Status}`", $"Target: `{pair.TargetAlias}`", $"Model SHA-256: `{pair.ModelHash}`", $"Lookup SHA-256: `{pair.LookupHash}`", $"Binding SHA-256: `{pair.BindingDigest}`", $"Current target fingerprint: `{targetFingerprint}`", "", "## Counts", "" };
        lines.AddRange(result.Counts.OrderBy(item => item.Key, StringComparer.Ordinal).Select(item => $"- `{item.Key}`: {item.Value}"));
        lines.Add("\n## Operations\n");
        lines.AddRange(result.Operations.OrderBy(item => item.Kind, StringComparer.Ordinal).ThenBy(item => item.SchemaUId).ThenBy(item => item.RecordId).ThenBy(item => item.ColumnUId).Select(item => $"- `{item.Kind}` — schema `{item.SchemaUId:D}`, record `{(item.RecordId == Guid.Empty ? "none" : item.RecordId.ToString("D"))}`, column `{item.ColumnUId:D}`, desired-state `{item.DesiredState}`, value-kind `{item.ValueKind ?? "none"}`, desired-value SHA-256 `{item.DesiredValueHash}`, current fingerprint `{item.ExpectedCurrentFingerprint}`."));
        lines.Add("\n## Blockers\n");
        lines.AddRange(result.Blockers.OrderBy(item => item.Code, StringComparer.Ordinal).ThenBy(item => item.StableKeyHash, StringComparer.Ordinal).Select(item => $"- `{item.Code}` — stable-key SHA-256 `{item.StableKeyHash}`. Действие: {item.Action}"));
        lines.Add("\nЭтот отчёт не содержит фактических lookup values. `compare-plan.json` является конфиденциальным локальным артефактом.");
        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }
    private static string HashFile(string path) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
}
