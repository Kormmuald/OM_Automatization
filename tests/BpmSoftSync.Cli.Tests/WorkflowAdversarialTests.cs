using BpmSoftSync.Application;
using BpmSoftSync.Adapters.BpmSoft;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Tests;

public static class WorkflowAdversarialTests
{
    public static async Task PagingFailureIsTerminalAfterExactlyTwoReadsAsync()
    {
        var source = new BrokenPagingSource();
        var result = await new CatalogQualificationService(source).QualifyAsync();
        if (result.IsQualified || result.Result.Reason != "CATALOG_ORDER_OR_PAGING_UNQUALIFIED" || source.ReadCount != 2 || result.RetryCount != 0) throw new InvalidOperationException("Paging failure was not terminal and bounded.");
    }

    public static async Task TargetMutationHasNoRetryOrThirdPassAsync()
    {
        var fixture = Path.Combine("tests", "fixtures", "read-only", "target-state-change.json");
        var source = new FixtureCatalogSource(fixture);
        var result = await new CatalogQualificationService(source).QualifyAsync();
        if (result.IsQualified || result.Result.Reason != "TARGET_STATE_CHANGED_DURING_QUALIFICATION" || result.RetryCount != 0 || source.ReadCount != 2) throw new InvalidOperationException("Target mutation retried or did not create a named terminal blocker.");
    }

    private sealed class BrokenPagingSource : ICatalogSource
    {
        public int ReadCount { get; private set; }
        public ValueTask<CatalogPassInput> ReadPassAsync(string target, string scope, int passNumber, CancellationToken cancellationToken = default)
        {
            ReadCount++;
            return ValueTask.FromResult(new CatalogPassInput("bad-paging", target, scope, ["v1"], new CollectionDefinition("fixture", "id", 3), [new CatalogPage("0", ["one"], false), new CatalogPage("0", ["two"], true)], [], [], [], 2));
        }
    }
}
