using BpmSoftSync.Domain;

namespace BpmSoftSync.Domain.Tests;

public static class LegacyDispositionTests
{
    public static void ManifestClassifiesEveryLegacySemantic()
    {
        var result = LegacyDispositionManifest.TryRead(File.ReadAllText(Path.Combine("tests", "fixtures", "read-only", "legacy-disposition.json")), out var manifest, out var blocker);
        Assert(result && blocker is null && manifest is not null && manifest.Entries.Count == 4 && manifest.Entries.All(entry => entry.Disposition is LegacyDisposition.ReuseSemantics or LegacyDisposition.Rewrite or LegacyDisposition.Drop or LegacyDisposition.Defer), "Legacy manifest is incomplete or invalid.");
    }

    public static void InvalidDispositionIsRejected()
    {
        var result = LegacyDispositionManifest.TryRead("{\"schema\":\"LegacyDispositionManifest/v1\",\"semantics\":[{\"id\":\"unsafe\",\"disposition\":\"port-by-copy\"}]}", out _, out var blocker);
        Assert(!result && blocker?.Code == BlockerCode.LegacyDispositionInvalid, "Unclassified legacy semantic was accepted.");
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
