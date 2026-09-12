using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Cli.Commands;

public sealed class CatalogValidateOfflineCommand
{
    private readonly IReadOnlyTransport _transport;

    public CatalogValidateOfflineCommand(IReadOnlyTransport transport)
    {
        _transport = transport;
    }

    public async Task<SafeResult> ExecuteAsync(string[] args, TextWriter output, CancellationToken cancellationToken = default)
    {
        if (args.Length != 2 || !string.Equals(args[0], "--fixture", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(args[1]))
        {
            return await WriteAsync(SafeResult.Blocked(new Blocker(BlockerCode.OfflineFixtureInvalid, "command", "OFFLINE_FIXTURE_REQUIRED", "Supply exactly --fixture <sanitized-fixture>.", "Use a sanitized local fixture.")), output);
        }

        var fixturePath = Path.GetFullPath(args[1]);
        if (!File.Exists(fixturePath))
        {
            return await WriteAsync(SafeResult.Blocked(new Blocker(BlockerCode.OfflineFixtureInvalid, "fixture", "OFFLINE_FIXTURE_INVALID", "Use a present sanitized fixture.", "Stop the offline run.")), output);
        }

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(fixturePath, Encoding.UTF8, cancellationToken));
        if (!document.RootElement.TryGetProperty("fixtureId", out var id) || id.ValueKind != JsonValueKind.String ||
            !document.RootElement.TryGetProperty("classification", out var classification) || classification.GetString() != "sanitized")
        {
            return await WriteAsync(SafeResult.Blocked(new Blocker(BlockerCode.OfflineFixtureInvalid, "fixture", "OFFLINE_FIXTURE_INVALID", "Use a manifest-approved sanitized fixture.", "Stop the offline run.")), output);
        }

        var endpoint = new EndpointClassification("GET_PACKAGES", "POST", "/ServiceModel/PackageService.svc/GetPackages", RequestBodyShape.EmptyObject, EndpointClassification.ExactAllowlistVersion);
        var transportResult = await _transport.SendAsync(endpoint, cancellationToken);
        if (!transportResult.IsSuccess)
        {
            return await WriteAsync(transportResult, output);
        }

        return await WriteAsync(SafeResult.SuccessForHumanReview("fixture " + Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(fixturePath, cancellationToken))).ToLowerInvariant()), output);
    }

    private static async Task<SafeResult> WriteAsync(SafeResult result, TextWriter output)
    {
        await output.WriteLineAsync($"reason={result.Reason}");
        await output.WriteLineAsync($"scope={result.Scope}");
        await output.WriteLineAsync($"recovery={result.Recovery}");
        await output.WriteLineAsync($"nextAction={result.NextPermittedAction}");
        return result;
    }
}
