using BpmSoftSync.Domain;
namespace BpmSoftSync.Cli.Tests;
public static class ReadOnlyQualificationSecurityRegressionTests
{
    public static void TargetMutationIsTerminalWithoutRetry()
    {
        var first = new QualificationPass("A", new TargetFingerprint(TargetFingerprint.SchemaVersion, "one", []), [], [], TimeSpan.Zero);
        var second = new QualificationPass("B", new TargetFingerprint(TargetFingerprint.SchemaVersion, "two", []), [], [], TimeSpan.Zero);
        var result = CatalogQualification.Reconcile(first, second);
        if (result.Result.Reason != "TARGET_STATE_CHANGED_DURING_QUALIFICATION" || result.RetryCount != 0) throw new InvalidOperationException("Mutation must be terminal without retry or Pass C.");
    }
}
