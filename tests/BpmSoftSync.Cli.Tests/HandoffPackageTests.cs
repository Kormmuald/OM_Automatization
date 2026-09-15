namespace BpmSoftSync.Cli.Tests;
public static class HandoffPackageTests
{
    public static void HandoffIsFixtureOnlyAndSecretFree()
    {
        var root = Path.Combine("docs", "read-only-handoff");
        foreach (var name in new[] { "configuration-schema.md", "install-update-prerequisites.md", "operator-runbook.md", "failure-path.md" })
            if (!File.Exists(Path.Combine(root, name))) throw new InvalidOperationException("Missing handoff file: " + name);
        var all = string.Join("\n", Directory.EnumerateFiles(root, "*.md").Select(File.ReadAllText));
        if (!all.Contains("HUMAN_REVIEW_REQUIRED", StringComparison.Ordinal) || !all.Contains("FULL_CATALOG_NOT_QUALIFIED", StringComparison.Ordinal) || all.Contains("C:\\", StringComparison.Ordinal)) throw new InvalidOperationException("Handoff is incomplete or contains a hidden local path.");
    }
}
