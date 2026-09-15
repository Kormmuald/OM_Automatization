using System.Text.Json;
using BpmSoftSync.Adapters.FileSystem;

namespace BpmSoftSync.Adapters.FileSystem.Tests;

public static class AuditMetadataTests
{
    public static void AllowlistAcceptsOnlyDeclaredMetadata()
    {
        var valid = EvidenceEnvelopeTests.Envelope(Guid.NewGuid(), "pass-a");
        var json = JsonSerializer.Serialize(valid, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var validator = new EvidenceEnvelopeValidator();
        if (!validator.Validate(json).IsSuccess) throw new InvalidOperationException("Declared qualification metadata was rejected.");
        if (validator.Validate(json.Replace("\"targetAlias\"", "\"credential\"", StringComparison.Ordinal)).IsSuccess) throw new InvalidOperationException("Metadata allowlist is not fail-closed.");
    }
}
