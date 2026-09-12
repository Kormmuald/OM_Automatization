using System.Text.Json;

namespace BpmSoftSync.Domain;

public enum LegacyDisposition
{
    ReuseSemantics,
    Rewrite,
    Drop,
    Defer
}

public sealed record LegacyDispositionEntry(string Id, LegacyDisposition Disposition);
public sealed record LegacyDispositionManifest(IReadOnlyList<LegacyDispositionEntry> Entries)
{
    public static bool TryRead(string json, out LegacyDispositionManifest? manifest, out Blocker? blocker)
    {
        manifest = null;
        blocker = null;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("schema", out var schema) || schema.GetString() != "LegacyDispositionManifest/v1" || !document.RootElement.TryGetProperty("semantics", out var semantics) || semantics.ValueKind != JsonValueKind.Array)
            {
                return Invalid(out blocker);
            }

            var entries = new List<LegacyDispositionEntry>();
            foreach (var item in semantics.EnumerateArray())
            {
                if (!item.TryGetProperty("id", out var idProperty) || string.IsNullOrWhiteSpace(idProperty.GetString()) || !item.TryGetProperty("disposition", out var dispositionProperty) || !TryDisposition(dispositionProperty.GetString(), out var disposition))
                {
                    return Invalid(out blocker);
                }
                entries.Add(new LegacyDispositionEntry(idProperty.GetString()!, disposition));
            }
            if (entries.Count == 0 || entries.Select(entry => entry.Id).Distinct(StringComparer.Ordinal).Count() != entries.Count) return Invalid(out blocker);
            manifest = new LegacyDispositionManifest(entries);
            return true;
        }
        catch (JsonException)
        {
            return Invalid(out blocker);
        }
    }

    private static bool Invalid(out Blocker? blocker)
    {
        blocker = new Blocker(BlockerCode.LegacyDispositionInvalid, "legacy", "LEGACY_DISPOSITION_INVALID", "Classify every semantic as reuse-semantics, rewrite, drop or defer.", "Stop this qualification run.");
        return false;
    }

    private static bool TryDisposition(string? source, out LegacyDisposition disposition)
    {
        disposition = source switch
        {
            "reuse-semantics" => LegacyDisposition.ReuseSemantics,
            "rewrite" => LegacyDisposition.Rewrite,
            "drop" => LegacyDisposition.Drop,
            "defer" => LegacyDisposition.Defer,
            _ => default
        };
        return source is "reuse-semantics" or "rewrite" or "drop" or "defer";
    }
}
