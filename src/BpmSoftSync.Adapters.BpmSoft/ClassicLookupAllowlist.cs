namespace BpmSoftSync.Adapters.BpmSoft;

/// <summary>
/// Classical business lookups inherited from the legacy Google Sheets configuration and
/// confirmed by name in the current BPMSoft LookupRegistry.  Every other registry entry is
/// deliberately outside export and future update scope.
/// </summary>
internal static class ClassicLookupAllowlist
{
    private static readonly HashSet<string> Names = new(StringComparer.Ordinal)
    {
        "AccountType",
        "ActivityCategory",
        "ActivityPriority",
        "ActivityResult",
        "ActivityStatus",
        "AddressType"
    };

    internal static bool Contains(string schemaName) => Names.Contains(schemaName);
}
