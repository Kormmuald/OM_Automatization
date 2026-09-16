using BpmSoftSync.Adapters.BpmSoft;

namespace BpmSoftSync.Adapters.BpmSoft.Tests;

public static class LookupExportExclusionsTests
{
    public static void OnlyDocumentedNonstandardLookupsAreExcluded()
    {
        Assert(!LookupExportExclusions.ShouldExport("SocialAccount"), "SocialAccount must be excluded from export.");
        Assert(!LookupExportExclusions.ShouldExport("Calendar"), "Calendar must be excluded from export.");
        Assert(!LookupExportExclusions.ShouldExport("EmailTemplate"), "EmailTemplate must be excluded from export.");
        Assert(LookupExportExclusions.ShouldExport("AccountType"), "A regular lookup must remain exportable.");
        Assert(LookupExportExclusions.ShouldExport("CustomBusinessLookup"), "A non-excluded lookup must remain exportable.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
