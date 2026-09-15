using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.FileSystem;

/// <summary>
/// Diagnostic-only append-once store. Its run tree contains no audit, output,
/// workbook, full-catalog journal, or response material.
/// </summary>
public sealed class AppendOnlySchemaDiagnosticEvidenceStore : ISchemaDiagnosticEvidenceStore
{
    private readonly string _evidenceRoot;
    private readonly Func<SchemaDiagnosticEvidencePersistenceStep, CancellationToken, ValueTask>? _beforeStep;
    private readonly SchemaDiagnosticEvidenceValidator _validator = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AppendOnlySchemaDiagnosticEvidenceStore(string evidenceRoot) : this(evidenceRoot, null) { }

    internal AppendOnlySchemaDiagnosticEvidenceStore(string evidenceRoot, Func<SchemaDiagnosticEvidencePersistenceStep, CancellationToken, ValueTask>? beforeStep)
    {
        _evidenceRoot = Path.GetFullPath(evidenceRoot);
        _beforeStep = beforeStep;
    }

    public async ValueTask<SchemaDiagnosticEvidenceWrite> SealTerminalAsync(string targetAlias, SafeResult terminal, CancellationToken cancellationToken = default)
    {
        if (!IsTargetAlias(targetAlias) || !TryMap(terminal, out var outcome))
            return new(Failed("DIAGNOSTIC_EVIDENCE_TERMINAL_NOT_ALLOWED"), null);

        var evidenceToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(20)).ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;
        var finalRoot = Path.Combine(_evidenceRoot, "diagnostic-runs", now.UtcDateTime.ToString("yyyy"), now.UtcDateTime.ToString("MM"), now.UtcDateTime.ToString("dd"), evidenceToken);
        var finalParent = Path.GetDirectoryName(finalRoot)!;
        var stagingRoot = Path.Combine(finalParent, ".staging-" + evidenceToken);
        var evidence = new SchemaDiagnosticTerminalEvidence(SchemaDiagnosticTerminalEvidence.SchemaVersion, targetAlias, SchemaDiagnosticTerminalEvidence.RouteVersion, outcome, terminal.Blocker?.FailedShape);
        var json = JsonSerializer.Serialize(evidence, JsonOptions);
        var validated = _validator.Validate(json);
        if (!validated.IsSuccess) return new(validated, null);

        var published = false;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!TryEnsureDirectoryPath(finalParent) || Directory.Exists(finalRoot) || Directory.Exists(stagingRoot))
                return new(Failed("DIAGNOSTIC_EVIDENCE_WRITE_FAILED"), null);
            Directory.CreateDirectory(stagingRoot);
            if (!IsPhysicalDirectory(stagingRoot)) return new(Failed("DIAGNOSTIC_EVIDENCE_WRITE_FAILED"), null);

            var terminalPath = Path.Combine(stagingRoot, "diagnostic-terminal.json");
            await WriteNewAsync(terminalPath, json, cancellationToken);
            await InvokeBeforeStepAsync(SchemaDiagnosticEvidencePersistenceStep.AfterTerminalWrite, cancellationToken);
            var readBack = await File.ReadAllTextAsync(terminalPath, cancellationToken);
            var readBackValidation = _validator.Validate(readBack);
            if (!readBackValidation.IsSuccess) return new(readBackValidation, null);
            var seal = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(readBack))).ToLowerInvariant();
            await InvokeBeforeStepAsync(SchemaDiagnosticEvidencePersistenceStep.BeforeSealWrite, cancellationToken);
            await WriteNewAsync(Path.Combine(stagingRoot, ".sealed"), seal, cancellationToken);
            if (!ValidateDirectory(stagingRoot, out _).IsSuccess) return new(Failed("DIAGNOSTIC_EVIDENCE_INVALID"), null);
            await InvokeBeforeStepAsync(SchemaDiagnosticEvidencePersistenceStep.BeforePublish, cancellationToken);
            Directory.Move(stagingRoot, finalRoot);
            published = true;
            return new(SafeResult.SuccessForHumanReview("schema-diagnostic-evidence"), evidenceToken);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            return new(Failed("DIAGNOSTIC_EVIDENCE_WRITE_FAILED"), null);
        }
        finally
        {
            if (!published) TryDeleteStaging(stagingRoot);
        }
    }

    public SafeResult ReadBack(string evidenceToken, out SchemaDiagnosticTerminalEvidence? evidence)
    {
        evidence = null;
        var diagnosticRuns = Path.Combine(_evidenceRoot, "diagnostic-runs");
        var roots = Directory.Exists(diagnosticRuns) && IsEvidenceToken(evidenceToken)
            ? Directory.EnumerateDirectories(diagnosticRuns, evidenceToken, SearchOption.AllDirectories).Take(2).ToArray()
            : [];
        if (roots.Length != 1) return Failed("DIAGNOSTIC_EVIDENCE_NOT_FOUND");
        var validation = ValidateDirectory(roots[0], out var json);
        if (!validation.IsSuccess || json is null) return validation;
        evidence = JsonSerializer.Deserialize<SchemaDiagnosticTerminalEvidence>(json, JsonOptions);
        return evidence is null ? Failed("DIAGNOSTIC_EVIDENCE_INVALID") : SafeResult.SuccessForHumanReview("schema-diagnostic-evidence");
    }

    private async ValueTask InvokeBeforeStepAsync(SchemaDiagnosticEvidencePersistenceStep step, CancellationToken cancellationToken)
    {
        if (_beforeStep is not null) await _beforeStep(step, cancellationToken);
    }

    private SafeResult ValidateDirectory(string root, out string? json)
    {
        json = null;
        if (!ExistingDirectoryPathIsPhysical(root)) return Failed("DIAGNOSTIC_EVIDENCE_INVALID");
        var names = Directory.EnumerateFileSystemEntries(root).Select(Path.GetFileName).OrderBy(value => value, StringComparer.Ordinal).ToArray();
        if (!names.SequenceEqual([".sealed", "diagnostic-terminal.json"], StringComparer.Ordinal)) return Failed("DIAGNOSTIC_EVIDENCE_INVALID");
        var terminalPath = Path.Combine(root, "diagnostic-terminal.json");
        var sealPath = Path.Combine(root, ".sealed");
        if (!File.Exists(terminalPath) || !File.Exists(sealPath)) return Failed("DIAGNOSTIC_EVIDENCE_NOT_SEALED");
        json = File.ReadAllText(terminalPath);
        var validation = _validator.Validate(json);
        return !validation.IsSuccess || !string.Equals(File.ReadAllText(sealPath), Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant(), StringComparison.Ordinal)
            ? Failed("DIAGNOSTIC_EVIDENCE_INVALID")
            : SafeResult.SuccessForHumanReview("schema-diagnostic-evidence");
    }

    private static bool TryEnsureDirectoryPath(string directory)
    {
        var full = Path.GetFullPath(directory);
        var volume = Path.GetPathRoot(full);
        if (string.IsNullOrWhiteSpace(volume)) return false;
        if (!IsPhysicalDirectory(volume)) return false;
        var current = volume;
        foreach (var segment in full[volume.Length..].Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current)) return false;
            if (!Directory.Exists(current)) Directory.CreateDirectory(current);
            if (!IsPhysicalDirectory(current)) return false;
        }
        return true;
    }

    private static bool ExistingDirectoryPathIsPhysical(string directory)
    {
        var full = Path.GetFullPath(directory);
        var volume = Path.GetPathRoot(full);
        if (string.IsNullOrWhiteSpace(volume) || !IsPhysicalDirectory(volume)) return false;
        var current = volume;
        foreach (var segment in full[volume.Length..].Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current) || !IsPhysicalDirectory(current)) return false;
        }
        return true;
    }

    private static bool IsPhysicalDirectory(string path)
    {
        if (!Directory.Exists(path) || File.Exists(path)) return false;
        var directory = new DirectoryInfo(path);
        return (directory.Attributes & FileAttributes.ReparsePoint) == 0 && directory.LinkTarget is null;
    }

    private static void TryDeleteStaging(string stagingRoot)
    {
        try
        {
            if (Directory.Exists(stagingRoot) && IsPhysicalDirectory(stagingRoot)) Directory.Delete(stagingRoot, true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static async Task WriteNewAsync(string path, string value, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(value), cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static bool TryMap(SafeResult terminal, out SchemaDiagnosticTerminalOutcome outcome)
    {
        outcome = terminal.Reason switch
        {
            "SCHEMA_INVENTORY_UNQUALIFIED" => SchemaDiagnosticTerminalOutcome.UnknownShapeUnqualified,
            "SCHEMA_READ_UNAVAILABLE" or "SCHEMA_DIAGNOSTIC_READ_UNAVAILABLE" => SchemaDiagnosticTerminalOutcome.SchemaReadUnavailable,
            "ENDPOINT_NOT_ALLOWLISTED" => SchemaDiagnosticTerminalOutcome.EndpointNotAllowlisted,
            "SCHEMA_DIAGNOSTIC_NO_SCHEMA_CANDIDATE" => SchemaDiagnosticTerminalOutcome.NoSchemaCandidate,
            "SCHEMA_DIAGNOSTIC_COMPLETED_WITHOUT_BLOCKER" => SchemaDiagnosticTerminalOutcome.CompletedWithoutBlocker,
            _ => default
        };
        return terminal.Reason is "SCHEMA_INVENTORY_UNQUALIFIED" or "SCHEMA_READ_UNAVAILABLE" or "SCHEMA_DIAGNOSTIC_READ_UNAVAILABLE" or "ENDPOINT_NOT_ALLOWLISTED" or "SCHEMA_DIAGNOSTIC_NO_SCHEMA_CANDIDATE" or "SCHEMA_DIAGNOSTIC_COMPLETED_WITHOUT_BLOCKER";
    }

    private static bool IsTargetAlias(string? value) => !string.IsNullOrWhiteSpace(value) && value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
    private static bool IsEvidenceToken(string? value) => value is { Length: 40 } && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static SafeResult Failed(string reason) => SafeResult.Blocked(new Blocker(BlockerCode.EvidenceSchemaInvalid, "schema-diagnostic-evidence", reason, "Inspect the local diagnostic evidence root and start a new human-authorized diagnostic only after review.", "Do not retry, publish output, or run qualification."));
}

internal enum SchemaDiagnosticEvidencePersistenceStep
{
    AfterTerminalWrite,
    BeforeSealWrite,
    BeforePublish
}
