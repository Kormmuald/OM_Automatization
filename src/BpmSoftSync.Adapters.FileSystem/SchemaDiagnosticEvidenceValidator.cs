using System.Text.Json;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.FileSystem;

/// <summary>Strict allowlist for the diagnostic-only terminal record.</summary>
public sealed class SchemaDiagnosticEvidenceValidator
{
    private static readonly HashSet<string> AllowedRoot = new(StringComparer.Ordinal)
    {
        "schema", "targetAlias", "route", "outcome", "failedShape"
    };
    private readonly SecretValueScanner _scanner = new();

    public SafeResult Validate(string json)
    {
        if (_scanner.Scan(json, "schema-diagnostic-evidence").Count != 0)
            return Failed("DIAGNOSTIC_EVIDENCE_SCAN_FAILED");
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != AllowedRoot.Count || root.EnumerateObject().Any(item => !AllowedRoot.Contains(item.Name)) ||
                !root.TryGetProperty("schema", out var schema) || schema.ValueKind != JsonValueKind.String || schema.GetString() != SchemaDiagnosticTerminalEvidence.SchemaVersion ||
                !root.TryGetProperty("targetAlias", out var alias) || alias.ValueKind != JsonValueKind.String || !IsTargetAlias(alias.GetString()) ||
                !root.TryGetProperty("route", out var route) || route.ValueKind != JsonValueKind.String || route.GetString() != SchemaDiagnosticTerminalEvidence.RouteVersion ||
                !root.TryGetProperty("outcome", out var outcome) || outcome.ValueKind != JsonValueKind.Number || !outcome.TryGetInt32(out var outcomeValue) || !Enum.IsDefined(typeof(SchemaDiagnosticTerminalOutcome), outcomeValue) ||
                !root.TryGetProperty("failedShape", out var failedShape) || !ValidateFailedShape(failedShape))
                return Failed("DIAGNOSTIC_EVIDENCE_SCHEMA_INVALID");
        }
        catch (JsonException) { return Failed("DIAGNOSTIC_EVIDENCE_SCHEMA_INVALID"); }
        return SafeResult.SuccessForHumanReview("schema-diagnostic-evidence");
    }

    private static bool ValidateFailedShape(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null) return true;
        var expectedNames = new[] { "path", "expected", "observed", "arrayCardinality", "ordinal" };
        var expectedWithCompanion = expectedNames.Append("companionGuidStringStatus");
        if (value.ValueKind != JsonValueKind.Object || !value.EnumerateObject().Select(item => item.Name).OrderBy(name => name, StringComparer.Ordinal).SequenceEqual(expectedNames.OrderBy(name => name, StringComparer.Ordinal), StringComparer.Ordinal) && !value.EnumerateObject().Select(item => item.Name).OrderBy(name => name, StringComparer.Ordinal).SequenceEqual(expectedWithCompanion.OrderBy(name => name, StringComparer.Ordinal), StringComparer.Ordinal)) return false;
        return value.TryGetProperty("path", out var path) && path.ValueKind == JsonValueKind.Number && path.TryGetInt32(out var pathValue) && Enum.IsDefined(typeof(FailedShapePath), pathValue) &&
            value.TryGetProperty("expected", out var expected) && expected.ValueKind == JsonValueKind.Number && expected.TryGetInt32(out var expectedValue) && Enum.IsDefined(typeof(ExpectedShapeCategory), expectedValue) &&
            value.TryGetProperty("observed", out var observed) && observed.ValueKind == JsonValueKind.Number && observed.TryGetInt32(out var observedValue) && Enum.IsDefined(typeof(ObservedJsonKind), observedValue) &&
            value.TryGetProperty("arrayCardinality", out var cardinality) && cardinality.ValueKind == JsonValueKind.Number && cardinality.TryGetInt32(out var cardinalityValue) && Enum.IsDefined(typeof(ArrayCardinalityBucket), cardinalityValue) &&
            value.TryGetProperty("ordinal", out var ordinal) && (ordinal.ValueKind == JsonValueKind.Null || ordinal.ValueKind == JsonValueKind.Number && ordinal.TryGetInt32(out var ordinalValue) && ordinalValue is >= 0 and <= 9_999_999) &&
            (!value.TryGetProperty("companionGuidStringStatus", out var companion) || companion.ValueKind == JsonValueKind.Null || pathValue == (int)FailedShapePath.SchemaPackageId && companion.ValueKind == JsonValueKind.Number && companion.TryGetInt32(out var companionValue) && Enum.IsDefined(typeof(GuidStringPredicateStatus), companionValue));
    }

    private static bool IsTargetAlias(string? value) => !string.IsNullOrWhiteSpace(value) && value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
    private static SafeResult Failed(string reason) => SafeResult.Blocked(new Blocker(BlockerCode.EvidenceSchemaInvalid, "schema-diagnostic-evidence", reason, "Create a new bounded diagnostic with only the closed evidence contract.", "Do not retry, publish output, or run qualification."));
}
