using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Diagnostics;

public static class SafeDiagnosticRenderer
{
    public static string Render(SafeResult result) => $"reason={result.Reason}{Environment.NewLine}scope={result.Scope}{Environment.NewLine}recovery={result.Recovery}{Environment.NewLine}nextAction={result.NextPermittedAction}";
}
