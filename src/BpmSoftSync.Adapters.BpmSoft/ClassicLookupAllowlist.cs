namespace BpmSoftSync.Adapters.BpmSoft;

/// <summary>
/// Nonstandard BPMSoft lookups that are deliberately outside catalog export scope.
/// The exclusion derives from the BPMSoft 1.9 documentation for external-resource
/// accounts, calendars, and message templates. Every other LookupRegistry entry is
/// eligible for export and is still subject to the normal schema/value validation.
/// </summary>
internal static class LookupExportExclusions
{
    private static readonly HashSet<string> Names = new(StringComparer.Ordinal)
    {
        "SocialAccount",
        "Calendar",
        "EmailTemplate"
    };

    internal static bool ShouldExport(string schemaName) => !Names.Contains(schemaName);
}
