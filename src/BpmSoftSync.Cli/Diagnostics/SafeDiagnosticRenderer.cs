using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Diagnostics;

public static class SafeDiagnosticRenderer
{
    public static string Render(SafeResult result)
    {
        var standard = $"reason={result.Reason}{Environment.NewLine}scope={result.Scope}{Environment.NewLine}recovery={result.Recovery}{Environment.NewLine}nextAction={result.NextPermittedAction}";
        var diagnostic = result.Blocker?.FailedShape;
        if (diagnostic is null) return standard;
        var companion = diagnostic.Path == FailedShapePath.SchemaPackageId && diagnostic.CompanionGuidStringStatus is not null
            ? $";companionGuidString:{diagnostic.CompanionGuidStringStatus}"
            : string.Empty;
        return standard + Environment.NewLine + $"failedShape=path:{diagnostic.Path};expected:{diagnostic.Expected};observed:{diagnostic.Observed};array:{diagnostic.ArrayCardinality};ordinal:{diagnostic.Ordinal?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "none"}{companion}";
    }
}
