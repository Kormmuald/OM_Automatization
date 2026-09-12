try
{
    BpmSoftSync.Domain.Tests.DomainContractTests.RequiredContractsExist();
    Console.WriteLine("PASS DomainContractTests.RequiredContractsExist");
    BpmSoftSync.Domain.Tests.CatalogReaderPagingTests.EveryAdversarialFixtureTerminatesWithNamedBlocker();
    BpmSoftSync.Domain.Tests.CatalogReaderPagingTests.ValidPagingHasCanonicalSafeManifests();
    Console.WriteLine("PASS CatalogReaderPagingTests");
    BpmSoftSync.Domain.Tests.FixtureManifestTests.EveryApprovedFixtureHasMatchingSha256Digest();
    Console.WriteLine("PASS FixtureManifestTests");
    BpmSoftSync.Domain.Tests.WorkspaceInventoryTests.PackageLayerIdentityDoesNotMergeDisplayNames();
    BpmSoftSync.Domain.Tests.WorkspaceInventoryTests.EverySourceItemHasExactlyOneSupportStatus();
    Console.WriteLine("PASS WorkspaceInventoryTests");
    BpmSoftSync.Domain.Tests.TargetFingerprintTests.PropertyOrderDoesNotChangeDigestButContractDataDoes();
    BpmSoftSync.Domain.Tests.TargetFingerprintTests.SemanticallyEquivalentJsonPropertyOrdersProduceSameFingerprint();
    Console.WriteLine("PASS TargetFingerprintTests");
    BpmSoftSync.Domain.Tests.LegacyDispositionTests.ManifestClassifiesEveryLegacySemantic();
    BpmSoftSync.Domain.Tests.LegacyDispositionTests.InvalidDispositionIsRejected();
    Console.WriteLine("PASS LegacyDispositionTests");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
