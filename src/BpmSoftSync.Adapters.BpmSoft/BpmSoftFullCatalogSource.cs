using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using BpmSoftSync.Application;
using BpmSoftSync.Domain;

namespace BpmSoftSync.Adapters.BpmSoft;

/// <summary>Production read-only source assembled solely from the accepted transport and adapters.</summary>
public sealed class BpmSoftFullCatalogSource(BpmSoftReadTransport transport, BpmSoftTargetOrigin origin) : IFullCatalogSource
{
    private readonly BpmSoftReadTransport _transport = transport ?? throw new ArgumentNullException(nameof(transport));
    private readonly BpmSoftTargetOrigin _origin = origin ?? throw new ArgumentNullException(nameof(origin));

    public async ValueTask<FullCatalogRead> ReadFullAsync(CatalogReadRequest request, CancellationToken cancellationToken = default)
        => await ReadCoreAsync(request, bestEffort: false, cancellationToken);

    public async ValueTask<FullCatalogRead> ReadBestEffortAsync(CatalogReadRequest request, CancellationToken cancellationToken = default)
        => await ReadCoreAsync(request, bestEffort: true, cancellationToken);

    private async ValueTask<FullCatalogRead> ReadCoreAsync(CatalogReadRequest request, bool bestEffort, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var watch = Stopwatch.StartNew();
        var workspaceReadId = Guid.NewGuid();
        var workspace = await WorkspaceInventoryAdapter.ReadFullAsync(_transport, cancellationToken);
        var schemaReadIds = workspace.Schemas.Select(_ => Guid.NewGuid()).ToArray();
        var lookupReadId = Guid.NewGuid();
        var limits = request.Policy.LookupTemplate.Limits;
        var lookupLimits = new LookupReadLimits(limits.PageSize, limits.MaxPages, limits.MaxRows, limits.MaxResponseBytes);
        var lookupSource = new LookupCatalogSource(_transport);
        var readLookups = bestEffort
            ? await lookupSource.ReadBestEffortAsync(workspace, lookupLimits, cancellationToken)
            : await lookupSource.ReadFullAsync(workspace, lookupLimits, cancellationToken);
        // The SelectQuery schema name is transport detail. The sealed scope exposes the canonical registry contract only.
        var lookups = readLookups.RegistryManifest is null
            ? readLookups
            : readLookups with { RegistryManifest = readLookups.RegistryManifest with { CollectionId = "lookup-registry" } };
        var lookupReadIds = lookups.Collections.ToDictionary(item => item.Manifest.CollectionId, _ => Guid.NewGuid(), StringComparer.Ordinal);
        var contracts = request.SealedScope?.Collections ?? request.Policy.CreatePassAContracts(lookups.Collections.Select(item => item.Manifest.CollectionId).ToArray());
        var buckets = contracts.ToDictionary(item => item.CollectionId, item => item.CollectionId switch
        {
            "workspace" => "0-16KiB",
            "schemas" => "0-16KiB",
            "lookup-registry" => lookups.RegistryManifest?.Telemetry.ResponseSizeBucket ?? "not-recorded",
            _ => lookups.Collections.SingleOrDefault(collection => collection.Manifest.CollectionId == item.CollectionId)?.Manifest.Telemetry.ResponseSizeBucket ?? "0-16KiB"
        }, StringComparer.Ordinal);
        var blocker = workspace.Blocker ?? lookups.Blocker;
        return new FullCatalogRead(
            request.IndependentReadId,
            "bpmsoft-origin-sha256:" + Digest(_origin.Uri.GetLeftPart(UriPartial.Authority)),
            workspace,
            lookups,
            ["origin-policy-sha256:" + Digest(_origin.Uri.GetLeftPart(UriPartial.Authority))],
            contracts,
            new CatalogReadAttestation(workspaceReadId, schemaReadIds, lookupReadId, lookupReadIds),
            buckets,
            watch.Elapsed,
            1_048_576,
            blocker);
    }

    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
