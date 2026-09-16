using BpmSoftSync.Domain;

namespace BpmSoftSync.Application;

/// <summary>Application boundary for one read-only in-memory Compare MVP evaluation.</summary>
public sealed class CompareMvpWorkflow
{
    public CompareMvpResult Execute(CompareWorkbookPair pair, WorkspaceObjectModel workspace, LookupCatalog lookups) =>
        CompareMvpService.Compare(pair, workspace, lookups);
}
